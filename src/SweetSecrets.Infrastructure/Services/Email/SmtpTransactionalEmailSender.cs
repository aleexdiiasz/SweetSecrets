using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Email;

namespace SweetSecrets.Infrastructure.Services.Email;

public sealed class SmtpTransactionalEmailSender(
    IOptions<SmtpOptions> options,
    ISmtpTransport transport,
    ILogger<SmtpTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        try
        {
            var envelope = new SmtpEnvelope(_options.FromEmail, _options.FromName,
                message.Recipient, message.Subject, message.TextBody);
            await transport.SendAsync(envelope, _options, cancellationToken);
            logger.LogInformation("Email transaccional {Category} entregado mediante SMTP.", message.Category);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            logger.LogWarning("Falló la entrega SMTP del email transaccional {Category}.", message.Category);
            throw new InvalidOperationException("No fue posible entregar el email transaccional.");
        }
    }
}
