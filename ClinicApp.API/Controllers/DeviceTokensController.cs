using ClinicApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/device-tokens")]
[Authorize]
public sealed class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;

    public DeviceTokensController(IDeviceTokenService deviceTokenService)
    {
        _deviceTokenService = deviceTokenService;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        await _deviceTokenService.RegisterTokenAsync(userId, request.Token, request.Platform ?? "web", ct);
        return NoContent();
    }
}

public sealed class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string? Platform { get; set; }
}
