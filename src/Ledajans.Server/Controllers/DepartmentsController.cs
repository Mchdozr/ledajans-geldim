using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILocationScope _locationScope;

    public DepartmentsController(AppDbContext db, ILocationScope locationScope)
    {
        _db = db;
        _locationScope = locationScope;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll()
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var items = await _db.Departments
            .Where(d => d.LocationId == locationId)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                SortOrder = d.SortOrder
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentRequest request)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Departman adı gerekli." });

        var exists = await _db.Departments.AnyAsync(d => d.LocationId == locationId && d.Name == name);
        if (exists)
            return Conflict(new { message = "Bu departman zaten tanımlı." });

        var maxSort = await _db.Departments
            .Where(d => d.LocationId == locationId)
            .Select(d => (int?)d.SortOrder)
            .MaxAsync() ?? 0;

        var entity = new Department
        {
            LocationId = locationId.Value,
            Name = name,
            SortOrder = maxSort + 1
        };

        _db.Departments.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            SortOrder = entity.SortOrder
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.LocationId == locationId);
        if (department is null)
            return NotFound();

        var inUse = await _db.Users.AnyAsync(u =>
            u.LocationId == locationId && u.Department == department.Name);
        if (inUse)
            return Conflict(new { message = "Bu departmanda çalışan var. Önce çalışanları başka departmana taşıyın." });

        var count = await _db.Departments.CountAsync(d => d.LocationId == locationId);
        if (count <= 1)
            return BadRequest(new { message = "En az bir departman kalmalı." });

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
