using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Api.Controllers.Admin;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Contracts.Admin.Users;

namespace SweetSecrets.UnitTests;

public sealed class PlatformUserSessionManagementTests
{
    [Fact]
    public void AdministrativeUsersApi_RequiresPlatformAdmin() =>
        Assert.Equal(PlatformRoles.PlatformAdmin,
            Assert.Single(typeof(UsersController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);

    [Fact]
    public async Task UserSearch_ForwardsServerSideSearchAndFilters()
    {
        var query = new RecordingQueryService();
        var controller = CreateController(query, new StubAdminService());
        await controller.GetUsers("dulce", PlatformRoles.TenantOwner, true, false, 2, 25, default);
        Assert.Equal("dulce", query.UserSearch?.Search);
        Assert.Equal(PlatformRoles.TenantOwner, query.UserSearch?.Role);
        Assert.True(query.UserSearch?.IsBlocked);
        Assert.False(query.UserSearch?.IsOnline);
        Assert.Equal(2, query.UserSearch?.Page);
    }

    [Fact]
    public async Task MissingUser_DetailReturnsNotFound()
    {
        var result = await CreateController(new RecordingQueryService(), new StubAdminService()).GetById(Guid.NewGuid(), default);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void UserAndSessionContracts_DoNotExposeIdentitySecretsOrDatabaseInfrastructure()
    {
        var names = typeof(PlatformUserDetailResponse).GetProperties().Select(x => x.Name)
            .Concat(typeof(PlatformSessionItemResponse).GetProperties().Select(x => x.Name)).ToList();
        foreach (var forbidden in new[] { "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Token", "Cookie", "DatabaseName", "ConnectionString" })
            Assert.DoesNotContain(forbidden, names);
    }

    [Fact]
    public async Task SessionSearch_ForwardsServerSideFilters()
    {
        var query = new RecordingQueryService();
        await CreateController(query, new StubAdminService()).GetSessions("owner", true, 1, 30, default);
        Assert.Equal("owner", query.SessionSearch?.Search);
        Assert.True(query.SessionSearch?.IsActive);
        Assert.Equal(30, query.SessionSearch?.PageSize);
    }

    [Theory]
    [InlineData(PlatformUserChangeOutcome.Forbidden)]
    [InlineData(PlatformUserChangeOutcome.AlreadyInState)]
    public async Task SensitiveUserAction_IsRejectedWhenPolicyDoesNotAllowIt(PlatformUserChangeOutcome outcome)
    {
        var result = await CreateController(new RecordingQueryService(), new StubAdminService { UserOutcome = outcome })
            .BlockUser(Guid.NewGuid(), default);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CurrentAdministrativeSession_CannotBeRevoked()
    {
        var admin = new StubAdminService { SessionOutcome = PlatformSessionRevokeOutcome.Forbidden };
        var result = await CreateController(new RecordingQueryService(), admin).RevokeSession(Guid.NewGuid(), default);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task SuccessfulRevocation_ReturnsNoContent()
    {
        var admin = new StubAdminService { SessionOutcome = PlatformSessionRevokeOutcome.Success };
        var result = await CreateController(new RecordingQueryService(), admin).RevokeSession(Guid.NewGuid(), default);
        Assert.IsType<NoContentResult>(result);
    }

    private static UsersController CreateController(IPlatformUserQueryService query, IPlatformUserAdminService admin)
    {
        var controller = new UsersController(query, admin)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("session_id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, PlatformRoles.PlatformAdmin)], "tests"));
        return controller;
    }

    private sealed class RecordingQueryService : IPlatformUserQueryService
    {
        public PlatformUserSearch? UserSearch { get; private set; }
        public PlatformUserSessionSearch? SessionSearch { get; private set; }
        public Task<PlatformUserPage> SearchAsync(PlatformUserSearch search, TimeSpan onlineWindow, CancellationToken cancellationToken = default)
        { UserSearch = search; return Task.FromResult(new PlatformUserPage([], 0, search.Page, search.PageSize)); }
        public Task<PlatformUserDetail?> GetAsync(Guid userId, TimeSpan onlineWindow, CancellationToken cancellationToken = default) => Task.FromResult<PlatformUserDetail?>(null);
        public Task<PlatformUserSessionPage> SearchSessionsAsync(PlatformUserSessionSearch search, CancellationToken cancellationToken = default)
        { SessionSearch = search; return Task.FromResult(new PlatformUserSessionPage([], 0, search.Page, search.PageSize)); }
    }

    private sealed class StubAdminService : IPlatformUserAdminService
    {
        public PlatformUserChangeOutcome UserOutcome { get; set; } = PlatformUserChangeOutcome.Success;
        public PlatformSessionRevokeOutcome SessionOutcome { get; set; } = PlatformSessionRevokeOutcome.Success;
        public Task<PlatformUserChangeOutcome> BlockUserAsync(Guid userId, Guid performedByUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) => Task.FromResult(UserOutcome);
        public Task<PlatformUserChangeOutcome> UnblockUserAsync(Guid userId, Guid performedByUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) => Task.FromResult(UserOutcome);
        public Task<PlatformSessionRevokeOutcome> RevokeSessionAsync(Guid sessionId, Guid performedByUserId, Guid? performedFromSessionId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) => Task.FromResult(SessionOutcome);
    }
}
