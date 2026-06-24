using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class AttendanceService
{
    private readonly HttpClient _http;

    public AttendanceService(HttpClient http) => _http = http;

    public async Task<TodayStatusResponse> GetTodayAsync()
    {
        var response = await _http.GetAsync("api/attendance/today");
        if (!response.IsSuccessStatusCode)
            return new TodayStatusResponse();
        return await response.Content.ReadFromJsonAsync<TodayStatusResponse>() ?? new();
    }

    public async Task<CheckInResponse> CheckInAsync(CheckInRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/attendance/checkin", request);
        if (!response.IsSuccessStatusCode)
            return new CheckInResponse { Success = false, Message = "Sunucuya bağlanılamadı." };
        return await response.Content.ReadFromJsonAsync<CheckInResponse>()
            ?? new CheckInResponse { Success = false, Message = "Sunucu hatası." };
    }

    public async Task<List<MyAttendanceHistoryItem>> GetMyHistoryAsync(int limit = 100)
        => await _http.GetFromJsonAsync<List<MyAttendanceHistoryItem>>($"api/attendance/history?limit={limit}") ?? new();

    public async Task<AttendanceReportItem?> ManualCheckInAsync(ManualCheckInRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/attendance/manual", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AttendanceReportItem>();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/attendance/{id}");
        return response.IsSuccessStatusCode;
    }
}
