namespace Ledajans.Shared;

public static class Departments
{
    public const string Teknik = "Teknik";
    public const string Depo = "Depo";
    public const string Satis = "Satış";
    public const string Muhasebe = "Muhasebe";

    public static readonly string[] All = [Teknik, Depo, Satis, Muhasebe];
}

public static class NonWorkingDayTypes
{
    public const string Holiday = "Holiday";
    public const string Leave = "Leave";
    public const string AnnualLeave = "AnnualLeave";
}
