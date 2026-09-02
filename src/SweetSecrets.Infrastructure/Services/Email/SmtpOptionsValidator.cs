using Microsoft.Extensions.Options;
using MimeKit;

namespace SweetSecrets.Infrastructure.Services.Email;

public sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Host)) errors.Add("Email:Smtp:Host es obligatorio.");
        if (options.Port is < 1 or > 65535) errors.Add("Email:Smtp:Port debe estar entre 1 y 65535.");
        if (!IsValidEmail(options.FromEmail))
            errors.Add("Email:Smtp:FromEmail debe ser un correo válido.");
        if (string.IsNullOrWhiteSpace(options.FromName)) errors.Add("Email:Smtp:FromName es obligatorio.");
        if (string.IsNullOrWhiteSpace(options.Username) != string.IsNullOrWhiteSpace(options.Password))
            errors.Add("Email:Smtp:Username y Email:Smtp:Password deben configurarse juntos cuando SMTP requiere autenticación.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MailboxAddress.TryParse(value, out _);
}
