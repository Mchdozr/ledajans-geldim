using Ledajans.Shared;
using Microsoft.AspNetCore.Identity;

namespace Ledajans.Server.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = Departments.Teknik;
    public bool IsActive { get; set; } = true;
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
}
