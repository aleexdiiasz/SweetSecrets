using Microsoft.Extensions.Logging.Abstractions;
using SweetSecrets.Application.Common.Email;
using SweetSecrets.Infrastructure.Services.Email;

namespace SweetSecrets.UnitTests;

public sealed class DevelopmentTransactionalEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WritesTransactionalEmailToDevelopmentPickupDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SweetSecrets", "email");
        Directory.CreateDirectory(directory);
        var existingFiles = Directory.GetFiles(directory).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sender = new DevelopmentTransactionalEmailSender(NullLogger<DevelopmentTransactionalEmailSender>.Instance);
        var message = new TransactionalEmailMessage(
            "test@example.com",
            "Confirma tu correo",
            "https://localhost:7011/confirm-email?token=test",
            "email-confirmation");

        await sender.SendAsync(message);

        var createdFile = Directory.GetFiles(directory).Single(file => !existingFiles.Contains(file));
        try
        {
            var content = await File.ReadAllTextAsync(createdFile);
            Assert.Contains(message.Recipient, content, StringComparison.Ordinal);
            Assert.Contains(message.Subject, content, StringComparison.Ordinal);
            Assert.Contains(message.TextBody, content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(createdFile);
        }
    }
}
