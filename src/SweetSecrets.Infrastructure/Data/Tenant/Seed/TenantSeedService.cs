using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Domain.Entities.Tenant;
using System.Text.Json;

namespace SweetSecrets.Infrastructure.Data.Tenant.Seed;

public class TenantSeedService : ITenantSeedService
{
    public async Task SeedAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        await using var dbContext = new TenantDbContext(optionsBuilder.Options);

        await SeedUnitsAsync(dbContext, cancellationToken);

        await SeedSettingsAsync(dbContext, cancellationToken);

        await SeedProductsAsync(dbContext, cancellationToken);
    }

    private static async Task SeedUnitsAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Units.AnyAsync(cancellationToken))
        {
            return;
        }

        var units = new[]
        {
            new Unit
            {
                Id = Guid.NewGuid(),
                Code = "GR",
                Name = "Gramo",
                Symbol = "g",
                IsActive = true
            },

            new Unit
            {
                Id = Guid.NewGuid(),
                Code = "KG",
                Name = "Kilogramo",
                Symbol = "kg",
                IsActive = true
            },

            new Unit
            {
                Id = Guid.NewGuid(),
                Code = "ML",
                Name = "Mililitro",
                Symbol = "ml",
                IsActive = true
            },

            new Unit
            {
                Id = Guid.NewGuid(),
                Code = "L",
                Name = "Litro",
                Symbol = "l",
                IsActive = true
            },

            new Unit
            {
                Id = Guid.NewGuid(),
                Code = "PZA",
                Name = "Pieza",
                Symbol = "pza",
                IsActive = true
            }
        };

        await dbContext.Units.AddRangeAsync(units, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSettingsAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Settings.AnyAsync(cancellationToken))
        {
            return;
        }

        var settings = new[]
        {
            new TenantSetting
            {
                Id = Guid.NewGuid(),

                Key = "MULTIPLIER",
                Value = "3",

                Description =
                    "Multiplicador general utilizado para calcular el precio sugerido de las recetas.",

                CreatedAt = DateTime.UtcNow
            }
        };

        await dbContext.Settings.AddRangeAsync(settings, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProductsAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        const string resourceName =
            "SweetSecrets.Infrastructure.Data.Tenant.Seed.Data.products.seed.json";

        await using var stream =
            typeof(TenantSeedService)
                .Assembly
                .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso '{resourceName}'.");

        var seedProducts =
            await JsonSerializer.DeserializeAsync<List<ProductSeedItem>>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No fue posible cargar el catálogo inicial de productos.");

        var units = await dbContext.Units
            .ToDictionaryAsync(
                x => x.Code,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        var products = new List<Product>();

        foreach (var item in seedProducts)
        {
            if (!units.TryGetValue(
                    item.UnitCode,
                    out var unit))
            {
                throw new InvalidOperationException(
                    $"Unidad no encontrada para '{item.Name}': {item.UnitCode}.");
            }

            if (item.PurchaseQuantity <= 0)
            {
                throw new InvalidOperationException(
                    $"Cantidad inválida para '{item.Name}'.");
            }

            var unitCost =
                Math.Round(
                    item.PurchasePrice / item.PurchaseQuantity,
                    6,
                    MidpointRounding.AwayFromZero);

            products.Add(
                new Product
                {
                    Id = Guid.NewGuid(),

                    Name = item.Name.Trim(),

                    PurchaseQuantity = item.PurchaseQuantity,

                    UnitId = unit.Id,

                    PurchasePrice = item.PurchasePrice,

                    UnitCost = unitCost,

                    IsActive = true,

                    CreatedAt = DateTime.UtcNow
                });
        }

        await dbContext.Products.AddRangeAsync(products, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}