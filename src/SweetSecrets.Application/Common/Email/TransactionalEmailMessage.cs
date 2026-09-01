namespace SweetSecrets.Application.Common.Email;

public sealed record TransactionalEmailMessage(
    string Recipient,
    string Subject,
    string TextBody,
    string Category);
