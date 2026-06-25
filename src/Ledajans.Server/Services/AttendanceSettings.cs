namespace Ledajans.Server.Services;

public class AttendanceSettings
{
    public const string SectionName = "Attendance";

    public int LateCheckInHour { get; set; } = 9;
    public int LateCheckInMinute { get; set; } = 15;
    public double MaxGpsAccuracyMeters { get; set; } = 30;

    public TimeOnly LateThreshold => new(LateCheckInHour, LateCheckInMinute);

    public bool IsLate(DateTime checkInUtc)
        => AppTime.TimeInTurkey(checkInUtc) > LateThreshold;
}
