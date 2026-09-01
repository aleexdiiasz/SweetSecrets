namespace SweetSecrets.Application.Common.Authentication;

public interface IAccountSessionRefresher
{
    Task RefreshAsync(Guid userId, CancellationToken cancellationToken = default);
}
