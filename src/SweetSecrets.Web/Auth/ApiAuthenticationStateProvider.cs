using Microsoft.AspNetCore.Components.Authorization;
using SweetSecrets.Contracts.Auth;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SweetSecrets.Web.Auth;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public ApiAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response =
                await _httpClient.GetAsync(
                    "api/auth/me");

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return CreateAnonymousState();
            }

            if (!response.IsSuccessStatusCode)
            {
                return CreateAnonymousState();
            }

            var currentUser =
                await response.Content
                    .ReadFromJsonAsync<CurrentUserResponse>();

            if (currentUser is null)
            {
                return CreateAnonymousState();
            }

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    currentUser.UserId.ToString()),

                new(
                    ClaimTypes.Email,
                    currentUser.Email),

                new(
                    ClaimTypes.Name,
                    currentUser.Email)
            };

            if (currentUser.TenantId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "tenant_id",
                        currentUser.TenantId.Value.ToString()));
            }

            if (currentUser.SessionId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "session_id",
                        currentUser.SessionId.Value.ToString()));
            }

            foreach (var role in currentUser.Roles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var identity =
                new ClaimsIdentity(
                    claims,
                    authenticationType:
                        "SweetSecrets.Cookie");

            var principal =
                new ClaimsPrincipal(identity);

            return new AuthenticationState(
                principal);
        }
        catch (HttpRequestException)
        {
            return CreateAnonymousState();
        }
    }

    public void NotifyAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState CreateAnonymousState()
    {
        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity());

        return new AuthenticationState(
            principal);
    }
}