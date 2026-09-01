using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Units;

public sealed record UnitListItem(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    MeasurementType MeasurementType,
    decimal ConversionFactor,
    bool IsActive);