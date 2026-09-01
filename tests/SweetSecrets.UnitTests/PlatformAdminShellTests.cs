using Microsoft.AspNetCore.Authorization;
using SweetSecrets.Api.Controllers;
using SweetSecrets.Api.Controllers.Admin;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Auth;

namespace SweetSecrets.UnitTests;

public sealed class PlatformAdminShellTests
{
    [Fact]
    public void PlatformAdmin_IsDirectedToAdministrativeArea()
    {
        var destination = AuthenticatedAreaNavigation.ResolveHome(
            [PlatformRoles.PlatformAdmin]);

        Assert.Equal("/admin", destination);
    }

    [Fact]
    public void TenantOwner_IsDirectedToTenantApplication()
    {
        var destination = AuthenticatedAreaNavigation.ResolveHome(
            [PlatformRoles.TenantOwner]);

        Assert.Equal("/", destination);
    }

    [Fact]
    public void MissingAuthenticatedRole_IsDirectedToLogin()
    {
        Assert.Equal(
            "/login",
            AuthenticatedAreaNavigation.ResolveHome([]));
    }

    [Theory]
    [InlineData(typeof(UsersController))]
    [InlineData(typeof(TenantsController))]
    public void ExistingAdministrativeApi_RequiresPlatformAdmin(Type controllerType)
    {
        var authorize = Assert.Single(
            controllerType
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(PlatformRoles.PlatformAdmin, authorize.Roles);
    }

    [Theory]
    [InlineData(typeof(DashboardController))]
    [InlineData(typeof(ProductsController))]
    [InlineData(typeof(RecipesController))]
    [InlineData(typeof(SettingsController))]
    public void TenantOperationalApi_RemainsRestrictedToTenantOwner(
        Type controllerType)
    {
        var authorize = Assert.Single(
            controllerType
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(PlatformRoles.TenantOwner, authorize.Roles);
    }
}
