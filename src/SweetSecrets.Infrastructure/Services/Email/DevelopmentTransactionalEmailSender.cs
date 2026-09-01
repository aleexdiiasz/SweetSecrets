using System.Text;
using Microsoft.Extensions.Logging;
using SweetSecrets.Application.Common.Email;

namespace SweetSecrets.Infrastructure.Services.Email;

public sealed class DevelopmentTransactionalEmailSender : ITransactionalEmailSender
{
    private readonly ILogger<DevelopmentTransactionalEmailSender> _logger;

    public DevelopmentTransactionalEmailSender(ILogger<DevelopmentTransactionalEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(Path.GetTempPath(), "SweetSecrets", "email");
        Directory.CreateDirectory(directory);

        var safeCategory = string.Concat(message.Category.Where(character => char.IsLetterOrDigit(character) || character == '-'));
        var fileName = $"{safeCategory}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.txt";
        var filePath = Path.Combine(directory, fileName);
        var content = $"Para: {message.Recipient}{Environment.NewLine}Asunto: {message.Subject}{Environment.NewLine}{Environment.NewLine}{message.TextBody}{Environment.NewLine}";

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);

        _logger.LogInformation("Email transaccional Development guardado en {FilePath}", filePath);
    }
}
