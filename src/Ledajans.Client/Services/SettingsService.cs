using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class SettingsService
{
    private readonly HttpClient _http;

    public SettingsService(HttpClient http) => _http = http;

    public async Task<AttendancePolicyDto?> GetAttendancePolicyAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/settings/attendance");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AttendancePolicyDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<AttendancePolicyDto?> UpdateAttendancePolicyAsync(AttendancePolicyDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/settings/attendance", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AttendancePolicyDto>();
    }
}
