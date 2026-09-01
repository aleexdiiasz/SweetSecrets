namespace SweetSecrets.Application.Common.Email;

public interface ITransactionalEmailSender
{
    Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default);
}
