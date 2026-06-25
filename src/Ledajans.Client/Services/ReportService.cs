using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class ReportService
{
    private readonly HttpClient _http;

    public ReportService(HttpClient http) => _http = http;

    public async Task<TodaySummaryResponse> GetTodaySummaryAsync(string? department = null)
    {
        var url = string.IsNullOrWhiteSpace(department)
            ? "api/reports/today-summary"
            : $"api/reports/today-summary?department={Uri.EscapeDataString(department)}";
        return await _http.GetJsonOrDefaultAsync<TodaySummaryResponse>(url) ?? new();
    }

    public async Task<List<AttendanceReportItem>> GetAsync(
        DateOnly? from, DateOnly? to, string? userId, string? department = null)
    {
        var url = $"api/reports?{BuildQuery(from, to, userId, department)}";
        return await _http.GetJsonOrDefaultAsync<List<AttendanceReportItem>>(url) ?? new();
    }

    public string BuildExportUrl(DateOnly? from, DateOnly? to, string? userId, string? department = null)
        => $"api/reports/export?{BuildQuery(from, to, userId, department)}";

    public string BuildAbsentExportUrl(DateOnly? date = null, DateOnly? from = null, DateOnly? to = null, string? department = null)
    {
        var parts = new List<string>();
        if (from is not null && to is not null)
        {
            parts.Add($"from={from:yyyy-MM-dd}");
            parts.Add($"to={to:yyyy-MM-dd}");
        }
        else if (date is not null)
            parts.Add($"date={date:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(department))
            parts.Add($"department={Uri.EscapeDataString(department)}");

        return parts.Count == 0
            ? "api/reports/export-absent"
            : $"api/reports/export-absent?{string.Join("&", parts)}";
    }

    private static string BuildQuery(DateOnly? from, DateOnly? to, string? userId, string? department)
    {
        var parts = new List<string>();
        if (from is not null) parts.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) parts.Add($"to={to:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(userId)) parts.Add($"userId={Uri.EscapeDataString(userId)}");
        if (!string.IsNullOrWhiteSpace(department)) parts.Add($"department={Uri.EscapeDataString(department)}");
        return string.Join("&", parts);
    }
}
