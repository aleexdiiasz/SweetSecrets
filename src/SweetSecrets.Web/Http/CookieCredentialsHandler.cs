using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SweetSecrets.Web.Http;

public sealed class CookieCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(        HttpRequestMessage request,        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(            BrowserRequestCredentials.Include);

        return base.SendAsync(
            request,
            cancellationToken);
    }
}