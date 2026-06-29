using System.Net.Http.Json;
using Blazored.LocalStorage;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class LocationService
{
    public const string CodeKey = "ledajans_location_code";
    public const string NameKey = "ledajans_location_name";

    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;

    public LocationService(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public event Action? Changed;

    public async Task<string?> GetCodeAsync()
    {
        var code = await _localStorage.GetItemAsStringAsync(CodeKey);
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().Trim('"');
    }

    public async Task<string?> GetNameAsync()
    {
        var name = await _localStorage.GetItemAsStringAsync(NameKey);
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim().Trim('"');
    }

    public async Task<bool> HasSelectionAsync()
        => !string.IsNullOrWhiteSpace(await GetCodeAsync());

    public async Task SetAsync(LocationDto location)
    {
        await _localStorage.SetItemAsStringAsync(CodeKey, location.Code);
        await _localStorage.SetItemAsStringAsync(NameKey, location.Name);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync(CodeKey);
        await _localStorage.RemoveItemAsync(NameKey);
        Changed?.Invoke();
    }

    public async Task<List<LocationDto>> GetAllAsync()
    {
        var response = await _http.GetAsync("api/locations");
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<LocationDto>>() ?? [];
    }
}
