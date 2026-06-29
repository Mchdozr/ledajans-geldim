using System.Security.Claims;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledajans.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IAttendancePolicyService _policy;
    private readonly ILocationScope _locationScope;

    public SettingsController(IAttendancePolicyService policy, ILocationScope locationScope)
    {
        _policy = policy;
        _locationScope = locationScope;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("attendance")]
    public async Task<ActionResult<AttendancePolicyDto>> GetAttendance()
    {
        var locationId = await ResolveLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        return Ok(await _policy.GetAsync(locationId.Value));
    }

    [HttpPut("attendance")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AttendancePolicyDto>> UpdateAttendance(AttendancePolicyDto dto)
    {
        var locationId = await _locationScope.GetAdminLocationIdAsync();
        if (locationId is null)
            return BadRequest(new { message = "Kurum seçimi gerekli." });

        try
        {
            return Ok(await _policy.UpdateAsync(locationId.Value, dto));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { message = "Geçersiz saat. Saat 0-23, dakika 0-59 olmalı." });
        }
    }

    private async Task<int?> ResolveLocationIdAsync()
    {
        if (User.IsInRole(Roles.Admin))
            return await _locationScope.GetAdminLocationIdAsync();

        return await _locationScope.GetEmployeeLocationIdAsync(UserId);
    }
}
