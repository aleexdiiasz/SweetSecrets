using System.Net.Http.Json;
using System.Text.Json;
using System.Net;
using SweetSecrets.Contracts.Auth;
using SweetSecrets.Web.Auth;

namespace SweetSecrets.Web.Services.Auth;

public sealed class AuthApiClient
{
    internal const string TooManyRequestsMessage = "Has realizado demasiados intentos. Intenta nuevamente en unos minutos.";

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
                var error = await ReadLoginErrorAsync(response, cancellationToken);
                return LoginAttemptResult.Failed(error.Message, error.ErrorCode);
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

    public async Task<EmailConfirmationAttemptResult> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/resend-confirmation", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(response, "No fue posible procesar la solicitud.", cancellationToken);
                return EmailConfirmationAttemptResult.Failed(error);
            }

            var result = await response.Content.ReadFromJsonAsync<ResendEmailConfirmationResponse>(cancellationToken: cancellationToken);
            return result is null
                ? EmailConfirmationAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : EmailConfirmationAttemptResult.Success(result.Message);
        }
        catch (HttpRequestException)
        {
            return EmailConfirmationAttemptResult.Failed("No fue posible conectar con el servidor.");
        }
    }

    public async Task<EmailConfirmationAttemptResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/confirm-email", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(response, "El enlace de confirmación no es válido o ya expiró.", cancellationToken);
                return EmailConfirmationAttemptResult.Failed(error);
            }

            var result = await response.Content.ReadFromJsonAsync<ConfirmEmailResponse>(cancellationToken: cancellationToken);
            return result is null
                ? EmailConfirmationAttemptResult.Failed("No fue posible validar la respuesta del servidor.")
                : EmailConfirmationAttemptResult.Success(result.Message);
        }
        catch (HttpRequestException)
        {
            return EmailConfirmationAttemptResult.Failed("No fue posible conectar con el servidor.");
        }
    }

    private static async Task<(string Message, string? ErrorCode)> ReadLoginErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return (TooManyRequestsMessage, "RATE_LIMITED");

        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            var root = document.RootElement;
            var message = TryGetString(root, "message") ?? TryGetString(root, "Message") ?? "Correo o contraseña incorrectos.";
            var errorCode = TryGetString(root, "errorCode") ?? TryGetString(root, "ErrorCode");
            return (message, errorCode);
        }
        catch (JsonException)
        {
            return ("Correo o contraseña incorrectos.", null);
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    private static Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        ReadErrorMessageAsync(response, "Correo o contraseña incorrectos.", cancellationToken);

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return TooManyRequestsMessage;

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

public sealed record LoginAttemptResult(bool Succeeded, LoginResponse? Response, string? ErrorMessage, string? ErrorCode)
{
    public static LoginAttemptResult Success(
        LoginResponse response)
    {
        return new LoginAttemptResult(
            true,
            response,
            null,
            null);
    }

    public static LoginAttemptResult Failed(string message, string? errorCode = null)
    {
        return new LoginAttemptResult(
            false,
            null,
            message,
            errorCode);
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

public sealed record EmailConfirmationAttemptResult(bool Succeeded, string? Message, string? ErrorMessage)
{
    public static EmailConfirmationAttemptResult Success(string message) => new(true, message, null);
    public static EmailConfirmationAttemptResult Failed(string message) => new(false, null, message);
}
