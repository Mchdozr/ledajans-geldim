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

    public SettingsController(IAttendancePolicyService policy) => _policy = policy;

    [HttpGet("attendance")]
    public async Task<ActionResult<AttendancePolicyDto>> GetAttendance()
        => Ok(await _policy.GetAsync());

    [HttpPut("attendance")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AttendancePolicyDto>> UpdateAttendance(AttendancePolicyDto dto)
    {
        try
        {
            return Ok(await _policy.UpdateAsync(dto));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { message = "Geçersiz saat. Saat 0-23, dakika 0-59 olmalı." });
        }
    }
}
