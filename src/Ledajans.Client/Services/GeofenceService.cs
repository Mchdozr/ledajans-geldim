using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class GeofenceService
{
    private readonly HttpClient _http;

    public GeofenceService(HttpClient http) => _http = http;

    public async Task<GeofenceDto?> GetAsync()
    {
        var response = await _http.GetAsync("api/geofence");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GeofenceDto>();
    }

    public async Task<GeofenceDto?> UpdateAsync(GeofenceDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/geofence", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GeofenceDto>();
    }
}
