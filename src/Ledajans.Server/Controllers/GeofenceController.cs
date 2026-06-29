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
public class GeofenceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILocationScope _locationScope;

    public GeofenceController(AppDbContext db, ILocationScope locationScope)
    {
        _db = db;
        _locationScope = locationScope;
    }

    [HttpGet]
    public async Task<ActionResult<GeofenceDto>> Get()
    {
        var locationId = await ResolveLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var g = await _db.Geofences.FirstOrDefaultAsync(x => x.IsActive && x.LocationId == locationId);
        if (g is null) return NotFound();
        return Ok(ToDto(g));
    }

    [HttpPut]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<GeofenceDto>> Update(GeofenceDto dto)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var g = await _db.Geofences.FirstOrDefaultAsync(x => x.LocationId == locationId);
        if (g is null)
        {
            g = new Geofence { LocationId = locationId.Value, IsActive = true };
            _db.Geofences.Add(g);
        }

        g.Name = dto.Name;
        g.Latitude = dto.Latitude;
        g.Longitude = dto.Longitude;
        g.RadiusMeters = dto.RadiusMeters;
        g.IsActive = true;

        await _db.SaveChangesAsync();
        return Ok(ToDto(g));
    }

    private async Task<int?> ResolveLocationIdAsync()
    {
        if (User.IsInRole(Roles.Admin))
            return await _locationScope.GetAdminLocationIdAsync();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return null;
        return await _locationScope.GetEmployeeLocationIdAsync(userId);
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
