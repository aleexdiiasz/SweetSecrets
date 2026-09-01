using SweetSecrets.Application.Common.Email;

namespace SweetSecrets.Infrastructure.Services.Email;

public sealed class UnconfiguredTransactionalEmailSender : ITransactionalEmailSender
{
    public Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("No existe un proveedor de email transaccional configurado para este ambiente.");
    }
}
