using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Api.Controllers.Admin;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Tenants;
using SweetSecrets.Contracts.Admin.Tenants;
using SweetSecrets.Domain.Enums;

namespace SweetSecrets.UnitTests;

public sealed class PlatformTenantManagementTests
{
    [Theory]
    [InlineData(TenantStatus.Active, TenantStatus.Suspended, true)]
    [InlineData(TenantStatus.Suspended, TenantStatus.Active, true)]
    [InlineData(TenantStatus.Failed, TenantStatus.Active, false)]
    [InlineData(TenantStatus.Provisioning, TenantStatus.Active, false)]
    [InlineData(TenantStatus.Disabled, TenantStatus.Active, false)]
    public void TransitionPolicy_AllowsOnlyFormalTransitions(TenantStatus current, TenantStatus target, bool expected) =>
        Assert.Equal(expected, PlatformTenantAdminService.IsAllowed(current, target));

    [Theory]
    [InlineData(TenantStatus.Active, TenantStatus.Suspended, "TENANT_SUSPENDED")]
    [InlineData(TenantStatus.Suspended, TenantStatus.Active, "TENANT_ACTIVATED")]
    public async Task ValidTransition_UpdatesMasterAndRegistersActorAudit(TenantStatus current, TenantStatus target, string action)
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var store = new FakeStore(CreateDetail(tenantId, current));
        var audit = new RecordingAudit();
        var service = new PlatformTenantAdminService(store, audit);

        var outcome = await service.ChangeStatusAsync(tenantId, target, actorId, "127.0.0.1", "tests");

        Assert.Equal(TenantStatusChangeOutcome.Success, outcome);
        Assert.Equal(target, store.ChangedTo);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(action, entry.Action);
        Assert.Equal(actorId, entry.UserId);
        Assert.Equal(tenantId, entry.TenantId);
    }

    [Fact]
    public async Task InvalidTransition_DoesNotUpdateOrAudit()
    {
        var store = new FakeStore(CreateDetail(Guid.NewGuid(), TenantStatus.Failed));
        var audit = new RecordingAudit();
        var outcome = await new PlatformTenantAdminService(store, audit)
            .ChangeStatusAsync(store.Detail!.Id, TenantStatus.Active, Guid.NewGuid(), null, null);

        Assert.Equal(TenantStatusChangeOutcome.InvalidTransition, outcome);
        Assert.Null(store.ChangedTo);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task MissingTenant_DetailReturnsNotFound()
    {
        var controller = CreateController(new StubAdminService());
        var result = await controller.GetById(Guid.NewGuid(), default);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task SearchAndStatus_AreForwardedServerSide()
    {
        var service = new StubAdminService
        {
            Page = new PlatformTenantPage([], 0, 1, 20)
        };
        var controller = CreateController(service);
        await controller.GetAll("dulce", "Suspended", 1, 20, default);

        Assert.Equal("dulce", service.Search?.Search);
        Assert.Equal(TenantStatus.Suspended, service.Search?.Status);
    }

    [Fact]
    public void PublicResponses_DoNotExposeDatabaseInfrastructure()
    {
        var propertyNames = typeof(TenantDetailResponse).GetProperties()
            .Select(property => property.Name).ToList();
        Assert.DoesNotContain("DatabaseName", propertyNames);
        Assert.DoesNotContain("ConnectionString", propertyNames);
    }

    private static TenantsController CreateController(IPlatformTenantAdminService service)
    {
        var controller = new TenantsController(null!, null!, service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "tests"));
        return controller;
    }

    private static PlatformTenantDetail CreateDetail(Guid id, TenantStatus status) =>
        new(id, "TEN-000001", "Tenant", status, DateTime.UtcNow, null, null, null, null, null, null, []);

    private sealed class FakeStore(PlatformTenantDetail? detail) : IPlatformTenantStore
    {
        public PlatformTenantDetail? Detail { get; } = detail;
        public TenantStatus? ChangedTo { get; private set; }
        public Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(Detail);
        public Task<bool> TryChangeStatusAsync(Guid tenantId, TenantStatus expectedStatus, TenantStatus newStatus, DateTime updatedAt, CancellationToken cancellationToken = default) { ChangedTo = newStatus; return Task.FromResult(true); }
    }

    private sealed class RecordingAudit : IPlatformAuditService
    {
        public List<PlatformAuditEntry> Entries { get; } = [];
        public Task RegisterAsync(PlatformAuditEntry entry, CancellationToken cancellationToken = default) { Entries.Add(entry); return Task.CompletedTask; }
    }

    private sealed class StubAdminService : IPlatformTenantAdminService
    {
        public PlatformTenantSearch? Search { get; private set; }
        public PlatformTenantPage Page { get; set; } = new([], 0, 1, 20);
        public Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default) { Search = search; return Task.FromResult(Page); }
        public Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<PlatformTenantDetail?>(null);
        public Task<TenantStatusChangeOutcome> ChangeStatusAsync(Guid tenantId, TenantStatus targetStatus, Guid actorUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
