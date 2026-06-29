using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Services;

public class DeviceBindingService : IDeviceBindingService
{
    private readonly AppDbContext _db;

    public DeviceBindingService(AppDbContext db) => _db = db;

    public async Task<DeviceBindingResult> ValidateAndRegisterAsync(string userId, string deviceId, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return new DeviceBindingResult(false,
                "Cihaz tanımlanamadı. Tarayıcı depolama iznini kontrol edip tekrar deneyin.");
        }

        deviceId = deviceId.Trim();
        if (deviceId.Length is < 16 or > 128)
        {
            return new DeviceBindingResult(false, "Geçersiz cihaz kimliği.");
        }

        var existing = await _db.UserDevices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            _db.UserDevices.Add(new UserDevice
            {
                DeviceId = deviceId,
                UserId = userId,
                RegisteredAtUtc = now,
                LastLoginUtc = now,
                UserAgent = TruncateUserAgent(userAgent)
            });
            await _db.SaveChangesAsync();
            return new DeviceBindingResult(true, null);
        }

        if (existing.UserId == userId)
        {
            existing.LastLoginUtc = now;
            if (!string.IsNullOrWhiteSpace(userAgent))
                existing.UserAgent = TruncateUserAgent(userAgent);
            await _db.SaveChangesAsync();
            return new DeviceBindingResult(true, null);
        }

        return new DeviceBindingResult(false,
            "Bu cihaz daha önce başka bir kullanıcı için kayıt edilmiştir. Yöneticinize başvurun.");
    }

    private static string? TruncateUserAgent(string? userAgent)
        => string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 512)];
}
