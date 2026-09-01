namespace SweetSecrets.Contracts.Units;

public sealed class UnitListItemResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int MeasurementType { get; set; }

    public decimal ConversionFactor { get; set; }

    public bool IsActive { get; set; }
}