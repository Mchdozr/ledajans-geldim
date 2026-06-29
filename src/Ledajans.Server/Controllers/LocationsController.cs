using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LocationsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<LocationDto>>> GetAll()
    {
        var items = await _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Code = l.Code
            })
            .ToListAsync();

        return Ok(items);
    }
}
