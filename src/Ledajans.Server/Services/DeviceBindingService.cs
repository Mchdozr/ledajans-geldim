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

        var deviceBinding = await _db.UserDevices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        var userBinding = await _db.UserDevices.FirstOrDefaultAsync(d => d.UserId == userId);
        var now = DateTime.UtcNow;
        var agent = TruncateUserAgent(userAgent);

        if (deviceBinding is not null && deviceBinding.UserId != userId)
        {
            return new DeviceBindingResult(false,
                "Bu cihaz daha önce başka bir kullanıcı için kayıt edilmiştir. Yöneticinize başvurun.");
        }

        if (userBinding is not null && userBinding.DeviceId != deviceId)
        {
            return new DeviceBindingResult(false,
                "Hesabınız başka bir cihaz veya tarayıcıda kayıtlıdır. İlk giriş yaptığınız yerden devam edin veya yöneticinize başvurun.");
        }

        if (deviceBinding is not null)
        {
            deviceBinding.LastLoginUtc = now;
            if (agent is not null)
                deviceBinding.UserAgent = agent;
            await _db.SaveChangesAsync();
            return new DeviceBindingResult(true, null);
        }

        _db.UserDevices.Add(new UserDevice
        {
            DeviceId = deviceId,
            UserId = userId,
            RegisteredAtUtc = now,
            LastLoginUtc = now,
            UserAgent = agent
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return new DeviceBindingResult(false,
                "Cihaz kaydı oluşturulamadı. Yöneticinize başvurun.");
        }

        return new DeviceBindingResult(true, null);
    }

    private static string? TruncateUserAgent(string? userAgent)
        => string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 512)];
}
