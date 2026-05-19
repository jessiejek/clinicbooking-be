using ClinicApp.Application.Features.Auth.Dtos;
using System.Security.Claims;

namespace ClinicApp.Application.Common.Interfaces.Authentication;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientRequestDto request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponseDto> SocialLoginAsync(SocialLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task SetPasswordAsync(ClaimsPrincipal principal, SetPasswordRequestDto request, CancellationToken cancellationToken);
}
