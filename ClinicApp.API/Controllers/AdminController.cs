using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IClinicBookingsService _bookingsService;

    public AdminController(IClinicBookingsService bookingsService)
    {
        _bookingsService = bookingsService;
    }

    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetDashboardSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var summary = await _bookingsService.GetAdminDashboardSummaryAsync(from, to, cancellationToken);
        return Ok(summary);
    }
}
