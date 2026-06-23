using System.Security.Claims;
using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public AttendanceController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("today")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<TodayStatusResponse>> Today()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
        var ipAddress = ClientIpHelper.GetClientIp(HttpContext);
        _db.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = UserId,
            CheckInUtc = now,
            LocalDate = today,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DistanceMeters = Math.Round(distance, 1),
            IpAddress = ipAddress
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

    [HttpGet("history")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<List<MyAttendanceHistoryItem>>> MyHistory([FromQuery] int limit = 100)
    {
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
}
