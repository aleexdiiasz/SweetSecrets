using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Domain.Entities.Master;
using SweetSecrets.Infrastructure.Identity;
using MasterTenant = SweetSecrets.Domain.Entities.Master.Tenant;

namespace SweetSecrets.Infrastructure.Data.Master;

public class MasterDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<MasterTenant> Tenants => Set<MasterTenant>();

    public DbSet<PlatformAuditLog> PlatformAuditLogs => Set<PlatformAuditLog>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureTenants(builder);
        ConfigureUserTenant(builder);
        ConfigurePlatformAuditLogs(builder);
        ConfigureUserSessions(builder);
    }

    private static void ConfigureTenants(ModelBuilder builder)
    {
        builder.Entity<MasterTenant>(entity =>
        {
            entity.ToTable("tenants");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.DatabaseName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.DatabaseName)
                .IsUnique();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });
    }

    private static void ConfigureUserTenant(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IsBlocked)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => x.TenantId);

            entity.HasOne<MasterTenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>()
            .ToTable("platform_users");

        builder.Entity<IdentityRole<Guid>>()
            .ToTable("platform_roles");

        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("platform_user_roles");

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("platform_user_claims");

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("platform_user_logins");

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("platform_role_claims");

        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("platform_user_tokens");
    }

    private static void ConfigurePlatformAuditLogs(ModelBuilder builder)
    {
        builder.Entity<PlatformAuditLog>(entity =>
        {
            entity.ToTable("platform_audit_logs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Entity)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityId)
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => x.CreatedAt);
        });
    }

    private static void ConfigureUserSessions(ModelBuilder builder)
    {
        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.StartedAt)
                .IsRequired();

            entity.Property(x => x.LastActivityAt)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(1000);

            entity.Property(x => x.EndReason)
                .HasMaxLength(200);

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => x.IsActive);

            entity.HasIndex(x => x.LastActivityAt);
        });
    }
}