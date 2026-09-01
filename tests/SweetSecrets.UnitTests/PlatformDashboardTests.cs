using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Admin.Dashboard;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;
using SweetSecrets.Infrastructure.Services.Dashboard;
using AdminPlatformDashboardController = SweetSecrets.Api.Controllers.Admin.PlatformDashboardController;

namespace SweetSecrets.UnitTests;

public sealed class PlatformDashboardTests
{
    [Fact]
    public void Endpoint_RequiresPlatformAdmin()
    {
        var authorize = Assert.Single(typeof(AdminPlatformDashboardController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PlatformRoles.PlatformAdmin, authorize.Roles);
    }

    [Fact]
    public async Task Endpoint_MapsTenantUserSessionAndRecentMetrics()
    {
        var summary = new PlatformDashboardSummary(8,
            new Dictionary<TenantStatus,int>{{TenantStatus.Active,4},{TenantStatus.Suspended,1},{TenantStatus.Provisioning,1},{TenantStatus.Failed,1},{TenantStatus.Disabled,1}},
            12, 10, 2, 3, 4, 6, 5,
            [new(Guid.NewGuid(),"000008","Dulce",TenantStatus.Active,DateTime.UtcNow)],
            [new(Guid.NewGuid(),"Owner","owner@test.local",PlatformRoles.TenantOwner,DateTime.UtcNow)],
            [new("USER_BLOCKED","Usuario bloqueado",DateTime.UtcNow)]);
        var response = Assert.IsType<PlatformDashboardResponse>(Assert.IsType<OkObjectResult>(
            (await new AdminPlatformDashboardController(new StubService(summary)).Get(default)).Result).Value);

        Assert.Equal(8, response.TotalTenants);
        Assert.Equal(4, response.TenantsByStatus["Active"]);
        Assert.Equal(12, response.TotalUsers);
        Assert.Equal(10, response.TenantOwners);
        Assert.Equal(2, response.PlatformAdmins);
        Assert.Equal(3, response.BlockedUsers);
        Assert.Equal(6, response.ActiveSessions);
        Assert.Equal(5, response.OnlineUsers);
        Assert.Single(response.RecentEvents);
    }

    [Fact]
    public async Task EmptyMaster_ReturnsZerosAndEmptyLists()
    {
        var statuses = Enum.GetValues<TenantStatus>().ToDictionary(x => x, _ => 0);
        var summary = new PlatformDashboardSummary(0,statuses,0,0,0,0,0,0,0,[],[],[]);
        var response = Assert.IsType<PlatformDashboardResponse>(Assert.IsType<OkObjectResult>(
            (await new AdminPlatformDashboardController(new StubService(summary)).Get(default)).Result).Value);
        Assert.Equal(0, response.TotalTenants);
        Assert.Equal(0, response.TotalUsers);
        Assert.Empty(response.RecentTenants);
    }

    [Fact]
    public void PublicContract_DoesNotExposeSensitiveOrTenantDatabaseFields()
    {
        var names = typeof(PlatformDashboardResponse).GetProperties().Select(x => x.Name)
            .Concat(typeof(PlatformRecentTenantResponse).GetProperties().Select(x => x.Name)).ToList();
        foreach (var name in new[]{"DatabaseName","ConnectionString","PasswordHash","SecurityStamp","Token","Cookie","IpAddress","UserAgent"})
            Assert.DoesNotContain(name, names);
    }

    [Fact]
    public void OnlineAndRecentQueries_TranslateWithExpectedWindowOrderAndLimit()
    {
        using var context = CreateContext();
        var service = new PlatformDashboardQueryService(context);
        var onlineSql = service.BuildOnlineUsersQuery(DateTime.UtcNow.AddMinutes(-5)).ToQueryString();
        Assert.Contains("user_sessions", onlineSql);
        Assert.Contains("\"IsActive\"", onlineSql);
        Assert.Contains("\"LastActivityAt\"", onlineSql);
        Assert.Contains("DISTINCT", onlineSql);

        foreach (var sql in new[]{service.BuildRecentTenantsQuery(5).ToQueryString(),
            service.BuildRecentUsersQuery(5).ToQueryString(), service.BuildRecentEventsQuery(5).ToQueryString()})
        {
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("LIMIT", sql);
        }
    }

    private static MasterDbContext CreateContext() => new(new DbContextOptionsBuilder<MasterDbContext>()
        .UseNpgsql("Host=localhost;Database=translation_only;Username=translation_only").Options);

    private sealed class StubService(PlatformDashboardSummary summary) : IPlatformDashboardQueryService
    {
        public Task<PlatformDashboardSummary> GetAsync(TimeSpan onlineWindow, int recentLimit, CancellationToken cancellationToken = default)
        {
            Assert.Equal(TimeSpan.FromMinutes(5), onlineWindow);
            Assert.Equal(5, recentLimit);
            return Task.FromResult(summary);
        }
    }
}
