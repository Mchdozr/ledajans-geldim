using Ledajans.Server.Data;
using Ledajans.Server.Data;
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

    public UsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var list = new List<UserDto>();
        foreach (var u in users)
        {
            var role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? Roles.Employee;
            list.Add(ToDto(u, role));
        }
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        if (await _userManager.FindByNameAsync(request.UserName) is not null)
            return BadRequest(new { message = "Bu kullanıcı adı zaten kullanımda." });

        var role = request.Role == Roles.Admin ? Roles.Admin : Roles.Employee;
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Department = NormalizeDepartment(request.Department),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, role);
        return Ok(ToDto(user, role));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.Department = NormalizeDepartment(request.Department);
        await _userManager.UpdateAsync(user);

        var newRole = request.Role == Roles.Admin ? Roles.Admin : Roles.Employee;
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(newRole))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);
        }

        return Ok(ToDto(user, newRole));
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> SetPassword(string id, SetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return NoContent();
    }

    [HttpDelete("{id}/devices")]
    public async Task<IActionResult> ClearDevices(string id, [FromServices] AppDbContext db)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var devices = await db.UserDevices.Where(d => d.UserId == id).ToListAsync();
        if (devices.Count == 0)
            return NoContent();

        db.UserDevices.RemoveRange(devices);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    private static UserDto ToDto(ApplicationUser u, string role) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName,
        Email = u.Email,
        Role = role,
        Department = u.Department,
        IsActive = u.IsActive
    };

    private static string NormalizeDepartment(string? department)
        => Departments.All.Contains(department ?? "") ? department! : Departments.Teknik;
}
