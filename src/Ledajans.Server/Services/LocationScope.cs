using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Services;

public class LocationScope : ILocationScope
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;
    private int? _cachedAdminLocationId;

    public LocationScope(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<int?> GetAdminLocationIdAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedAdminLocationId.HasValue)
            return _cachedAdminLocationId;

        var code = _httpContextAccessor.HttpContext?.Request.Headers[LocationHeaders.Name]
            .FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
            return null;

        var locationId = await _db.Locations.AsNoTracking()
            .Where(l => l.IsActive && l.Code == code)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (locationId.HasValue)
            _cachedAdminLocationId = locationId;

        return locationId;
    }

    public async Task<int> RequireAdminLocationIdAsync(CancellationToken cancellationToken = default)
    {
        var id = await GetAdminLocationIdAsync(cancellationToken);
        if (id is null)
            throw new InvalidOperationException("Kurum seçimi gerekli.");

        return id.Value;
    }

    public Task<int?> GetEmployeeLocationIdAsync(string userId, CancellationToken cancellationToken = default)
        => _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.LocationId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> RequireEmployeeLocationIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var id = await GetEmployeeLocationIdAsync(userId, cancellationToken);
        if (id is null)
            throw new InvalidOperationException("Hesabınıza kurum atanmamış. Yöneticinize başvurun.");

        return id.Value;
    }
}
