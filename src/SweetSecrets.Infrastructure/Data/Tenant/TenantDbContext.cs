using Microsoft.EntityFrameworkCore;
using SweetSecrets.Domain.Entities.Tenant;

namespace SweetSecrets.Infrastructure.Data.Tenant;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();

    public DbSet<TenantSetting> Settings => Set<TenantSetting>();

    public DbSet<ProductPriceHistory> ProductPriceHistory =>
        Set<ProductPriceHistory>();

    public DbSet<RecipeCostHistory> RecipeCostHistory =>
        Set<RecipeCostHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUnits(builder);
        ConfigureProducts(builder);
        ConfigureRecipes(builder);
        ConfigureRecipeItems(builder);
        ConfigureSettings(builder);
        ConfigureProductPriceHistory(builder);
        ConfigureRecipeCostHistory(builder);
    }

    private static void ConfigureUnits(ModelBuilder builder)
    {
        builder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Symbol)
                .HasMaxLength(20)
                .IsRequired();
        });
    }

    private static void ConfigureProducts(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Name);

            entity.Property(x => x.PurchaseQuantity)
                .HasPrecision(18, 4);

            entity.Property(x => x.PurchasePrice)
                .HasPrecision(18, 4);

            entity.Property(x => x.UnitCost)
                .HasPrecision(18, 6);

            entity.HasOne<Unit>()
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRecipes(ModelBuilder builder)
    {
        builder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Name);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Multiplier)
                .HasPrecision(18, 4);

            entity.Property(x => x.TotalCost)
                .HasPrecision(18, 4);

            entity.Property(x => x.SuggestedPrice)
                .HasPrecision(18, 4);
        });
    }

    private static void ConfigureRecipeItems(ModelBuilder builder)
    {
        builder.Entity<RecipeItem>(entity =>
        {
            entity.ToTable("recipe_items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            entity.Property(x => x.UnitCost)
                .HasPrecision(18, 6);

            entity.Property(x => x.TotalCost)
                .HasPrecision(18, 4);

            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.RecipeId);
            entity.HasIndex(x => x.ProductId);
        });
    }

    private static void ConfigureSettings(ModelBuilder builder)
    {
        builder.Entity<TenantSetting>(entity =>
        {
            entity.ToTable("settings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Key)
                .IsUnique();

            entity.Property(x => x.Value)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);
        });
    }

    private static void ConfigureProductPriceHistory(ModelBuilder builder)
    {
        builder.Entity<ProductPriceHistory>(entity =>
        {
            entity.ToTable("product_price_history");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PreviousPrice)
                .HasPrecision(18, 4);

            entity.Property(x => x.NewPrice)
                .HasPrecision(18, 4);

            entity.Property(x => x.PreviousUnitCost)
                .HasPrecision(18, 6);

            entity.Property(x => x.NewUnitCost)
                .HasPrecision(18, 6);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.ChangedAt);
        });
    }

    private static void ConfigureRecipeCostHistory(ModelBuilder builder)
    {
        builder.Entity<RecipeCostHistory>(entity =>
        {
            entity.ToTable("recipe_cost_history");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PreviousCost)
                .HasPrecision(18, 4);

            entity.Property(x => x.NewCost)
                .HasPrecision(18, 4);

            entity.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasOne(x => x.Recipe)
                .WithMany()
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.RecipeId);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}