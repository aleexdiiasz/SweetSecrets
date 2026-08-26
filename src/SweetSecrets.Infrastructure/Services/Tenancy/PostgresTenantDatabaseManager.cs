using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class PostgresTenantDatabaseManager : ITenantDatabaseManager
{
    private readonly TenantDatabaseOptions _options;

    public PostgresTenantDatabaseManager(IOptions<TenantDatabaseOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.AdminConnectionString))
        {
            throw new InvalidOperationException(
                "La conexión administrativa de PostgreSQL no está configurada.");
        }
    }

    public async Task<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ValidateDatabaseName(databaseName);

        await using var connection =
            new NpgsqlConnection(
                BuildAdministrativeConnectionString());

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM pg_database
                WHERE datname = @databaseName
            );
            """;

        command.Parameters.AddWithValue(
            "databaseName",
            databaseName);

        var result =
            await command.ExecuteScalarAsync(cancellationToken);

        return result is true;
    }

    public async Task CreateAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ValidateDatabaseName(databaseName);

        if (await ExistsAsync(databaseName, cancellationToken))
        {
            throw new InvalidOperationException(
                $"La base '{databaseName}' ya existe.");
        }

        await using var connection = new NpgsqlConnection(BuildAdministrativeConnectionString());

        await connection.OpenAsync(cancellationToken);

        var quotedDatabaseName =
            new NpgsqlCommandBuilder()
                .QuoteIdentifier(databaseName);

        await using var command = connection.CreateCommand();

        command.CommandText = $"CREATE DATABASE {quotedDatabaseName};";

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private string BuildAdministrativeConnectionString()
    {
        var builder =
            new NpgsqlConnectionStringBuilder(
                _options.AdminConnectionString)
            {
                Database = "postgres"
            };

        return builder.ConnectionString;
    }

    private void ValidateDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException(
                "El nombre de la base es obligatorio.",
                nameof(databaseName));
        }

        var escapedPrefix = Regex.Escape(_options.DatabasePrefix);

        var pattern = $"^{escapedPrefix}[0-9]{{6}}$";

        if (!Regex.IsMatch(
                databaseName,
                pattern,
                RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException(
                $"Nombre de base tenant inválido: '{databaseName}'.");
        }
    }
}