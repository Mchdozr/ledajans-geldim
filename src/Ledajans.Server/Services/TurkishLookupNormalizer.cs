using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace Ledajans.Server.Services;

public class TurkishLookupNormalizer : ILookupNormalizer
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public string? NormalizeName(string? name)
        => name?.ToUpper(Turkish);

    public string? NormalizeEmail(string? email)
        => email?.ToUpperInvariant();
}
