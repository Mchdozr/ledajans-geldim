using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Ledajans.Client.Services;
using Ledajans.Shared;
using Microsoft.AspNetCore.Components;

namespace Ledajans.Client.Auth;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthStateProvider _authState;
    private readonly NavigationManager _navigation;

    public AuthHeaderHandler(
        ILocalStorageService localStorage,
        AuthStateProvider authState,
        NavigationManager navigation)
    {
        _localStorage = localStorage;
        _authState = authState;
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync(AuthStateProvider.TokenKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            token = token.Trim().Trim('"');
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var locationCode = await _localStorage.GetItemAsStringAsync(LocationService.CodeKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            locationCode = locationCode.Trim().Trim('"');
            request.Headers.TryAddWithoutValidation(LocationHeaders.Name, locationCode);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !request.RequestUri?.AbsolutePath.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _authState.NotifyLogout();
            if (!_navigation.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = Uri.EscapeDataString(new Uri(_navigation.Uri).PathAndQuery);
                _navigation.NavigateTo($"/login?returnUrl={returnUrl}");
            }
        }

        return response;
    }
}
