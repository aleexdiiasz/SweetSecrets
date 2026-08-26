using Microsoft.EntityFrameworkCore;
using SweetSecrets.Infrastructure.Data.Master;
using Microsoft.AspNetCore.Identity;
using SweetSecrets.Infrastructure.Identity;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Infrastructure.Services.Auditing;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Infrastructure.Services.Sessions;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Services.Users;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Infrastructure.Services.Authentication;
using SweetSecrets.Api.Middleware;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Tenant;
using SweetSecrets.Infrastructure.Services.Tenancy;

var builder = WebApplication.CreateBuilder(args);

var masterConnectionString =
    builder.Configuration.GetConnectionString("MasterDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MasterDatabase' was not configured.");

builder.Services.Configure<TenantDatabaseOptions>(
    options =>
    {
        options.AdminConnectionString =
            masterConnectionString;

        options.DatabasePrefix =
            "sweetsecrets_tenant_";
    });

builder.Services.AddDbContext<MasterDbContext>(options =>
{
    options.UseNpgsql(masterConnectionString);
});

builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(
        BootstrapAdminOptions.SectionName));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MasterDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SweetSecrets.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Lax;

    options.SlidingExpiration = true;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddScoped<MasterDataInitializer>();

builder.Services.AddScoped<IPlatformAuditService, PlatformAuditService>();

builder.Services.AddScoped<IUserSessionService, UserSessionService>();

builder.Services.AddScoped<IPlatformUserAdminService, PlatformUserAdminService>();

builder.Services.AddScoped<IPlatformUserQueryService, PlatformUserQueryService>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddScoped<ITenantDatabaseManager, PostgresTenantDatabaseManager>();

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "SweetSecrets API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<UserActivityMiddleware>();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var initializer =
        scope.ServiceProvider
            .GetRequiredService<MasterDataInitializer>();

    await initializer.InitializeAsync();
}

app.Run();