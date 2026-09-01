using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetNotificationService _notificationService;
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<PasswordRecoveryService> _logger;

    public PasswordRecoveryService(
        UserManager<ApplicationUser> userManager,
        IPasswordResetNotificationService notificationService,
        IOptions<PasswordRecoveryOptions> options,
        ILogger<PasswordRecoveryService> logger)
    {
        _userManager = userManager;
        _notificationService = notificationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RequestResetAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo electrónico es obligatorio.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null || !user.IsActive || user.IsBlocked)
            return;

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUri = BuildResetUri(normalizedEmail, encodedToken);

            await _notificationService.SendAsync(normalizedEmail, resetUri, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "No fue posible entregar las instrucciones de recuperación de contraseña.");
        }
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return PasswordResetResult.Failed("El enlace de recuperación no es válido o ya expiró.");

        if (string.IsNullOrWhiteSpace(newPassword))
            return PasswordResetResult.Failed("La nueva contraseña es obligatoria.");

        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());

        if (user is null || !user.IsActive || user.IsBlocked)
            return PasswordResetResult.Failed("El enlace de recuperación no es válido o ya expiró.");

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return PasswordResetResult.Failed("El enlace de recuperación no es válido o ya expiró.");
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

        if (result.Succeeded)
            return PasswordResetResult.Success();

        if (result.Errors.Any(error => error.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase)))
            return PasswordResetResult.Failed("El enlace de recuperación no es válido o ya expiró.");

        var errors = string.Join(" ", result.Errors.Select(error => error.Description));

        return PasswordResetResult.Failed(
            string.IsNullOrWhiteSpace(errors)
                ? "La nueva contraseña no cumple los requisitos de seguridad."
                : errors);
    }

    private Uri BuildResetUri(string email, string token)
    {
        if (!Uri.TryCreate(_options.ResetPageBaseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("PasswordRecovery:ResetPageBaseUrl no está configurado correctamente.");

        var url = QueryHelpers.AddQueryString(
            baseUri.ToString(),
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["token"] = token
            });

        return new Uri(url);
    }
}
