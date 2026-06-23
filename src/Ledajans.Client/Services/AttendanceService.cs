using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class AttendanceService
{
    private readonly HttpClient _http;

    public AttendanceService(HttpClient http) => _http = http;

    public async Task<TodayStatusResponse> GetTodayAsync()
        => await _http.GetFromJsonAsync<TodayStatusResponse>("api/attendance/today") ?? new();

    public async Task<CheckInResponse> CheckInAsync(CheckInRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/attendance/checkin", request);
        return await response.Content.ReadFromJsonAsync<CheckInResponse>()
            ?? new CheckInResponse { Success = false, Message = "Sunucu hatası." };
    }

    public async Task<List<MyAttendanceHistoryItem>> GetMyHistoryAsync(int limit = 100)
        => await _http.GetFromJsonAsync<List<MyAttendanceHistoryItem>>($"api/attendance/history?limit={limit}") ?? new();
}
