using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Api.Controllers;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Domain.Enums;

namespace SweetSecrets.UnitTests;

public sealed class TenantLoginPolicyTests
{
    [Fact]
    public async Task ActiveTenantOwner_IsAllowed()
    {
        var reader = new MutableTenantStatusReader(TenantStatus.Active);
        var decision = await new TenantLoginPolicy(reader).EvaluateAsync(
            Guid.NewGuid(),
            [PlatformRoles.TenantOwner]);

        Assert.Equal(TenantLoginDecision.Allowed, decision);
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task SuspendedTenantOwner_IsRejectedFromMasterStatus()
    {
        var reader = new MutableTenantStatusReader(TenantStatus.Suspended);
        var decision = await new TenantLoginPolicy(reader).EvaluateAsync(
            Guid.NewGuid(),
            [PlatformRoles.TenantOwner]);

        Assert.Equal(TenantLoginDecision.Suspended, decision);
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task PlatformAdmin_IsUnaffectedAndDoesNotReadTenantStatus()
    {
        var reader = new MutableTenantStatusReader(TenantStatus.Suspended);
        var decision = await new TenantLoginPolicy(reader).EvaluateAsync(
            null,
            [PlatformRoles.PlatformAdmin]);

        Assert.Equal(TenantLoginDecision.Allowed, decision);
        Assert.Equal(0, reader.ReadCount);
    }

    [Fact]
    public async Task Reactivation_RestoresTenantOwnerAccess()
    {
        var reader = new MutableTenantStatusReader(TenantStatus.Suspended);
        var policy = new TenantLoginPolicy(reader);
        var tenantId = Guid.NewGuid();

        Assert.Equal(
            TenantLoginDecision.Suspended,
            await policy.EvaluateAsync(tenantId, [PlatformRoles.TenantOwner]));

        reader.Status = TenantStatus.Active;

        Assert.Equal(
            TenantLoginDecision.Allowed,
            await policy.EvaluateAsync(tenantId, [PlatformRoles.TenantOwner]));
    }

    [Fact]
    public async Task Me_EndsExistingSessionWhenTenantBecomesSuspended()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var authentication = new RecordingAuthenticationService();
        var controller = new AuthController(
            authentication,
            null!,
            null!,
            null!,
            null!,
            new TenantLoginPolicy(
                new MutableTenantStatusReader(TenantStatus.Suspended)));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, PlatformRoles.TenantOwner),
                    new Claim("tenant_id", tenantId.ToString()),
                    new Claim("session_id", sessionId.ToString())
                ], "test"))
            }
        };

        var result = await controller.Me(default);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(sessionId, authentication.EndedSessionId);
    }

    private sealed class MutableTenantStatusReader(TenantStatus? status)
        : ITenantStatusReader
    {
        public TenantStatus? Status { get; set; } = status;
        public int ReadCount { get; private set; }

        public Task<TenantStatus?> GetStatusAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(Status);
        }
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public Guid? EndedSessionId { get; private set; }

        public Task<AuthenticationResult> LoginAsync(
            string email,
            string password,
            bool rememberMe,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task LogoutAsync(
            Guid userId,
            Guid? sessionId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            EndedSessionId = sessionId;
            return Task.CompletedTask;
        }
    }
}
