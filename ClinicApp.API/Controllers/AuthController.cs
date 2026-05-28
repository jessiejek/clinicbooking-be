using ClinicApp.Application.Common.Interfaces.Authentication;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> Google(
        [FromBody] SocialLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureProvider(request.Provider, "Google");
        var response = await _authService.SocialLoginAsync(request, GetClientIp(), cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("facebook")]
    public async Task<ActionResult<AuthResponseDto>> Facebook(
        [FromBody] FacebookLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.FacebookLoginAsync(request, GetClientIp(), cancellationToken);
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
    [HttpPut("me")]
    public async Task<ActionResult<AuthUserDto>> UpdateProfile(
        [FromBody] UpdateAuthProfileDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.UpdateProfileAsync(User, request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(User, request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
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

    private static void EnsureProvider(string provider, string expectedProvider)
    {
        if (string.IsNullOrWhiteSpace(provider) || !provider.Equals(expectedProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(HttpStatusCode.BadRequest, $"Provider must be {expectedProvider}.");
        }
    }
}
