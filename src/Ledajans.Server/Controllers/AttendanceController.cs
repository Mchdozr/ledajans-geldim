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
            CheckInUtc = record?.CheckInUtc
        });
    }

    [HttpPost("checkin")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<CheckInResponse>> CheckIn(CheckInRequest request)
    {
        if (!await IsActiveEmployeeAsync())
            return Unauthorized(new { message = "Hesabınız pasif." });

        if (request.Accuracy is > 0 and var acc && acc > _settings.MaxGpsAccuracyMeters)
        {
            return Ok(new CheckInResponse
            {
                Success = false,
                Message = $"Konum hassasiyeti düşük ({Math.Round(acc)} m). Açık alana çıkıp tekrar deneyin."
            });
        }

        var geofence = await _db.Geofences.FirstOrDefaultAsync(g => g.IsActive);
        if (geofence is null)
            return Ok(new CheckInResponse { Success = false, Message = "Aktif konum tanımlı değil. Yöneticinize başvurun." });

        var distance = GeoHelper.DistanceMeters(
            geofence.Latitude, geofence.Longitude,
            request.Latitude, request.Longitude);

        if (distance > geofence.RadiusMeters)
        {
            return Ok(new CheckInResponse
            {
                Success = false,
                Message = $"Konum sınırının dışındasınız. Sınıra uzaklığınız {Math.Round(distance - geofence.RadiusMeters)} m.",
                DistanceMeters = Math.Round(distance, 1)
            });
        }

        var today = AppTime.Today;
        var exists = await _db.AttendanceRecords
            .AnyAsync(a => a.UserId == UserId && a.LocalDate == today);

        if (exists)
        {
            return Ok(new CheckInResponse
            {
                Success = false,
                Message = "Bugün zaten 'Geldim' işaretlediniz.",
                DistanceMeters = Math.Round(distance, 1)
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
            DistanceMeters = Math.Round(distance, 1),
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
            DistanceMeters = Math.Round(distance, 1)
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
                DistanceMeters = a.DistanceMeters
            })
            .ToListAsync();

        return Ok(items);
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
