using System.ComponentModel.DataAnnotations;

namespace Ledajans.Server.Data;

public class UserDevice
{
    public int Id { get; set; }

    [MaxLength(128)]
    public string DeviceId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime RegisteredAtUtc { get; set; }
    public DateTime LastLoginUtc { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
