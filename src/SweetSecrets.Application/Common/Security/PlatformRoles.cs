namespace SweetSecrets.Application.Common.Security;

public static class PlatformRoles
{
    public const string PlatformAdmin = "PLATFORM_ADMIN";
    public const string TenantOwner = "TENANT_OWNER";
    public const string TenantUser = "TENANT_USER";

    public static readonly string[] All =
    [
        PlatformAdmin,
        TenantOwner,
        TenantUser
    ];
}