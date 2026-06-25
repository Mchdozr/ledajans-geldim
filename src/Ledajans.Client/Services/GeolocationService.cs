using Microsoft.JSInterop;

namespace Ledajans.Client.Services;

public record GeoPosition(double Latitude, double Longitude, double Accuracy);

public class GeolocationService
{
    private const double MaxAccuracyMeters = 30;
    private readonly IJSRuntime _js;

    public GeolocationService(IJSRuntime js) => _js = js;

    public async Task<GeoPosition> GetCurrentPositionAsync()
        => await _js.InvokeAsync<GeoPosition>("ledajansGeo.getCurrentPosition", MaxAccuracyMeters);

    public async Task DownloadAsync(string fileName, string contentType, byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        await _js.InvokeVoidAsync("ledajansDownload", fileName, contentType, base64);
    }
}
