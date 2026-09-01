using Microsoft.Extensions.Logging.Abstractions;
using SweetSecrets.Infrastructure.Services.Authentication;

namespace SweetSecrets.UnitTests;

public class DevelopmentPasswordResetNotificationServiceTests
{
    [Fact]
    public async Task SendAsync_WritesResetInstructionsToDevelopmentPickupDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SweetSecrets", "password-recovery");
        Directory.CreateDirectory(directory);

        var existingFiles = Directory.GetFiles(directory).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var service = new DevelopmentPasswordResetNotificationService(
            NullLogger<DevelopmentPasswordResetNotificationService>.Instance);
        var resetUri = new Uri(
            "https://localhost:7011/reset-password?email=test%40example.com&token=safe-test-token");

        await service.SendAsync("test@example.com", resetUri);

        var createdFile = Directory.GetFiles(directory)
            .Single(file => !existingFiles.Contains(file));

        try
        {
            var content = await File.ReadAllTextAsync(createdFile);

            Assert.Contains("test@example.com", content, StringComparison.Ordinal);
            Assert.Contains(resetUri.ToString(), content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(createdFile);
        }
    }
}
