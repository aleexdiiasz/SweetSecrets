namespace SweetSecrets.Contracts.Auth;

public static class AuthenticatedAreaNavigation
{
    public const string PlatformAdminRole = "PLATFORM_ADMIN";
    public const string TenantOwnerRole = "TENANT_OWNER";
    public const string AdminHome = "/admin";
    public const string TenantHome = "/";
    public const string Login = "/login";

    public static string ResolveHome(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.Ordinal);

        if (roleSet.Contains(PlatformAdminRole))
        {
            return AdminHome;
        }

        return roleSet.Contains(TenantOwnerRole)
            ? TenantHome
            : Login;
    }
}
