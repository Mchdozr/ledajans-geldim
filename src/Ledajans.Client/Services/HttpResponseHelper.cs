using System.Net;
using System.Net.Http.Json;

namespace Ledajans.Client.Services;

internal static class HttpResponseHelper
{
    private sealed record ApiError(string? Message);

    internal static async Task<string> ToUserMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiError>();
            if (!string.IsNullOrWhiteSpace(err?.Message))
                return err.Message;
        }
        catch
        {
            // JSON degilse asagidaki kod mesajina dus
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Oturum süresi doldu. Çıkış yapıp tekrar giriş yapın.",
            HttpStatusCode.Forbidden => "Bu işlem için yetkiniz yok. Çalışan hesabıyla giriş yapın.",
            HttpStatusCode.BadRequest => "Geçersiz istek. Sayfayı yenileyip tekrar deneyin.",
            _ => $"Sunucu yanıt vermedi ({(int)response.StatusCode})."
        };
    }
}
