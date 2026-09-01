using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Api.Controllers;

namespace SweetSecrets.UnitTests;

public sealed class EmailConfirmationEndpointTests
{
    [Theory]
    [InlineData(nameof(AuthController.ResendConfirmation), "resend-confirmation")]
    [InlineData(nameof(AuthController.ConfirmEmail), "confirm-email")]
    public void EmailConfirmationEndpoints_AreExplicitlyPublicPostActions(string methodName, string route)
    {
        var method = typeof(AuthController).GetMethod(methodName)!;

        Assert.Single(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        var httpPost = Assert.Single(method.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>());
        Assert.Equal(route, httpPost.Template);
    }
}
