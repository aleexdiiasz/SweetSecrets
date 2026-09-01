using System.Net.Http.Json;
using System.Text.Json;
using SweetSecrets.Contracts.Auth;
using SweetSecrets.Web.Auth;

namespace SweetSecrets.Web.Services.Auth;

public sealed class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiAuthenticationStateProvider _authenticationStateProvider;

    public AuthApiClient(HttpClient httpClient, ApiAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<LoginAttemptResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message =
                    await ReadErrorMessageAsync(
                        response,
                        cancellationToken);

                return LoginAttemptResult.Failed(
                    message);
            }

            var loginResponse =
                await response.Content
                    .ReadFromJsonAsync<LoginResponse>(
                        cancellationToken: cancellationToken);

            if (loginResponse is null)
            {
                return LoginAttemptResult.Failed(
                    "No fue posible validar la respuesta de inicio de sesión.");
            }

            _authenticationStateProvider
                .NotifyAuthenticationChanged();

            return LoginAttemptResult.Success(
                loginResponse);
        }
        catch (HttpRequestException)
        {
            return LoginAttemptResult.Failed(
                "No fue posible conectar con el servidor.");
        }
    }

    public async Task<RegistrationAttemptResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(
                    response,
                    "No fue posible crear la cuenta.",
                    cancellationToken);

                return RegistrationAttemptResult.Failed(message);
            }

            var registerResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>(
                cancellationToken: cancellationToken);

            return registerResponse is null
                ? RegistrationAttemptResult.Failed("No fue posible validar la respuesta del registro.")
                : RegistrationAttemptResult.Success(registerResponse);
        }
        catch (HttpRequestException)
        {
            return RegistrationAttemptResult.Failed("No fue posible conectar con el servidor.");
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsync(
                "api/auth/logout",
                content: null,
                cancellationToken);
        }
        finally
        {
            _authenticationStateProvider
                .NotifyAuthenticationChanged();
        }
    }

    private static Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        ReadErrorMessageAsync(response, "Correo o contraseña incorrectos.", cancellationToken);

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var document =
                await JsonDocument.ParseAsync(
                    await response.Content
                        .ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var messageElement))
            {
                return messageElement.GetString()
                    ?? fallbackMessage;
            }

            if (document.RootElement.TryGetProperty(
                    "Message",
                    out messageElement))
            {
                return messageElement.GetString()
                    ?? fallbackMessage;
            }
        }
        catch (JsonException)
        {
        }

        return fallbackMessage;
    }
}

public sealed record LoginAttemptResult(bool Succeeded, LoginResponse? Response, string? ErrorMessage)
{
    public static LoginAttemptResult Success(
        LoginResponse response)
    {
        return new LoginAttemptResult(
            true,
            response,
            null);
    }

    public static LoginAttemptResult Failed(string message)
    {
        return new LoginAttemptResult(
            false,
            null,
            message);
    }
}

public sealed record RegistrationAttemptResult(bool Succeeded, RegisterResponse? Response, string? ErrorMessage)
{
    public static RegistrationAttemptResult Success(RegisterResponse response) =>
        new(true, response, null);

    public static RegistrationAttemptResult Failed(string message) =>
        new(false, null, message);
}
