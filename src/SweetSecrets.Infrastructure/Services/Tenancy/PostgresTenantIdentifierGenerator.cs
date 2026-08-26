using System.Data;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class PostgresTenantIdentifierGenerator : ITenantIdentifierGenerator
{
    private readonly MasterDbContext _dbContext;

    public PostgresTenantIdentifierGenerator(MasterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantIdentifier> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();

        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText =
                "SELECT nextval('tenant_number_seq');";

            var result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (result is null || !long.TryParse(result.ToString(), out var number))
            {
                throw new InvalidOperationException(
                    "No fue posible generar el número del tenant.");
            }

            if (number > 999999)
            {
                throw new InvalidOperationException(
                    "Se agotó el rango disponible de identificadores tenant.");
            }

            var code = number.ToString("D6");

            return new TenantIdentifier(
                number,
                code,
                $"sweetsecrets_tenant_{code}");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}