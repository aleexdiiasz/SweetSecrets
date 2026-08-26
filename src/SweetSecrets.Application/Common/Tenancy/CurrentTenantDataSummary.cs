namespace SweetSecrets.Application.Common.Tenancy;

public sealed record CurrentTenantDataSummary(
    int Units,
    int Products,
    int Recipes);