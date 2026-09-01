using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SweetSecrets.Web;
using System.Globalization;
using SweetSecrets.Web.Auth;
using SweetSecrets.Web.Http;
using SweetSecrets.Web.Services.Auth;
using SweetSecrets.Web.Services.Units;
using SweetSecrets.Web.Services.Products;
using SweetSecrets.Web.Services.Recipes;
using SweetSecrets.Web.Services.Settings;
using SweetSecrets.Web.Services.Dashboard;
using SweetSecrets.Web.Services.Admin;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var culture = CultureInfo.GetCultureInfo("es-MX");

CultureInfo.DefaultThreadCurrentCulture = culture;

CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl =
    builder.Configuration["ApiBaseUrl"];

var apiBaseAddress =
    string.IsNullOrWhiteSpace(apiBaseUrl)
        ? builder.HostEnvironment.BaseAddress
        : apiBaseUrl;

builder.Services.AddTransient<CookieCredentialsHandler>();

builder.Services
    .AddHttpClient(
        "SweetSecretsApi",
        client =>
        {
            client.BaseAddress =
                new Uri(apiBaseAddress);
        })
    .AddHttpMessageHandler<CookieCredentialsHandler>();

builder.Services.AddScoped(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("SweetSecretsApi"));

builder.Services.AddAuthorizationCore();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<
    ApiAuthenticationStateProvider>();

builder.Services.AddScoped<
    AuthenticationStateProvider>(
        serviceProvider =>
            serviceProvider
                .GetRequiredService<
                    ApiAuthenticationStateProvider>());

builder.Services.AddScoped<AuthApiClient>();

builder.Services.AddScoped<UnitsApiClient>();

builder.Services.AddScoped<ProductsApiClient>();

builder.Services.AddScoped<RecipesApiClient>();

builder.Services.AddScoped<SettingsApiClient>();

builder.Services.AddScoped<DashboardApiClient>();

builder.Services.AddScoped<TenantsAdminApiClient>();
builder.Services.AddScoped<UsersAdminApiClient>();
builder.Services.AddScoped<PlatformDashboardApiClient>();

await builder.Build().RunAsync();
