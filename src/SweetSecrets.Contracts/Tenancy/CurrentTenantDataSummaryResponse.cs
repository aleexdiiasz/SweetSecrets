namespace SweetSecrets.Contracts.Tenancy;

public sealed class CurrentTenantDataSummaryResponse
{
    public int Units { get; set; }

    public int Products { get; set; }

    public int Recipes { get; set; }
}