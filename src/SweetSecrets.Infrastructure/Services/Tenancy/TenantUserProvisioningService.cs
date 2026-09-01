using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class TenantUserProvisioningService : ITenantUserProvisioningService
{
    private readonly MasterDbContext _masterDbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityErrorLocalizer _errorLocalizer;

    public TenantUserProvisioningService(
        MasterDbContext masterDbContext,
        UserManager<ApplicationUser> userManager,
        IdentityErrorLocalizer errorLocalizer)
    {
        _masterDbContext = masterDbContext;
        _userManager = userManager;
        _errorLocalizer = errorLocalizer;
    }

    public async Task<Guid> CreateOwnerAsync(
        Guid tenantId,
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _masterDbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == tenantId &&
                    x.Status == TenantStatus.Active,
                cancellationToken);

        if (tenant is null)
        {
            throw new InvalidOperationException(
                "El tenant no existe o no está activo.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "Ya existe un usuario con ese correo.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim(),
            UserName = email.Trim(),
            FullName = fullName.Trim(),
            IsActive = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                password);

        if (!createResult.Succeeded)
        {
            var errors = _errorLocalizer.Localize(createResult.Errors);

            throw new InvalidOperationException(
                $"No fue posible crear el usuario: {errors}");
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                PlatformRoles.TenantOwner);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(
                    x => x.Description));

            throw new InvalidOperationException(
                $"No fue posible asignar el rol TENANT_OWNER: {errors}");
        }

        return user.Id;
    }
}
