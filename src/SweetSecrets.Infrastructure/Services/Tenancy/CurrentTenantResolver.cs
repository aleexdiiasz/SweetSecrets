using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public class CurrentTenantResolver : ICurrentTenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterDbContext _masterDbContext;

    public CurrentTenantResolver(IHttpContextAccessor httpContextAccessor, MasterDbContext masterDbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDbContext = masterDbContext;
    }

    public async Task<CurrentTenantInfo> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        var principal =
            _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException(
                "No existe un usuario autenticado.");

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "El usuario no está autenticado.");
        }

        var userIdValue =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "No fue posible identificar al usuario autenticado.");
        }

        var user = await _masterDbContext.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsBlocked)
            .Select(x => new
            {
                x.Id,
                x.TenantId
            })
            .FirstOrDefaultAsync(
                cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "El usuario no está disponible.");
        }

        if (!user.TenantId.HasValue)
        {
            throw new InvalidOperationException(
                "El usuario autenticado no pertenece a un tenant.");
        }

        var tenant = await _masterDbContext.Tenants
            .AsNoTracking()
            .Where(x =>
                x.Id == user.TenantId.Value &&
                x.Status == TenantStatus.Active)
            .Select(x => new CurrentTenantInfo(
                x.Id,
                x.Code,
                x.Name,
                x.DatabaseName))
            .FirstOrDefaultAsync(
                cancellationToken);

        if (tenant is null)
        {
            throw new InvalidOperationException(
                "El tenant del usuario no existe o no está activo.");
        }

        if (string.IsNullOrWhiteSpace(
                tenant.DatabaseName))
        {
            throw new InvalidOperationException(
                "El tenant no tiene una base de datos configurada.");
        }

        return tenant;
    }
}