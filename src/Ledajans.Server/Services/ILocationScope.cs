namespace Ledajans.Server.Services;

public interface ILocationScope
{
    Task<int?> GetAdminLocationIdAsync(CancellationToken cancellationToken = default);
    Task<int> RequireAdminLocationIdAsync(CancellationToken cancellationToken = default);
    Task<int?> GetEmployeeLocationIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> RequireEmployeeLocationIdAsync(string userId, CancellationToken cancellationToken = default);
}
