using System.Text;
using Microsoft.Extensions.Logging;
using SweetSecrets.Application.Common.Authentication;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class DevelopmentPasswordResetNotificationService : IPasswordResetNotificationService
{
    private readonly ILogger<DevelopmentPasswordResetNotificationService> _logger;

    public DevelopmentPasswordResetNotificationService(
        ILogger<DevelopmentPasswordResetNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string email, Uri resetUri, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(Path.GetTempPath(), "SweetSecrets", "password-recovery");
        Directory.CreateDirectory(directory);

        var fileName = $"password-reset-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.txt";
        var filePath = Path.Combine(directory, fileName);
        var content = $"Correo: {email}{Environment.NewLine}Enlace: {resetUri}{Environment.NewLine}";

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);

        _logger.LogInformation(
            "Instrucciones de recuperación guardadas en la bandeja local de Development: {FilePath}",
            filePath);
    }
}
