using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Units;
using SweetSecrets.Contracts.Units;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public sealed class UnitsController : ControllerBase
{
    private readonly IUnitQueryService _unitQueryService;

    public UnitsController(
        IUnitQueryService unitQueryService)
    {
        _unitQueryService = unitQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitListItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var units =
            await _unitQueryService.GetAllAsync(
                cancellationToken);

        var response =
            units
                .Select(x => new UnitListItemResponse
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Symbol = x.Symbol,
                    MeasurementType = (int)x.MeasurementType,
                    ConversionFactor = x.ConversionFactor,
                    IsActive = x.IsActive
                })
                .ToList();

        return Ok(response);
    }
}