using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Settings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly IClinicSettingsService _clinicSettingsService;

    public SettingsController(IClinicSettingsService clinicSettingsService)
    {
        _clinicSettingsService = clinicSettingsService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ClinicSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await _clinicSettingsService.GetAsync(cancellationToken);
        return Ok(settings);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<ClinicSettingsDto>> Update(
        [FromBody] UpdateClinicSettingsDto dto,
        CancellationToken cancellationToken)
    {
        var settings = await _clinicSettingsService.UpdateAsync(dto, cancellationToken);
        return Ok(settings);
    }
}
