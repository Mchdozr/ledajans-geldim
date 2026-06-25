using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class UserService
{
    private readonly HttpClient _http;

    public UserService(HttpClient http) => _http = http;

    public async Task<List<UserDto>> GetAllAsync()
        => await _http.GetJsonOrDefaultAsync<List<UserDto>>("api/users") ?? new();

    public async Task<(bool ok, string? error)> CreateAsync(CreateUserRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/users", request);
        return await ParseAsync(response);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(string id, UpdateUserRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}", request);
        return await ParseAsync(response);
    }

    public async Task<(bool ok, string? error)> SetPasswordAsync(string id, SetPasswordRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}/password", request);
        return await ParseAsync(response);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/users/{id}");
        return await ParseAsync(response);
    }

    private static async Task<(bool, string?)> ParseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, err?.Message ?? "İşlem başarısız.");
        }
        catch
        {
            return (false, "İşlem başarısız.");
        }
    }

    private record ErrorResponse(string Message);
}
