using Ledajans.Shared;

namespace Ledajans.Server.Data;

public class NonWorkingDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Type { get; set; } = NonWorkingDayTypes.Holiday;
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? Note { get; set; }
}
