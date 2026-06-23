using System.Globalization;
using System.Text;
using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    private async Task<List<AttendanceReportItem>> QueryAsync(DateOnly? from, DateOnly? to, string? userId)
    {
        var query = _db.AttendanceRecords.Include(a => a.User).AsQueryable();
        if (from is not null) query = query.Where(a => a.LocalDate >= from);
        if (to is not null) query = query.Where(a => a.LocalDate <= to);
        if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(a => a.UserId == userId);

        return await query
            .OrderByDescending(a => a.CheckInUtc)
            .Select(a => new AttendanceReportItem
            {
                UserId = a.UserId,
                UserName = a.User!.UserName!,
                FullName = a.User.FullName,
                CheckInUtc = a.CheckInUtc,
                LocalDate = a.LocalDate,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                DistanceMeters = a.DistanceMeters,
                IpAddress = a.IpAddress
            })
            .ToListAsync();
    }

    [HttpGet]
    public async Task<ActionResult<List<AttendanceReportItem>>> Get(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? userId)
        => Ok(await QueryAsync(from, to, userId));

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? userId)
    {
        var items = await QueryAsync(from, to, userId);
        var sb = new StringBuilder();
        sb.AppendLine("Tarih;Kullanici;AdSoyad;GirisSaati(UTC);Enlem;Boylam;Uzaklik(m);IpAdresi");
        foreach (var i in items)
        {
            sb.AppendLine(string.Join(';',
                i.LocalDate.ToString("yyyy-MM-dd"),
                i.UserName,
                i.FullName,
                i.CheckInUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                i.Latitude.ToString(CultureInfo.InvariantCulture),
                i.Longitude.ToString(CultureInfo.InvariantCulture),
                i.DistanceMeters.ToString(CultureInfo.InvariantCulture),
                i.IpAddress ?? ""));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"yoklama_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }
}
