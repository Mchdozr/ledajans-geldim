using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class NonWorkingDaysController : ControllerBase
{
    private readonly AppDbContext _db;

    public NonWorkingDaysController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<NonWorkingDayDto>>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var query = _db.NonWorkingDays.Include(n => n.User).AsQueryable();
        if (from is not null) query = query.Where(n => n.Date >= from);
        if (to is not null) query = query.Where(n => n.Date <= to);

        var items = await query
            .OrderByDescending(n => n.Date)
            .ThenBy(n => n.User!.FullName)
            .Select(n => new NonWorkingDayDto
            {
                Id = n.Id,
                Date = n.Date,
                Type = n.Type,
                UserId = n.UserId,
                UserFullName = n.User != null ? n.User.FullName : null,
                Note = n.Note
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<NonWorkingDayDto>> Create(CreateNonWorkingDayRequest request)
    {
        if (!IsValidType(request.Type))
            return BadRequest(new { message = "Geçersiz izin/tatil türü." });

        if (request.Type == NonWorkingDayTypes.Holiday)
            request.UserId = null;
        else if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { message = "İzin için çalışan seçilmelidir." });

        if (request.Type is NonWorkingDayTypes.Leave or NonWorkingDayTypes.AnnualLeave)
        {
            var exists = await _db.NonWorkingDays.AnyAsync(n =>
                n.Date == request.Date && n.UserId == request.UserId);
            if (exists)
                return Conflict(new { message = "Bu çalışan için bu tarihte zaten kayıt var." });
        }
        else
        {
            var exists = await _db.NonWorkingDays.AnyAsync(n =>
                n.Date == request.Date && n.UserId == null && n.Type == NonWorkingDayTypes.Holiday);
            if (exists)
                return Conflict(new { message = "Bu tarih için zaten resmi tatil tanımlı." });
        }

        var entity = new NonWorkingDay
        {
            Date = request.Date,
            Type = request.Type,
            UserId = request.UserId,
            Note = request.Note
        };

        _db.NonWorkingDays.Add(entity);
        await _db.SaveChangesAsync();

        ApplicationUser? user = null;
        if (entity.UserId is not null)
            user = await _db.Users.FindAsync(entity.UserId);

        return Ok(new NonWorkingDayDto
        {
            Id = entity.Id,
            Date = entity.Date,
            Type = entity.Type,
            UserId = entity.UserId,
            UserFullName = user?.FullName,
            Note = entity.Note
        });
    }

    [HttpPost("range")]
    public async Task<ActionResult<CreateNonWorkingDayRangeResponse>> CreateRange(CreateNonWorkingDayRangeRequest request)
    {
        if (!IsValidType(request.Type))
            return BadRequest(new { message = "Geçersiz izin/tatil türü." });

        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { message = "Çalışan seçilmelidir." });

        if (request.FromDate > request.ToDate)
            return BadRequest(new { message = "Başlangıç tarihi bitişten büyük olamaz." });

        var dayCount = request.ToDate.DayNumber - request.FromDate.DayNumber + 1;
        if (dayCount > 366)
            return BadRequest(new { message = "En fazla 366 günlük izin aralığı girilebilir." });

        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var existingDates = await _db.NonWorkingDays
            .Where(n => n.UserId == request.UserId && n.Date >= request.FromDate && n.Date <= request.ToDate)
            .Select(n => n.Date)
            .ToListAsync();
        var existingSet = existingDates.ToHashSet();

        var created = 0;
        var skipped = 0;
        for (var d = request.FromDate; d <= request.ToDate; d = d.AddDays(1))
        {
            if (existingSet.Contains(d))
            {
                skipped++;
                continue;
            }

            _db.NonWorkingDays.Add(new NonWorkingDay
            {
                Date = d,
                Type = request.Type,
                UserId = request.UserId,
                Note = request.Note
            });
            created++;
        }

        if (created == 0)
            return Conflict(new { message = "Seçilen aralıkta eklenecek yeni gün yok (tümü zaten kayıtlı)." });

        await _db.SaveChangesAsync();

        return Ok(new CreateNonWorkingDayRangeResponse
        {
            CreatedCount = created,
            SkippedCount = skipped
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.NonWorkingDays.FindAsync(id);
        if (item is null) return NotFound();
        _db.NonWorkingDays.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsValidType(string type)
        => type is NonWorkingDayTypes.Holiday or NonWorkingDayTypes.Leave or NonWorkingDayTypes.AnnualLeave;
}
