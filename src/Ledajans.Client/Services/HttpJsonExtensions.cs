using System.Net.Http.Json;

namespace Ledajans.Client.Services;

internal static class HttpJsonExtensions
{
    public static async Task<T?> GetJsonOrDefaultAsync<T>(this HttpClient http, string url)
    {
        try
        {
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return default;
        }
    }
}
