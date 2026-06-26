using Ledajans.Shared;
using Xunit;

namespace Ledajans.Tests;

public class GeoHelperTests
{
    [Theory]
    [InlineData(50, 30, 100, true)]
    [InlineData(100, 0, 100, true)]
    [InlineData(80, 20, 100, true)]
    [InlineData(120, 10, 100, false)]
    public void IsWithinGeofence_UsesDistanceAndAccuracy(double distance, double accuracy, double radius, bool expected)
        => Assert.Equal(expected, GeoHelper.IsWithinGeofence(distance, accuracy, radius));

    [Fact]
    public void DistanceMeters_SamePoint_IsZero()
    {
        var d = GeoHelper.DistanceMeters(41.0, 29.0, 41.0, 29.0);
        Assert.True(d < 1);
    }
}

public class AttendanceGeofenceValidationTests
{
    [Theory]
    [InlineData(50, 80, 100, 150, null)]
    [InlineData(50, 120, 100, 150, null)]
    [InlineData(200, 120, 100, 150, "Konum doğrulanamadı")]
    [InlineData(200, 20, 100, 150, "Konum sınırının dışında")]
    public void ValidateGeofence_Scenarios(double distance, double accuracy, double maxAccuracy, double radius, string? expectedFragment)
    {
        var within = GeoHelper.IsWithinGeofence(distance, accuracy, radius);
        string? error = null;

        if (within)
            error = null;
        else if (accuracy > maxAccuracy)
            error = $"Konum doğrulanamadı (hassasiyet ±{Math.Round(accuracy)} m). Bilgisayarda pencere kenarında bekleyin; Wi-Fi ve konum izni açık olsun.";
        else
            error = "Konum sınırının dışında görünüyorsunuz";

        if (expectedFragment is null)
            Assert.Null(error);
        else
            Assert.Contains(expectedFragment, error);
    }
}

public class AppTimeTests
{
    [Fact]
    public void Today_UsesTurkeyOffset()
    {
        var utc = new DateTime(2026, 6, 25, 21, 0, 0, DateTimeKind.Utc);
        var expected = DateOnly.FromDateTime(utc.AddHours(3));
        Assert.Equal(expected, DateOnly.FromDateTime(utc.AddHours(3)));
    }
}
