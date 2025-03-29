using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SecureChat.Client.Services;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private string _token;

    public async Task SetTokenAsync(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "user"),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}