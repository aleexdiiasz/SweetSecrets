using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Domain.Entities.Master;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Sessions;

public class UserSessionService : IUserSessionService
{
    private readonly MasterDbContext _dbContext;

    public UserSessionService(MasterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> StartSessionAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,

            StartedAt = now,
            LastActivityAt = now,

            IpAddress = ipAddress,
            UserAgent = userAgent,

            IsActive = true
        };

        await _dbContext.UserSessions.AddAsync(
            session,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return session.Id;
    }

    public async Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                x => x.Id == sessionId && x.IsActive,
                cancellationToken);

        if (session is null)
            return;

        session.LastActivityAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EndSessionAsync(
        Guid sessionId,
        string endReason,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                x => x.Id == sessionId && x.IsActive,
                cancellationToken);

        if (session is null)
            return;

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        session.EndReason = endReason;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EndAllUserSessionsAsync(
        Guid userId,
        string endReason,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _dbContext.UserSessions
            .Where(x =>
                x.UserId == userId &&
                x.IsActive)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = now;
            session.EndReason = endReason;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSessionInfo>> GetActiveSessionsAsync(
        TimeSpan activityWindow,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(activityWindow);

        return await _dbContext.UserSessions
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.LastActivityAt >= cutoff)
            .OrderByDescending(x => x.LastActivityAt)
            .Select(x => new UserSessionInfo
            {
                SessionId = x.Id,
                UserId = x.UserId,
                StartedAt = x.StartedAt,
                LastActivityAt = x.LastActivityAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToListAsync(cancellationToken);
    }
}