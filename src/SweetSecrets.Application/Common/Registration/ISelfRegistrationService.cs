namespace SweetSecrets.Application.Common.Registration;

public interface ISelfRegistrationService
{
    Task<SelfRegistrationResult> RegisterAsync(SelfRegistrationCommand command, CancellationToken cancellationToken = default);
}