namespace Ledajans.Shared;

public static class GeoHelper
{
    private const double EarthRadiusMeters = 6371000d;

    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        if (!AreValidCoordinates(lat1, lon1) || !AreValidCoordinates(lat2, lon2))
            return double.PositiveInfinity;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180d;

    /// <summary>
    /// Ölçülen mesafe ofis yarıçapı içindeyse kabul et (kapalı alanda GPS hassasiyeti şişebilir).
    /// Sınırda ise mesafe + hassasiyet ile değerlendir.
    /// </summary>
    public static bool IsWithinGeofence(double distanceMeters, double accuracyMeters, double radiusMeters)
    {
        if (distanceMeters <= radiusMeters) return true;
        var accuracy = accuracyMeters > 0 ? accuracyMeters : 0;
        return distanceMeters + accuracy <= radiusMeters;
    }

    public static bool AreValidCoordinates(double latitude, double longitude)
        => latitude is >= -90 and <= 90
           && longitude is >= -180 and <= 180
           && !double.IsNaN(latitude) && !double.IsNaN(longitude)
           && !double.IsInfinity(latitude) && !double.IsInfinity(longitude);
}
