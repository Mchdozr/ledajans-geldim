using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ILocationScope _locationScope;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILocationScope locationScope)
    {
        _userManager = userManager;
        _db = db;
        _locationScope = locationScope;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var locationName = await _db.Locations
            .Where(l => l.Id == locationId)
            .Select(l => l.Name)
            .FirstAsync();

        var users = await _userManager.Users
            .Where(u => u.LocationId == locationId || u.LocationId == null)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var deviceCounts = await _db.UserDevices
            .GroupBy(d => d.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var list = new List<UserDto>();
        foreach (var u in users)
        {
            var role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? Roles.Employee;
            if (role == Roles.Employee && u.LocationId != locationId)
                continue;

            list.Add(ToDto(u, role, deviceCounts.GetValueOrDefault(u.Id), locationName));
        }
        return Ok(list);
    }

    [HttpGet("device-bindings")]
    public async Task<ActionResult<List<UserDeviceBindingDto>>> GetDeviceBindings()
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var locationUserIds = await _db.Users
            .Where(u => u.LocationId == locationId)
            .Select(u => u.Id)
            .ToListAsync();

        var rows = await _db.UserDevices
            .Where(d => locationUserIds.Contains(d.UserId))
            .OrderByDescending(d => d.LastLoginUtc)
            .ToListAsync();

        if (rows.Count == 0)
            return Ok(new List<UserDeviceBindingDto>());

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var result = rows.Select(d =>
        {
            users.TryGetValue(d.UserId, out var user);
            return new UserDeviceBindingDto
            {
                Id = d.Id,
                UserId = d.UserId,
                UserName = user?.UserName ?? "—",
                FullName = user?.FullName ?? "—",
                DeviceIdShort = ShortDeviceId(d.DeviceId),
                RegisteredAtUtc = d.RegisteredAtUtc,
                LastLoginUtc = d.LastLoginUtc,
                UserAgent = d.UserAgent
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        if (await _userManager.FindByNameAsync(request.UserName) is not null)
            return BadRequest(new { message = "Bu kullanıcı adı zaten kullanımda." });

        var role = request.Role == Roles.Admin ? Roles.Admin : Roles.Employee;
        var userLocationId = role == Roles.Admin ? request.LocationId : (request.LocationId ?? locationId);

        if (role == Roles.Employee && userLocationId != locationId)
            return BadRequest(new { message = "Çalışan yalnızca seçili kuruma eklenebilir." });

        var locationName = await _db.Locations
            .Where(l => l.Id == userLocationId)
            .Select(l => l.Name)
            .FirstOrDefaultAsync();

        var department = await NormalizeDepartmentAsync(locationId.Value, request.Department);
        if (department is null)
            return BadRequest(new { message = "Geçersiz departman seçimi." });

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Department = department,
            IsActive = true,
            LocationId = userLocationId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, role);
        return Ok(ToDto(user, role, 0, locationName));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var newRole = request.Role == Roles.Admin ? Roles.Admin : Roles.Employee;
        if (newRole == Roles.Employee && user.LocationId != locationId && request.LocationId != locationId)
            return NotFound();

        var department = await NormalizeDepartmentAsync(locationId.Value, request.Department);
        if (department is null)
            return BadRequest(new { message = "Geçersiz departman seçimi." });

        var newUserName = request.UserName.Trim();
        if (string.IsNullOrWhiteSpace(newUserName))
            return BadRequest(new { message = "Kullanıcı adı gerekli." });

        if (!string.Equals(user.UserName, newUserName, StringComparison.Ordinal))
        {
            var taken = await _userManager.FindByNameAsync(newUserName);
            if (taken is not null && taken.Id != user.Id)
                return BadRequest(new { message = "Bu kullanıcı adı zaten kullanımda." });

            var userNameResult = await _userManager.SetUserNameAsync(user, newUserName);
            if (!userNameResult.Succeeded)
                return BadRequest(new { message = string.Join(" ", userNameResult.Errors.Select(e => e.Description)) });
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.Department = department;

        if (newRole == Roles.Employee)
            user.LocationId = request.LocationId ?? locationId;
        else if (request.LocationId.HasValue)
            user.LocationId = request.LocationId;

        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(newRole))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);
        }

        var locationName = user.LocationId is not null
            ? await _db.Locations.Where(l => l.Id == user.LocationId).Select(l => l.Name).FirstOrDefaultAsync()
            : null;

        var deviceCount = await _db.UserDevices.CountAsync(d => d.UserId == id);
        return Ok(ToDto(user, newRole, deviceCount, locationName));
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> SetPassword(string id, SetPasswordRequest request)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.LocationId != locationId && user.LocationId is not null)
            return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return NoContent();
    }

    [HttpDelete("{id}/devices")]
    public async Task<IActionResult> ClearDevices(string id)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.LocationId != locationId)
            return NotFound();

        var devices = await _db.UserDevices.Where(d => d.UserId == id).ToListAsync();
        if (devices.Count == 0)
            return NoContent();

        _db.UserDevices.RemoveRange(devices);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.LocationId != locationId && user.LocationId is not null)
            return NotFound();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    private static UserDto ToDto(ApplicationUser u, string role, int boundDeviceCount, string? locationName) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName,
        Email = u.Email,
        Role = role,
        Department = u.Department,
        IsActive = u.IsActive,
        BoundDeviceCount = boundDeviceCount,
        LocationId = u.LocationId,
        LocationName = locationName
    };

    private async Task<string?> NormalizeDepartmentAsync(int locationId, string? department)
    {
        if (string.IsNullOrWhiteSpace(department))
            return await _db.Departments
                .Where(d => d.LocationId == locationId)
                .OrderBy(d => d.SortOrder)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();

        var trimmed = department.Trim();
        var exists = await _db.Departments.AnyAsync(d => d.LocationId == locationId && d.Name == trimmed);
        return exists ? trimmed : null;
    }

    private static string ShortDeviceId(string deviceId)
        => deviceId.Length <= 8 ? deviceId : deviceId[..8] + "…";
}
