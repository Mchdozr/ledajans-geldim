namespace Ledajans.Server.Services;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Ledajans";
    public string Audience { get; set; } = "Ledajans";
    public int ExpiryHours { get; set; } = 12;
}
