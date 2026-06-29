namespace Ledajans.Server.Data;

public class Department
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
