using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Registration;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Registration;

public sealed class SelfRegistrationService
    : ISelfRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantUserProvisioningService _tenantUserProvisioningService;
    private readonly ITenantRegistryService _tenantRegistryService;
    private readonly IEmailConfirmationService _emailConfirmationService;

    public SelfRegistrationService(
        UserManager<ApplicationUser> userManager,
        ITenantProvisioningService tenantProvisioningService,
        ITenantUserProvisioningService tenantUserProvisioningService,
        ITenantRegistryService tenantRegistryService,
        IEmailConfirmationService emailConfirmationService)
    {
        _userManager = userManager;
        _tenantProvisioningService = tenantProvisioningService;
        _tenantUserProvisioningService = tenantUserProvisioningService;
        _tenantRegistryService = tenantRegistryService;
        _emailConfirmationService = emailConfirmationService;
    }

    public async Task<SelfRegistrationResult> RegisterAsync(SelfRegistrationCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var email =
            command.Email.Trim().ToLowerInvariant();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "Ya existe una cuenta registrada con ese correo.");
        }

        TenantProvisioningResult? tenant = null;

        try
        {
            tenant =
                await _tenantProvisioningService.ProvisionAsync(
                    command.BusinessName.Trim(),
                    cancellationToken);

            var userId =
                await _tenantUserProvisioningService.CreateOwnerAsync(
                    tenant.TenantId,
                    email,
                    command.FullName.Trim(),
                    command.Password,
                    cancellationToken);

            await _emailConfirmationService.SendForUserAsync(userId, cancellationToken);

            return new SelfRegistrationResult(
                userId,
                tenant.TenantId,
                tenant.Code,
                tenant.Name,
                email);
        }
        catch
        {
            if (tenant is not null)
            {
                await MarkTenantFailedSafelyAsync(
                    tenant.TenantId,
                    cancellationToken);
            }

            throw;
        }
    }

    private async Task MarkTenantFailedSafelyAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await _tenantRegistryService.MarkFailedAsync(
                tenantId,
                cancellationToken);
        }
        catch
        {
            // El error original del registro debe conservarse.
        }
    }

    private static void Validate(SelfRegistrationCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.BusinessName))
        {
            throw new ArgumentException(
                "El nombre del negocio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            throw new ArgumentException(
                "El nombre del propietario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException(
                "El correo electrónico es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ArgumentException(
                "La contraseña es obligatoria.");
        }

        if (command.BusinessName.Trim().Length > 200)
        {
            throw new ArgumentException(
                "El nombre del negocio no puede superar 200 caracteres.");
        }

        if (command.FullName.Trim().Length > 200)
        {
            throw new ArgumentException(
                "El nombre del propietario no puede superar 200 caracteres.");
        }
    }
}
