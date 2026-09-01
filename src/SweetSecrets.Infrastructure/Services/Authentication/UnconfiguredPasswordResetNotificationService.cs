using SweetSecrets.Application.Common.Authentication;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class UnconfiguredPasswordResetNotificationService : IPasswordResetNotificationService
{
    public Task SendAsync(string email, Uri resetUri, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "No existe un proveedor de correo configurado para recuperación de contraseña.");
    }
}
