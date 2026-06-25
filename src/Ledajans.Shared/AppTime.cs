namespace Ledajans.Shared;

public static class AppTime
{
    private static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.Add(TurkeyOffset));

    public static DateTime TodayDate => DateTime.UtcNow.Add(TurkeyOffset).Date;

    public static DateTime ToTurkey(DateTime utc) => DateTime.SpecifyKind(utc.Add(TurkeyOffset), DateTimeKind.Unspecified);

    public static TimeOnly TimeInTurkey(DateTime utc) => TimeOnly.FromDateTime(ToTurkey(utc));
}
