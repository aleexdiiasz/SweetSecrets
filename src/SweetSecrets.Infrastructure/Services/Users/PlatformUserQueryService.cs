using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Data.Master;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Users;

public class PlatformUserQueryService : IPlatformUserQueryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MasterDbContext _dbContext;

    public PlatformUserQueryService(UserManager<ApplicationUser> userManager, MasterDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlatformUserSummary>> GetUsersAsync(
        TimeSpan onlineWindow,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(onlineWindow);

        var activeUserIds = await _dbContext.UserSessions
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.LastActivityAt >= cutoff)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _userManager.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new PlatformUserSummary
            {
                Id = x.Id,
                TenantId = x.TenantId,

                Email = x.Email ?? string.Empty,
                FullName = x.FullName,

                IsActive = x.IsActive,
                IsBlocked = x.IsBlocked,

                IsOnline = activeUserIds.Contains(x.Id),

                LastLoginAt = x.LastLoginAt,
                LastActivityAt = x.LastActivityAt,

                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}