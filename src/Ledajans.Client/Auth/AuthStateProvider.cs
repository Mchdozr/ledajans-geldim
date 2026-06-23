using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Ledajans.Client.Auth;

public class AuthStateProvider : AuthenticationStateProvider
{
    public const string TokenKey = "ledajans_token";
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public AuthStateProvider(ILocalStorageService localStorage) => _localStorage = localStorage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return _anonymous;

        var claims = ParseClaims(token);
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (expClaim is not null && long.TryParse(expClaim, out var exp))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expiry < DateTimeOffset.UtcNow)
            {
                await _localStorage.RemoveItemAsync(TokenKey);
                return _anonymous;
            }
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyLogin(string token)
    {
        await _localStorage.SetItemAsStringAsync(TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogout()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    private static IEnumerable<Claim> ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return Enumerable.Empty<Claim>();

        var payload = Decode(parts[1]);
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
        if (data is null) return Enumerable.Empty<Claim>();

        var claims = new List<Claim>();
        foreach (var kvp in data)
        {
            if (kvp.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in kvp.Value.EnumerateArray())
                    claims.Add(new Claim(Map(kvp.Key), item.ToString()));
            }
            else
            {
                claims.Add(new Claim(Map(kvp.Key), kvp.Value.ToString()));
            }
        }
        return claims;
    }

    private static string Map(string type) => type switch
    {
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => ClaimTypes.NameIdentifier,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => ClaimTypes.Name,
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
        "role" => ClaimTypes.Role,
        _ => type
    };

    private static string Decode(string base64Url)
    {
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
