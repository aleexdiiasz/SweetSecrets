using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Data.Master;

public class MasterDataInitializer
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BootstrapAdminOptions _adminOptions;

    public MasterDataInitializer(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<BootstrapAdminOptions> adminOptions)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminOptions = adminOptions.Value;
    }

    public async Task InitializeAsync()
    {
        await CreateRolesAsync();
        await CreatePlatformAdminAsync();
    }

    private async Task CreateRolesAsync()
    {
        foreach (var roleName in PlatformRoles.All)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await _roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No fue posible crear el rol '{roleName}': " +
                    string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }
    }

    private async Task CreatePlatformAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.Email) ||
            string.IsNullOrWhiteSpace(_adminOptions.Password) ||
            string.IsNullOrWhiteSpace(_adminOptions.FullName))
        {
            throw new InvalidOperationException(
                "La configuración BootstrapAdmin está incompleta.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(_adminOptions.Email);

        if (existingUser is not null)
        {
            if (!await _userManager.IsInRoleAsync(
                    existingUser,
                    PlatformRoles.PlatformAdmin))
            {
                await _userManager.AddToRoleAsync(
                    existingUser,
                    PlatformRoles.PlatformAdmin);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _adminOptions.Email,
            Email = _adminOptions.Email,
            EmailConfirmed = true,

            FullName = _adminOptions.FullName,

            TenantId = null,

            IsActive = true,
            IsBlocked = false,

            CreatedAt = DateTime.UtcNow
        };

        var createResult =
            await _userManager.CreateAsync(admin, _adminOptions.Password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "No fue posible crear PLATFORM_ADMIN: " +
                string.Join(", ",
                    createResult.Errors.Select(x => x.Description)));
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                admin,
                PlatformRoles.PlatformAdmin);

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "No fue posible asignar PLATFORM_ADMIN: " +
                string.Join(", ",
                    roleResult.Errors.Select(x => x.Description)));
        }
    }
}