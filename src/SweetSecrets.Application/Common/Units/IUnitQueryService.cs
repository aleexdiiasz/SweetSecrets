namespace SweetSecrets.Application.Common.Units;

public interface IUnitQueryService
{
    Task<IReadOnlyList<UnitListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);
}