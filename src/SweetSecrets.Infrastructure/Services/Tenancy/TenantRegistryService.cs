using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;
using MasterTenant = SweetSecrets.Domain.Entities.Master.Tenant;
using Microsoft.EntityFrameworkCore;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class TenantRegistryService : ITenantRegistryService
{
    private readonly MasterDbContext _dbContext;
    private readonly ITenantIdentifierGenerator _identifierGenerator;

    public TenantRegistryService(MasterDbContext dbContext, ITenantIdentifierGenerator identifierGenerator)
    {
        _dbContext = dbContext;
        _identifierGenerator = identifierGenerator;
    }

    public async Task<TenantRegistrationResult> CreateProvisioningAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre del tenant es obligatorio.",
                nameof(name));
        }

        name = name.Trim();

        if (name.Length > 200)
        {
            throw new ArgumentException(
                "El nombre del tenant no puede superar 200 caracteres.",
                nameof(name));
        }

        var identifier =
            await _identifierGenerator.GenerateAsync(
                cancellationToken);

        var tenant = new MasterTenant
        {
            Id = Guid.NewGuid(),

            Code = identifier.Code,

            Name = name,

            DatabaseName = identifier.DatabaseName,

            Status = TenantStatus.Provisioning,

            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Tenants.AddAsync(
            tenant,
            cancellationToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new TenantRegistrationResult(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.DatabaseName);
    }

    public async Task MarkActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(
                x => x.Id == tenantId,
                cancellationToken);

        if (tenant is null)
            throw new InvalidOperationException("Tenant no encontrado.");

        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(
                x => x.Id == tenantId,
                cancellationToken);

        if (tenant is null)
            return;

        tenant.Status = TenantStatus.Failed;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}