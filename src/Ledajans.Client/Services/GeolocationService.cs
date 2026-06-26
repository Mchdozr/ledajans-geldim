using Microsoft.JSInterop;

namespace Ledajans.Client.Services;

public record GeoPosition(double Latitude, double Longitude, double Accuracy, bool LowAccuracy = false);

public class GeolocationService
{
    private readonly IJSRuntime _js;

    public GeolocationService(IJSRuntime js) => _js = js;

    /// <summary>Harita ve mesafe önizlemesi — mümkün olan en iyi konumu döner.</summary>
    public async Task<GeoPosition> GetPreviewPositionAsync()
        => await _js.InvokeAsync<GeoPosition>("ledajansGeo.getCurrentPosition", new
        {
            mode = "preview",
            idealAccuracyMeters = 60,
            maxAccuracyMeters = 250,
            timeoutMs = 10000
        });

    public async Task<GeoPosition> GetCheckInPositionAsync()
        => await _js.InvokeAsync<GeoPosition>("ledajansGeo.getCurrentPosition", new
        {
            mode = "checkin",
            idealAccuracyMeters = 60,
            maxAccuracyMeters = 250,
            timeoutMs = 60000
        });

    public Task<GeoPosition> GetCurrentPositionAsync() => GetPreviewPositionAsync();

    public async Task DownloadAsync(string fileName, string contentType, byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        await _js.InvokeVoidAsync("ledajansDownload", fileName, contentType, base64);
    }
}
