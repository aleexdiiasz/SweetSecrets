using Microsoft.AspNetCore.Authorization;
using SweetSecrets.Api.Controllers;
using SweetSecrets.Application.Common.Security;

namespace SweetSecrets.UnitTests;

public sealed class AccountEndpointAuthorizationTests
{
    [Theory]
    [InlineData(nameof(AuthController.Account))]
    [InlineData(nameof(AuthController.ChangePassword))]
    public void AccountEndpoints_RequireTenantOwner(string methodName)
    {
        var method = typeof(AuthController).GetMethod(methodName);
        var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(PlatformRoles.TenantOwner, authorize.Roles);
    }
}
