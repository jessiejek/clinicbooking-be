using ClinicApp.Application.Common.Interfaces.Authentication;
using ClinicApp.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, GetClientIp(), cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterPatient(
        [FromBody] RegisterPatientRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterPatientAsync(request, GetClientIp(), cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, GetClientIp(), cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, GetClientIp(), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var response = await _authService.GetCurrentUserAsync(User, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword(
        [FromBody] SetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.SetPasswordAsync(User, request, cancellationToken);
        return NoContent();
    }

    private string? GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
