using System.Net;

namespace Ledajans.Server.Services;

public static class ClientIpHelper
{
    public static string? GetClientIp(HttpContext context)
    {
        var candidates = new[]
        {
            context.Request.Headers["CF-Connecting-IP"].FirstOrDefault(),
            context.Request.Headers["X-Real-IP"].FirstOrDefault(),
            GetForwardedForIp(context.Request.Headers["X-Forwarded-For"].FirstOrDefault()),
            context.Connection.RemoteIpAddress?.ToString()
        };

        foreach (var candidate in candidates)
        {
            var normalized = Normalize(candidate);
            if (!string.IsNullOrWhiteSpace(normalized))
                return normalized;
        }

        return null;
    }

    private static string? GetForwardedForIp(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return null;

        return header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }

    private static string? Normalize(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        ip = ip.Trim();

        if (IPAddress.TryParse(ip, out var address))
        {
            if (IPAddress.IsLoopback(address))
                return "127.0.0.1";

            if (address.IsIPv4MappedToIPv6)
                return address.MapToIPv4().ToString();

            return address.ToString();
        }

        if (ip.Equals("::1", StringComparison.OrdinalIgnoreCase))
            return "127.0.0.1";

        if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            return ip["::ffff:".Length..];

        return ip;
    }
}
