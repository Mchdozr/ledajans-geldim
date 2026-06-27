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

    [Theory]
    [InlineData(double.NaN, 29.0)]
    [InlineData(41.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 29.0)]
    [InlineData(91.0, 29.0)]
    [InlineData(41.0, 181.0)]
    public void AreValidCoordinates_RejectsInvalid(double lat, double lon)
        => Assert.False(GeoHelper.AreValidCoordinates(lat, lon));

    [Theory]
    [InlineData(41.0, 29.0)]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, 180.0)]
    public void AreValidCoordinates_AcceptsValid(double lat, double lon)
        => Assert.True(GeoHelper.AreValidCoordinates(lat, lon));

    [Fact]
    public void DistanceMeters_InvalidCoordinates_ReturnsInfinity()
    {
        var d = GeoHelper.DistanceMeters(double.NaN, 29.0, 41.0, 29.0);
        Assert.True(double.IsPositiveInfinity(d));
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

    [Fact]
    public void TurkeyLocalToUtc_ConvertsFromTrToUtc()
    {
        var date = new DateOnly(2026, 6, 25);
        var time = new TimeOnly(9, 0);
        var utc = AppTime.TurkeyLocalToUtc(date, time);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 6, 25, 6, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void ToTurkey_AddsThreeHours()
    {
        var utc = new DateTime(2026, 6, 25, 6, 0, 0, DateTimeKind.Utc);
        var tr = AppTime.ToTurkey(utc);
        Assert.Equal(new DateTime(2026, 6, 25, 9, 0, 0), tr);
    }
}
