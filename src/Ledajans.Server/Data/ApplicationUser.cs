using Microsoft.AspNetCore.Identity;

namespace Ledajans.Server.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
