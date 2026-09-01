using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Data.Master;
using SweetSecrets.Infrastructure.Services.Users;

namespace SweetSecrets.UnitTests;

public sealed class PlatformUserQueryTranslationTests
{
    [Fact]
    public void BaseListing_OrderingAndPagination_TranslateToPostgreSql()
    {
        using var context = CreateContext();
        var sql = new PlatformUserQueryService(context)
            .BuildSearchQuery(new PlatformUserSearch(null, null, null, null, 2, 20),
                DateTime.UtcNow.AddMinutes(-5), applyPagination: true)
            .ToQueryString();

        Assert.Contains("ORDER BY", sql);
        Assert.Contains("\"FullName\"", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
        Assert.DoesNotContain("UserRow", sql);
    }

    [Fact]
    public void SearchByNameEmailTenantAndCode_TranslatesToServerSideIlike()
    {
        using var context = CreateContext();
        var sql = Sql(context, new PlatformUserSearch("dulce", null, null, null, 1, 20));

        Assert.Contains("ILIKE", sql);
        Assert.Contains("\"FullName\"", sql);
        Assert.Contains("\"Email\"", sql);
        Assert.Contains("\"Code\"", sql);
        Assert.Contains("\"Name\"", sql);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RoleAndBlockedFilters_TranslateToPostgreSql(bool blocked)
    {
        using var context = CreateContext();
        var sql = Sql(context, new PlatformUserSearch(null, PlatformRoles.TenantOwner, blocked, null, 1, 20));

        Assert.Contains("\"IsBlocked\"", sql);
        Assert.Contains(PlatformRoles.TenantOwner, sql);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnlineFilter_UsesActiveSessionAndFiveMinuteCutoff(bool online)
    {
        using var context = CreateContext();
        var sql = Sql(context, new PlatformUserSearch(null, null, null, online, 1, 20));

        Assert.Contains("EXISTS", sql);
        Assert.Contains("user_sessions", sql);
        Assert.Contains("\"IsActive\"", sql);
        Assert.Contains("\"LastActivityAt\"", sql);
    }

    private static string Sql(MasterDbContext context, PlatformUserSearch search) =>
        new PlatformUserQueryService(context)
            .BuildSearchQuery(search, DateTime.UtcNow.AddMinutes(-5), applyPagination: true)
            .ToQueryString();

    private static MasterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=translation_only")
            .Options;
        return new MasterDbContext(options);
    }
}
