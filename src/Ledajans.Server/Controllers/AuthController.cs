using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ledajans.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly IDeviceBindingService _deviceBinding;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IDeviceBindingService deviceBinding)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _deviceBinding = deviceBinding;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Hesabınız pasif durumda. Yöneticinize başvurun." });

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Employee;

        if (role == Roles.Employee)
        {
            var deviceResult = await _deviceBinding.ValidateAndRegisterAsync(
                user.Id,
                request.DeviceId ?? string.Empty,
                Request.Headers.UserAgent.ToString());

            if (!deviceResult.Allowed)
                return Unauthorized(new { message = deviceResult.Message });
        }

        var (token, expires) = _tokenService.CreateToken(user, role);

        return Ok(new LoginResponse
        {
            Token = token,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Role = role,
            ExpiresUtc = expires
        });
    }
}
