using MudBlazor;

namespace Ledajans.Client.Theme;

public static class LedajansTheme
{
    public const string Orange = "#f46f2c";
    public const string OrangeDark = "#d85a20";
    public const string OrangeLight = "#ff8a5c";

    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Orange,
            PrimaryDarken = OrangeDark,
            PrimaryLighten = OrangeLight,
            Secondary = "#2d3436",
            SecondaryDarken = "#1e2324",
            SecondaryLighten = "#636e72",
            AppbarBackground = Orange,
            AppbarText = "#ffffff",
            Background = "#faf7f5",
            BackgroundGray = "#f3eeea",
            Surface = "#ffffff",
            DrawerBackground = "#ffffff",
            DrawerText = "#2d3436",
            DrawerIcon = Orange,
            TextPrimary = "#2d3436",
            TextSecondary = "#636e72",
            ActionDefault = "#636e72",
            Success = "#27ae60",
            Warning = "#f39c12",
            Error = "#e74c3c",
            Info = Orange,
            LinesDefault = "#ece5df",
            TableLines = "#f0ebe6",
            Divider = "#ece5df",
            HoverOpacity = 0.06
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
            AppbarHeight = "64px",
            DrawerWidthLeft = "280px"
        }
    };
}
