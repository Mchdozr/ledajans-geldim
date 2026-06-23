using System.Globalization;

namespace Ledajans.Client;

public static class TurkishFormat
{
    private static readonly Lazy<CultureInfo> _culture = new(() =>
    {
        try { return CultureInfo.GetCultureInfo("tr-TR"); }
        catch { return CultureInfo.InvariantCulture; }
    });

    public static CultureInfo Culture => _culture.Value;

    public static string FormatDateTime(DateTime? value, string format = "dd MMMM yyyy, HH:mm")
        => value?.ToString(format, Culture) ?? string.Empty;

    public static string FormatDate(DateOnly value, string format = "dd.MM.yyyy")
        => value.ToString(format, Culture);

    public static string FormatTimeLocal(DateTime utc, string format = "HH:mm")
        => utc.ToLocalTime().ToString(format, Culture);
}
