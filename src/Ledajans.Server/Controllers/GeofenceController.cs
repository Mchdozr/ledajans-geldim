using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GeofenceController : ControllerBase
{
    private readonly AppDbContext _db;

    public GeofenceController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<GeofenceDto>> Get()
    {
        var g = await _db.Geofences.FirstOrDefaultAsync(x => x.IsActive);
        if (g is null) return NotFound();
        return Ok(ToDto(g));
    }

    [HttpPut]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<GeofenceDto>> Update(GeofenceDto dto)
    {
        var g = await _db.Geofences.FirstOrDefaultAsync(x => x.IsActive);
        if (g is null)
        {
            g = new Geofence { IsActive = true };
            _db.Geofences.Add(g);
        }

        g.Name = dto.Name;
        g.Latitude = dto.Latitude;
        g.Longitude = dto.Longitude;
        g.RadiusMeters = dto.RadiusMeters;

        await _db.SaveChangesAsync();
        return Ok(ToDto(g));
    }

    private static GeofenceDto ToDto(Geofence g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        RadiusMeters = g.RadiusMeters
    };
}
