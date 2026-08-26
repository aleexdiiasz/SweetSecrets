using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class TenantProvisioningService
    : ITenantProvisioningService
{
    private readonly ITenantRegistryService _registryService;
    private readonly ITenantDatabaseManager _databaseManager;
    private readonly IPlatformAuditService _auditService;
    private readonly TenantDatabaseOptions _databaseOptions;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly ITenantSeedService _seedService;


    public TenantProvisioningService(
        ITenantRegistryService registryService,
        ITenantDatabaseManager databaseManager,
        IPlatformAuditService auditService,
        IOptions<TenantDatabaseOptions> databaseOptions,
        ILogger<TenantProvisioningService> logger,
        ITenantSeedService seedService)
    {
        _registryService = registryService;
        _databaseManager = databaseManager;
        _auditService = auditService;
        _databaseOptions = databaseOptions.Value;
        _seedService = seedService;
        _logger = logger;
    }

    public async Task<TenantProvisioningResult> ProvisionAsync(string tenantName, CancellationToken cancellationToken = default)
    {
        var registration =
            await _registryService.CreateProvisioningAsync(
                tenantName,
                cancellationToken);

        try
        {
            await _databaseManager.CreateAsync(registration.DatabaseName, cancellationToken);

            await ApplyTenantMigrationsAsync(registration.DatabaseName, cancellationToken);

            var tenantConnectionString = BuildTenantConnectionString(registration.DatabaseName);

            await _seedService.SeedAsync(tenantConnectionString, cancellationToken);

            await _registryService.MarkActiveAsync(registration.TenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkFailedSafelyAsync(
                registration,
                ex,
                cancellationToken);

            throw new InvalidOperationException(
                $"No fue posible aprovisionar el tenant '{registration.Code}'.",
                ex);
        }

        await RegisterSuccessAuditSafelyAsync(
            registration,
            cancellationToken);

        return new TenantProvisioningResult(
            registration.TenantId,
            registration.Code,
            registration.Name,
            registration.DatabaseName);
    }

    private async Task ApplyTenantMigrationsAsync(string databaseName, CancellationToken cancellationToken)
    {
        var connectionString =
            BuildTenantConnectionString(databaseName);

        var optionsBuilder =
            new DbContextOptionsBuilder<TenantDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsAssembly(
                    typeof(TenantDbContext)
                        .Assembly
                        .FullName);
            });

        await using var dbContext =
            new TenantDbContext(
                optionsBuilder.Options);

        await dbContext.Database.MigrateAsync(
            cancellationToken);
    }

    private string BuildTenantConnectionString(string databaseName)
    {
        var builder =
            new NpgsqlConnectionStringBuilder(
                _databaseOptions.AdminConnectionString)
            {
                Database = databaseName
            };

        return builder.ConnectionString;
    }

    private async Task MarkFailedSafelyAsync(TenantRegistrationResult registration, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            await _registryService.MarkFailedAsync(
                registration.TenantId,
                cancellationToken);
        }
        catch (Exception statusException)
        {
            _logger.LogError(
                statusException,
                "No fue posible marcar como Failed el tenant {TenantId}.",
                registration.TenantId);
        }

        try
        {
            await _auditService.RegisterAsync(
                new PlatformAuditEntry
                {
                    TenantId = registration.TenantId,

                    Action = "TENANT_FAILED",
                    Entity = "TENANT",
                    EntityId =
                        registration.TenantId.ToString(),

                    Description =
                        $"Falló el provisioning del tenant {registration.Code}.",

                    NewValues =
                        JsonSerializer.Serialize(
                            new
                            {
                                registration.Code,
                                registration.DatabaseName,
                                Error = exception.Message
                            })
                },
                cancellationToken);
        }
        catch (Exception auditException)
        {
            _logger.LogError(
                auditException,
                "No fue posible registrar TENANT_FAILED para {TenantId}.",
                registration.TenantId);
        }
    }

    private async Task RegisterSuccessAuditSafelyAsync(TenantRegistrationResult registration, CancellationToken cancellationToken)
    {
        try
        {
            await _auditService.RegisterAsync(
                new PlatformAuditEntry
                {
                    TenantId = registration.TenantId,

                    Action = "TENANT_CREATED",
                    Entity = "TENANT",
                    EntityId =
                        registration.TenantId.ToString(),

                    Description =
                        $"Tenant {registration.Code} creado correctamente.",

                    NewValues =
                        JsonSerializer.Serialize(
                            new
                            {
                                registration.Code,
                                registration.Name,
                                registration.DatabaseName
                            })
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "El tenant {TenantId} fue creado, pero falló su auditoría.",
                registration.TenantId);
        }
    }
}