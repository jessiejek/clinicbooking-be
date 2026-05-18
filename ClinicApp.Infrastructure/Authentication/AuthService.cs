using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces.Authentication;
using ClinicApp.Application.Common.Models.Authentication;
using ClinicApp.Application.Common.Options;
using ClinicApp.Application.Features.Auth.Dtos;
using ClinicApp.Domain.Entities.Authentication;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClinicApp.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Invalid email or password.");
        }

        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Email address is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Role = "Patient",
            AvatarUrl = request.AvatarUrl,
            AuthProvider = "Local",
            IsActive = true,
            IsFirstLogin = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(user, "Patient");

        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Refresh token is invalid or expired.");
        }

        var user = await _userManager.FindByIdAsync(token.UserId);
        if (user is null || !user.IsActive)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Refresh token is invalid or expired.");
        }

        var authResponse = await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = authResponse.RefreshToken;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return authResponse;
    }

    public async Task LogoutAsync(RefreshTokenRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        return MapUser(user);
    }

    public async Task SetPasswordAsync(ClaimsPrincipal principal, SetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(principal, cancellationToken);
        if (user is null)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        if (!user.IsFirstLogin || (user.Role is not "Staff" and not "Doctor"))
        {
            throw new ApiException(HttpStatusCode.Forbidden, "This action is only available for first-login staff or doctor accounts.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", removeResult.Errors.Select(x => x.Description)));
            }
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", addResult.Errors.Select(x => x.Description)));
        }

        user.IsFirstLogin = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(userId);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var authUser = MapUser(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(authUser, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken.AccessToken,
            RefreshToken: refreshToken,
            ExpiresAt: accessToken.ExpiresAt,
            User: authUser);
    }

    private static AuthUserDto MapUser(ApplicationUser user)
    {
        return new AuthUserDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email ?? string.Empty,
            Role: user.Role,
            AvatarUrl: user.AvatarUrl,
            IsFirstLogin: user.IsFirstLogin);
    }
}
