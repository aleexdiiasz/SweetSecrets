using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Domain.Entities.Master;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Auditing;

public class PlatformAuditService : IPlatformAuditService
{
    private readonly MasterDbContext _dbContext;

    public PlatformAuditService(MasterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterAsync(
        PlatformAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Action))
            throw new ArgumentException("Action es obligatorio.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.Entity))
            throw new ArgumentException("Entity es obligatorio.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.Description))
            throw new ArgumentException("Description es obligatorio.", nameof(entry));

        var auditLog = new PlatformAuditLog
        {
            Id = Guid.NewGuid(),

            UserId = entry.UserId,
            TenantId = entry.TenantId,

            Action = entry.Action.Trim(),
            Entity = entry.Entity.Trim(),
            EntityId = entry.EntityId,

            Description = entry.Description.Trim(),

            OldValues = entry.OldValues,
            NewValues = entry.NewValues,

            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,

            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.PlatformAuditLogs.AddAsync(
            auditLog,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}