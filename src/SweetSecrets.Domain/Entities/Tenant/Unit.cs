using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Domain.Entities.Tenant;

public class Unit
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public MeasurementType MeasurementType { get; set; }

    public decimal ConversionFactor { get; set; }

    public bool IsActive { get; set; } = true;
}