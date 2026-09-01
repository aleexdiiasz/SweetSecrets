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

    public async Task<PasswordRecoveryAttemptResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/forgot-password",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(
                    response,
                    "No fue posible procesar la solicitud.",
                    cancellationToken);

                return PasswordRecoveryAttemptResult.Failed(error);
            }

            var result = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(
                cancellationToken: cancellationToken);

            return result is null
                ? PasswordRecoveryAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : PasswordRecoveryAttemptResult.Success(result.Message);
        }
        catch (HttpRequestException)
        {
            return PasswordRecoveryAttemptResult.Failed("No fue posible conectar con el servidor.");
        }
    }

    public async Task<PasswordRecoveryAttemptResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/reset-password",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(
                    response,
                    "El enlace no es válido o la contraseña no cumple los requisitos.",
                    cancellationToken);

                return PasswordRecoveryAttemptResult.Failed(error);
            }

            var result = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>(
                cancellationToken: cancellationToken);

            return result is null
                ? PasswordRecoveryAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : PasswordRecoveryAttemptResult.Success(result.Message);
        }
        catch (HttpRequestException)
        {
            return PasswordRecoveryAttemptResult.Failed("No fue posible conectar con el servidor.");
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

    public async Task<AccountAttemptResult> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/auth/account", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(response, "No fue posible cargar la información de la cuenta.", cancellationToken);
                return AccountAttemptResult.Failed(error);
            }

            var account = await response.Content.ReadFromJsonAsync<AccountResponse>(cancellationToken: cancellationToken);
            return account is null
                ? AccountAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : AccountAttemptResult.Success(account);
        }
        catch (HttpRequestException)
        {
            return AccountAttemptResult.Failed("No fue posible conectar con el servidor.");
        }
    }

    public async Task<ChangePasswordAttemptResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(response, "No fue posible cambiar la contraseña.", cancellationToken);
                return ChangePasswordAttemptResult.Failed(error);
            }

            var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>(cancellationToken: cancellationToken);
            return result is null
                ? ChangePasswordAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : ChangePasswordAttemptResult.Success(result.Message);
        }
        catch (HttpRequestException)
        {
            return ChangePasswordAttemptResult.Failed("No fue posible conectar con el servidor.");
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

public sealed record PasswordRecoveryAttemptResult(bool Succeeded, string? Message, string? ErrorMessage)
{
    public static PasswordRecoveryAttemptResult Success(string message) =>
        new(true, message, null);

    public static PasswordRecoveryAttemptResult Failed(string message) =>
        new(false, null, message);
}

public sealed record AccountAttemptResult(bool Succeeded, AccountResponse? Account, string? ErrorMessage)
{
    public static AccountAttemptResult Success(AccountResponse account) => new(true, account, null);
    public static AccountAttemptResult Failed(string message) => new(false, null, message);
}

public sealed record ChangePasswordAttemptResult(bool Succeeded, string? Message, string? ErrorMessage)
{
    public static ChangePasswordAttemptResult Success(string message) => new(true, message, null);
    public static ChangePasswordAttemptResult Failed(string message) => new(false, null, message);
}
