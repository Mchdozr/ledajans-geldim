namespace Ledajans.Server.Data;

public class AttendanceRecord
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateOnly LocalDate { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public string? IpAddress { get; set; }
    public bool IsManual { get; set; }
    public string? AdminNote { get; set; }
}
