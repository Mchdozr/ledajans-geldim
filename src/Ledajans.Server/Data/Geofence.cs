namespace Ledajans.Server.Data;

public class Geofence
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}
