using System.Net.Http.Json;
using Ledajans.Shared;

namespace Ledajans.Client.Services;

public class DepartmentService
{
    private readonly HttpClient _http;

    public DepartmentService(HttpClient http) => _http = http;

    public async Task<List<DepartmentDto>> GetAllAsync()
        => await _http.GetJsonOrDefaultAsync<List<DepartmentDto>>("api/departments") ?? [];

    public async Task<(bool ok, string? error, DepartmentDto? item)> CreateAsync(string name)
    {
        var response = await _http.PostAsJsonAsync("api/departments", new CreateDepartmentRequest { Name = name });
        if (response.IsSuccessStatusCode)
        {
            var item = await response.Content.ReadFromJsonAsync<DepartmentDto>();
            return (true, null, item);
        }

        return (false, await ReadErrorAsync(response), null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/departments/{id}");
        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ReadErrorAsync(response));
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return err?.Message ?? "İşlem başarısız.";
        }
        catch
        {
            return "İşlem başarısız.";
        }
    }

    private record ErrorResponse(string Message);
}
