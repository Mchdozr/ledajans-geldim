using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class ReportService
{
    private readonly HttpClient _http;

    public ReportService(HttpClient http) => _http = http;

    public async Task<List<AttendanceReportItem>> GetAsync(DateOnly? from, DateOnly? to, string? userId)
    {
        var url = $"api/reports?{BuildQuery(from, to, userId)}";
        return await _http.GetFromJsonAsync<List<AttendanceReportItem>>(url) ?? new();
    }

    public string BuildExportUrl(DateOnly? from, DateOnly? to, string? userId)
        => $"api/reports/export?{BuildQuery(from, to, userId)}";

    private static string BuildQuery(DateOnly? from, DateOnly? to, string? userId)
    {
        var parts = new List<string>();
        if (from is not null) parts.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) parts.Add($"to={to:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(userId)) parts.Add($"userId={Uri.EscapeDataString(userId)}");
        return string.Join("&", parts);
    }
}
