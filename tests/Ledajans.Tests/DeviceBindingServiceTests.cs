using Ledajans.Server.Data;
using Ledajans.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledajans.Tests;

public class DeviceBindingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DeviceBindingService _service;

    public DeviceBindingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new DeviceBindingService(_db);
    }

    [Fact]
    public async Task FirstLogin_RegistersDevice()
    {
        var result = await _service.ValidateAndRegisterAsync("user-1", "device-abc-12345678", "Mozilla");
        Assert.True(result.Allowed);
        Assert.Single(_db.UserDevices);
    }

    [Fact]
    public async Task SameUserSameDevice_AllowsLogin()
    {
        await _service.ValidateAndRegisterAsync("user-1", "device-abc-12345678", null);
        var result = await _service.ValidateAndRegisterAsync("user-1", "device-abc-12345678", null);
        Assert.True(result.Allowed);
        Assert.Single(_db.UserDevices);
    }

    [Fact]
    public async Task DifferentUserSameDevice_BlocksLogin()
    {
        await _service.ValidateAndRegisterAsync("user-1", "device-abc-12345678", null);
        var result = await _service.ValidateAndRegisterAsync("user-2", "device-abc-12345678", null);
        Assert.False(result.Allowed);
        Assert.Contains("başka bir kullanıcı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SameUserDifferentDevice_BlocksLogin()
    {
        await _service.ValidateAndRegisterAsync("user-1", "device-abc-12345678", null);
        var result = await _service.ValidateAndRegisterAsync("user-1", "device-xyz-9876543210", null);
        Assert.False(result.Allowed);
        Assert.Contains("başka bir cihaz", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyDeviceId_BlocksLogin()
    {
        var result = await _service.ValidateAndRegisterAsync("user-1", "", null);
        Assert.False(result.Allowed);
    }

    public void Dispose() => _db.Dispose();
}
