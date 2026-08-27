using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Settings;
using SweetSecrets.Contracts.Settings;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(
    Roles =
        PlatformRoles.TenantOwner + "," +
        PlatformRoles.TenantUser)]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingQueryService _settingQueryService;
    private readonly ISettingCommandService _settingCommandService;

    public SettingsController(ISettingQueryService settingQueryService, ISettingCommandService settingCommandService)
    {
        _settingQueryService = settingQueryService;
        _settingCommandService = settingCommandService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SettingListItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var settings =
            await _settingQueryService.GetAllAsync(
                cancellationToken);

        var response =
            settings
                .Select(x => new SettingListItemResponse
                {
                    Key = x.Key,
                    Value = x.Value,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();

        return Ok(response);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<SettingDetailResponse>> GetByKey(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(
                new
                {
                    message = "La clave de configuración es obligatoria."
                });
        }

        var setting =
            await _settingQueryService.GetByKeyAsync(
                key.Trim().ToUpperInvariant(),
                cancellationToken);

        if (setting is null)
        {
            return NotFound();
        }

        return Ok(
            new SettingDetailResponse
            {
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            });
    }

    [HttpPut("{key}")]
    [Authorize(Roles = PlatformRoles.TenantOwner)]
    public async Task<ActionResult<UpdateSettingResponse>> Update(string key, UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _settingCommandService.UpdateAsync(
                    new UpdateSettingCommand(
                        key,
                        request.Value),
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(
                new UpdateSettingResponse
                {
                    Key = result.Key,
                    Value = result.Value,
                    Description = result.Description,
                    CreatedAt = result.CreatedAt,
                    UpdatedAt = result.UpdatedAt
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }
}