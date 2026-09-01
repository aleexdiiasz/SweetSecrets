using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Application.Common.Email;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class EmailConfirmationService : IEmailConfirmationService
{
    private const string InvalidConfirmationMessage = "El enlace de confirmación no es válido o ya expiró.";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITransactionalEmailSender _emailSender;
    private readonly EmailConfirmationOptions _options;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        UserManager<ApplicationUser> userManager,
        ITransactionalEmailSender emailSender,
        IOptions<EmailConfirmationOptions> options,
        ILogger<EmailConfirmationService> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || user.EmailConfirmed || !user.IsActive || user.IsBlocked || string.IsNullOrWhiteSpace(user.Email))
            return;

        await SendSafelyAsync(user, cancellationToken);
    }

    public async Task RequestResendAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo electrónico es obligatorio.");

        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());

        if (user is null || user.EmailConfirmed || !user.IsActive || user.IsBlocked)
            return;

        await SendSafelyAsync(user, cancellationToken);
    }

    public async Task<EmailConfirmationResult> ConfirmAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return EmailConfirmationResult.Failed(InvalidConfirmationMessage);

        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());

        if (user is null || !user.IsActive || user.IsBlocked)
            return EmailConfirmationResult.Failed(InvalidConfirmationMessage);

        if (user.EmailConfirmed)
            return EmailConfirmationResult.Success();

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return EmailConfirmationResult.Failed(InvalidConfirmationMessage);
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        return result.Succeeded
            ? EmailConfirmationResult.Success()
            : EmailConfirmationResult.Failed(InvalidConfirmationMessage);
    }

    private async Task SendSafelyAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationUri = BuildConfirmationUri(user.Email!, encodedToken);

            await _emailSender.SendAsync(
                new TransactionalEmailMessage(
                    user.Email!,
                    "Confirma tu correo de SweetSecrets",
                    $"Abre este enlace para confirmar tu correo:{Environment.NewLine}{confirmationUri}",
                    "email-confirmation"),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No fue posible entregar las instrucciones de confirmación de correo.");
        }
    }

    private Uri BuildConfirmationUri(string email, string token)
    {
        if (!Uri.TryCreate(_options.ConfirmationPageBaseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("EmailConfirmation:ConfirmationPageBaseUrl no está configurado correctamente.");

        var url = QueryHelpers.AddQueryString(
            baseUri.ToString(),
            new Dictionary<string, string?> { ["email"] = email, ["token"] = token });

        return new Uri(url);
    }
}
