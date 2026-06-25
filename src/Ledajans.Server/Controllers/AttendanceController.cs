using System.Security.Claims;
using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AttendanceSettings _settings;

    public AttendanceController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<AttendanceSettings> settings)
    {
        _db = db;
        _userManager = userManager;
        _settings = settings.Value;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("today")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<TodayStatusResponse>> Today()
    {
        if (!await IsActiveEmployeeAsync())
            return Unauthorized(new { message = "Hesabınız pasif." });

        var today = AppTime.Today;
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == UserId && a.LocalDate == today);

        return Ok(new TodayStatusResponse
        {
            HasCheckedIn = record is not null,
            CheckInUtc = record?.CheckInUtc,
            HasCheckedOut = record?.CheckOutUtc is not null,
            CheckOutUtc = record?.CheckOutUtc
        });
    }

    [HttpPost("checkin")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<CheckInResponse>> CheckIn(CheckInRequest request)
    {
        if (!await IsActiveEmployeeAsync())
            return Unauthorized(new { message = "Hesabınız pasif." });

        var geo = await ValidateGeofenceAsync(request);
        if (geo.Error is not null)
            return Ok(new CheckInResponse { Success = false, Message = geo.Error, DistanceMeters = geo.Distance });

        var today = AppTime.Today;
        var exists = await _db.AttendanceRecords
            .AnyAsync(a => a.UserId == UserId && a.LocalDate == today);

        if (exists)
        {
            return Ok(new CheckInResponse
            {
                Success = false,
                Message = "Bugün zaten 'Geldim' işaretlediniz.",
                DistanceMeters = geo.Distance
            });
        }

        var now = DateTime.UtcNow;
        _db.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = UserId,
            CheckInUtc = now,
            LocalDate = today,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DistanceMeters = geo.Distance,
            IpAddress = ClientIpHelper.GetClientIp(HttpContext)
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Ok(new CheckInResponse { Success = false, Message = "Bugün zaten 'Geldim' işaretlediniz." });
        }

        return Ok(new CheckInResponse
        {
            Success = true,
            Message = "Geldiğiniz başarıyla kaydedildi.",
            CheckInUtc = now,
            DistanceMeters = geo.Distance
        });
    }

    [HttpPost("checkout")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<CheckOutResponse>> CheckOut(CheckInRequest request)
    {
        if (!await IsActiveEmployeeAsync())
            return Unauthorized(new { message = "Hesabınız pasif." });

        var geo = await ValidateGeofenceAsync(request);
        if (geo.Error is not null)
            return Ok(new CheckOutResponse { Success = false, Message = geo.Error, DistanceMeters = geo.Distance });

        var today = AppTime.Today;
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == UserId && a.LocalDate == today);

        if (record is null)
        {
            return Ok(new CheckOutResponse
            {
                Success = false,
                Message = "Önce 'Geldim' işaretlemeniz gerekiyor.",
                DistanceMeters = geo.Distance
            });
        }

        if (record.CheckOutUtc is not null)
        {
            return Ok(new CheckOutResponse
            {
                Success = false,
                Message = "Bugün zaten 'Çıkış Yaptım' işaretlediniz.",
                DistanceMeters = geo.Distance
            });
        }

        var now = DateTime.UtcNow;
        record.CheckOutUtc = now;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        record.CheckOutDistanceMeters = geo.Distance;
        record.CheckOutIpAddress = ClientIpHelper.GetClientIp(HttpContext);

        await _db.SaveChangesAsync();

        return Ok(new CheckOutResponse
        {
            Success = true,
            Message = "Çıkışınız başarıyla kaydedildi.",
            CheckOutUtc = now,
            DistanceMeters = geo.Distance
        });
    }

    [HttpPost("manual")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AttendanceReportItem>> ManualCheckIn(ManualCheckInRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (!await _userManager.IsInRoleAsync(user, Roles.Employee))
            return BadRequest(new { message = "Yalnızca çalışan hesaplarına manuel kayıt eklenebilir." });

        var localDate = request.LocalDate ?? AppTime.Today;
        var exists = await _db.AttendanceRecords
            .AnyAsync(a => a.UserId == request.UserId && a.LocalDate == localDate);

        if (exists)
            return Conflict(new { message = "Bu tarih için zaten kayıt var." });

        var geofence = await _db.Geofences.FirstOrDefaultAsync(g => g.IsActive);
        var now = DateTime.UtcNow;
        var record = new AttendanceRecord
        {
            UserId = request.UserId,
            CheckInUtc = now,
            LocalDate = localDate,
            Latitude = geofence?.Latitude ?? 0,
            Longitude = geofence?.Longitude ?? 0,
            DistanceMeters = 0,
            IpAddress = "manual",
            IsManual = true,
            AdminNote = request.Note
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(MapToReportItem(record, user));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.AttendanceRecords.FindAsync(id);
        if (record is null)
            return NotFound();

        _db.AttendanceRecords.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("history")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<List<MyAttendanceHistoryItem>>> MyHistory([FromQuery] int limit = 100)
    {
        if (!await IsActiveEmployeeAsync())
            return Unauthorized(new { message = "Hesabınız pasif." });

        if (limit is < 1 or > 500) limit = 100;

        var items = await _db.AttendanceRecords
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.CheckInUtc)
            .Take(limit)
            .Select(a => new MyAttendanceHistoryItem
            {
                LocalDate = a.LocalDate,
                CheckInUtc = a.CheckInUtc,
                CheckOutUtc = a.CheckOutUtc,
                DistanceMeters = a.DistanceMeters
            })
            .ToListAsync();

        return Ok(items);
    }

    private async Task<(string? Error, double Distance)> ValidateGeofenceAsync(CheckInRequest request)
    {
        var geofence = await _db.Geofences.FirstOrDefaultAsync(g => g.IsActive);
        if (geofence is null)
            return ("Aktif konum tanımlı değil. Yöneticinize başvurun.", 0);

        var distance = Math.Round(GeoHelper.DistanceMeters(
            geofence.Latitude, geofence.Longitude,
            request.Latitude, request.Longitude), 1);

        var accuracy = request.Accuracy is > 0 ? request.Accuracy.Value : 0;

        if (!GeoHelper.IsWithinGeofence(distance, accuracy, geofence.RadiusMeters))
        {
            var worstCase = distance + accuracy;
            var outsideBy = Math.Round(worstCase - geofence.RadiusMeters);
            return ($"Konum sınırının dışında görünüyorsunuz (en iyi ihtimalle sınıra ~{outsideBy} m). Açık alana çıkıp tekrar deneyin.", distance);
        }

        return (null, distance);
    }

    private async Task<bool> IsActiveEmployeeAsync()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        return user is not null && user.IsActive;
    }

    internal static AttendanceReportItem MapToReportItem(AttendanceRecord record, ApplicationUser user, AttendanceSettings settings)
        => new()
        {
            Id = record.Id,
            UserId = record.UserId,
            UserName = user.UserName!,
            FullName = user.FullName,
            Department = user.Department,
            CheckInUtc = record.CheckInUtc,
            CheckOutUtc = record.CheckOutUtc,
            LocalDate = record.LocalDate,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            DistanceMeters = record.DistanceMeters,
            IpAddress = record.IpAddress,
            IsManual = record.IsManual,
            AdminNote = record.AdminNote,
            IsLate = settings.IsLate(record.CheckInUtc)
        };

    private AttendanceReportItem MapToReportItem(AttendanceRecord record, ApplicationUser user)
        => MapToReportItem(record, user, _settings);
}
