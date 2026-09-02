using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SweetSecrets.Infrastructure.Services.Email;

public interface ISmtpTransport
{
    Task SendAsync(SmtpEnvelope message, SmtpOptions options, CancellationToken cancellationToken = default);
}

public sealed record SmtpEnvelope(string FromEmail, string FromName, string ToEmail, string Subject, string TextBody);

public sealed class MailKitSmtpTransport : ISmtpTransport
{
    public async Task SendAsync(SmtpEnvelope envelope, SmtpOptions options, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(envelope.FromName, envelope.FromEmail));
        message.To.Add(MailboxAddress.Parse(envelope.ToEmail));
        message.Subject = envelope.Subject;
        message.Body = new TextPart("plain") { Text = envelope.TextBody };
        using var client = new SmtpClient();
        var socketOptions = options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(options.Host, options.Port, socketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(options.Username))
            await client.AuthenticateAsync(options.Username, options.Password!, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
