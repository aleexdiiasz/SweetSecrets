using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Api.Controllers.Admin;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Admin.Audit;
using SweetSecrets.Infrastructure.Data.Master;
using SweetSecrets.Infrastructure.Services.Auditing;

namespace SweetSecrets.UnitTests;

public sealed class PlatformAuditExplorerTests
{
    [Fact]
    public void Endpoints_RequirePlatformAdmin_SoTenantOwnerIsForbidden()
    {
        var attribute = Assert.Single(typeof(AuditController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PlatformRoles.PlatformAdmin, attribute.Roles);
        Assert.NotEqual(PlatformRoles.TenantOwner, attribute.Roles);
    }

    [Fact]
    public async Task Listing_ForwardsSearchFiltersDatesAndPagination_WithSafeMaximum()
    {
        var service = new RecordingService();
        var controller = new AuditController(service);
        var from = new DateTime(2026, 8, 1); var to = new DateTime(2026, 8, 31);
        await controller.GetAll("bloqueo", "USER_BLOCKED", from, to, "Dulce", "admin", "owner", 2, 500, default);
        var search = Assert.IsType<PlatformAuditSearch>(service.Search);
        Assert.Equal("bloqueo", search.Search); Assert.Equal("USER_BLOCKED", search.Action);
        Assert.Equal(from, search.From); Assert.Equal(to.AddDays(1), search.ToExclusive);
        Assert.Equal("Dulce", search.Tenant); Assert.Equal("admin", search.Actor); Assert.Equal("owner", search.TargetUser);
        Assert.Equal(2, search.Page); Assert.Equal(50, search.PageSize);
    }

    [Fact]
    public async Task InvalidDateRange_ReturnsBadRequestWithoutQuerying()
    {
        var service = new RecordingService();
        var result = await new AuditController(service).GetAll(null,null,new DateTime(2026,9,2),new DateTime(2026,9,1),null,null,null,1,20,default);
        Assert.IsType<BadRequestObjectResult>(result.Result); Assert.Null(service.Search);
    }

    [Fact]
    public async Task EmptyState_MapsZeroAndNoItems()
    {
        var response = Assert.IsType<PlatformAuditPageResponse>(Assert.IsType<OkObjectResult>((await new AuditController(new RecordingService())
            .GetAll(null,null,null,null,null,null,null,1,20,default)).Result).Value);
        Assert.Equal(0,response.TotalCount); Assert.Empty(response.Items);
    }

    [Fact]
    public async Task Detail_MapsAvailableData_AndMissingRelationsRemainVisible()
    {
        var id = Guid.NewGuid(); var service = new RecordingService
        {
            Detail = new PlatformAuditDetail(id,"LOGIN_FAILED","USER","Credenciales inválidas",DateTime.UtcNow,
                null,null,null,null,null,null,"127.0.0.1","Browser")
        };
        var response = Assert.IsType<PlatformAuditDetailResponse>(Assert.IsType<OkObjectResult>(
            (await new AuditController(service).GetById(id,default)).Result).Value);
        Assert.Equal(id,response.Id); Assert.Null(response.ActorName); Assert.Null(response.TenantName);
        Assert.Equal("Browser",response.UserAgent);
    }

    [Fact]
    public async Task MissingDetail_ReturnsNotFound() => Assert.IsType<NotFoundResult>(
        (await new AuditController(new RecordingService()).GetById(Guid.NewGuid(),default)).Result);

    [Fact]
    public void Contracts_ExcludeSecretsAndArbitraryMetadata()
    {
        var names = typeof(PlatformAuditDetailResponse).GetProperties().Select(x=>x.Name).ToList();
        foreach(var forbidden in new[]{"PasswordHash","Password","SecurityStamp","Cookie","Token","ConnectionString","DatabaseName","OldValues","NewValues","Headers"})
            Assert.DoesNotContain(forbidden,names);
    }

    [Fact]
    public void Service_DependsOnlyOnMasterContext_NotTenantContext()
    {
        var constructor = Assert.Single(typeof(PlatformAuditQueryService).GetConstructors());
        Assert.Equal([typeof(MasterDbContext)],constructor.GetParameters().Select(x=>x.ParameterType));
    }

    [Fact]
    public void SearchFiltersOrderingAndPagination_TranslateToPostgreSql()
    {
        using var context = new MasterDbContext(new DbContextOptionsBuilder<MasterDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=translation_only").Options);
        var service = new PlatformAuditQueryService(context);
        var sql = service.BuildPageQuery(new PlatformAuditSearch("owner","USER_BLOCKED",
            DateTime.UtcNow.AddDays(-7),DateTime.UtcNow.AddDays(1),"dulce","admin","owner",2,50)).ToQueryString();
        Assert.Contains("platform_audit_logs",sql); Assert.Contains("ILIKE",sql);
        Assert.Contains("ORDER BY",sql); Assert.Contains("DESC",sql);
        Assert.Contains("LIMIT",sql); Assert.Contains("OFFSET",sql);
        Assert.Contains("CreatedAt",sql); Assert.Contains("Action",sql);
    }

    private sealed class RecordingService : IPlatformAuditQueryService
    {
        public PlatformAuditSearch? Search { get; private set; }
        public PlatformAuditDetail? Detail { get; set; }
        public Task<PlatformAuditPage> SearchAsync(PlatformAuditSearch search,CancellationToken cancellationToken=default)
        { Search=search; return Task.FromResult(new PlatformAuditPage([],0,search.Page,search.PageSize)); }
        public Task<PlatformAuditDetail?> GetAsync(Guid id,CancellationToken cancellationToken=default)=>Task.FromResult(Detail);
    }
}
