using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IClinicServicesService _servicesService;

    public ServicesController(IClinicServicesService servicesService)
    {
        _servicesService = servicesService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetActive(CancellationToken cancellationToken)
    {
        var services = await _servicesService.GetActiveServicesAsync(cancellationToken);
        return Ok(services);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await _servicesService.GetServiceAsync(id, includeInactive: false, cancellationToken);
        return Ok(service);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromBody] CreateServiceDto dto,
        CancellationToken cancellationToken)
    {
        var service = await _servicesService.CreateServiceAsync(dto, cancellationToken);
        return Ok(service);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> Update(
        Guid id,
        [FromBody] UpdateServiceDto dto,
        CancellationToken cancellationToken)
    {
        var service = await _servicesService.UpdateServiceAsync(id, dto, cancellationToken);
        return Ok(service);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _servicesService.DeleteServiceAsync(id, cancellationToken);
        return NoContent();
    }
}
