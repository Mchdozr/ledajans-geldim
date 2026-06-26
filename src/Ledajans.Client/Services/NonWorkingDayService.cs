using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class NonWorkingDayService
{
    private readonly HttpClient _http;

    public NonWorkingDayService(HttpClient http) => _http = http;

    public async Task<List<NonWorkingDayDto>> GetAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var parts = new List<string>();
        if (from is not null) parts.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) parts.Add($"to={to:yyyy-MM-dd}");
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        return await _http.GetJsonOrDefaultAsync<List<NonWorkingDayDto>>($"api/nonworkingdays{qs}") ?? new();
    }

    public async Task<bool> CreateAsync(CreateNonWorkingDayRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/nonworkingdays", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<CreateNonWorkingDayRangeResponse?> CreateRangeAsync(CreateNonWorkingDayRangeRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/nonworkingdays/range", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateNonWorkingDayRangeResponse>();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/nonworkingdays/{id}");
        return response.IsSuccessStatusCode;
    }
}
