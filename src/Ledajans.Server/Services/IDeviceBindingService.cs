namespace Ledajans.Server.Services;

public interface IDeviceBindingService
{
    Task<DeviceBindingResult> ValidateAndRegisterAsync(string userId, string deviceId, string? userAgent);
}

public record DeviceBindingResult(bool Allowed, string? Message);
