namespace Ledajans.Server.Data;

public class CompanySettings
{
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public int LateCheckInHour { get; set; } = 9;
    public int LateCheckInMinute { get; set; } = 15;
}
