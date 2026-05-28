using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
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
using Microsoft.IdentityModel.Tokens;

namespace ClinicApp.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly GoogleAuthOptions _googleAuthOptions;
    private readonly FacebookAuthOptions _facebookAuthOptions;
    private static readonly string[] GoogleTokenIssuers =
    [
        "accounts.google.com",
        "https://accounts.google.com"
    ];

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IOptions<GoogleAuthOptions> googleAuthOptions,
        IOptions<FacebookAuthOptions> facebookAuthOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _googleAuthOptions = googleAuthOptions.Value;
        _facebookAuthOptions = facebookAuthOptions.Value;
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
            FullName = string.Join(" ", new[] { request.FirstName.Trim(), request.MiddleName?.Trim(), request.LastName.Trim() }
                .Where(p => !string.IsNullOrWhiteSpace(p))),
            Role = "Patient",
            AvatarUrl = request.AvatarUrl,
            AuthProvider = "Local",
            IsActive = true,
            IsFirstLogin = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(user, "Patient");

        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> SocialLoginAsync(SocialLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        var provider = NormalizeProvider(request.Provider);
        var socialProfile = provider switch
        {
            "Google" => await ValidateGoogleLoginAsync(request.IdToken, request.AccessToken, cancellationToken),
            "Facebook" => await ValidateFacebookLoginAsync(request.AccessToken, null, cancellationToken),
            _ => throw new ApiException(HttpStatusCode.BadRequest, "Provider must be Google or Facebook.")
        };

        return await CompleteSocialLoginAsync(provider, socialProfile, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> FacebookLoginAsync(FacebookLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Provider) && !request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Provider must be Facebook.");
        }

        var socialProfile = await ValidateFacebookLoginAsync(request.AccessToken, request.UserId, cancellationToken);
        return await CompleteSocialLoginAsync("Facebook", socialProfile, ipAddress, cancellationToken);
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

        if (!user.IsFirstLogin || (user.Role is not "Staff" and not "Doctor" and not "Patient"))
        {
            throw new ApiException(HttpStatusCode.Forbidden, "This action is only available for first-login staff, doctor, or patient accounts.");
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

    public async Task ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Passwords do not match.");
        }

        var user = await GetUserAsync(principal, cancellationToken);
        if (user is null)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ApiException(HttpStatusCode.BadRequest, $"Password change failed: {errors}");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Don't reveal whether the email exists
            return;
        }

        // Generate a secure random reset token
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        // Store in RefreshTokens table with a flag: no UserId constraint check since this is a reset
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = resetToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedByIp = null
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        // In production, email the token to the user.
        // The front-end reset-password page reads it from the query string (?token=...).
        _ = resetToken;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.Token && x.ExpiresAt > DateTime.UtcNow && x.RevokedAt == null, cancellationToken);

        if (storedToken is null)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid or expired password reset token.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid password reset request.");
        }

        // Verify the token belongs to the requested email
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Invalid password reset request.");
        }

        // Remove existing password and set new one
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                $"Password reset failed: {string.Join("; ", removeResult.Errors.Select(e => e.Description))}");
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                $"Password reset failed: {string.Join("; ", addResult.Errors.Select(e => e.Description))}");
        }

        // Revoke the reset token so it cannot be reused
        storedToken.RevokedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserDto> UpdateProfileAsync(ClaimsPrincipal principal, UpdateAuthProfileDto request, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(principal, cancellationToken);
        if (user is null)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        if (request.FullName is not null)
        {
            user.FullName = request.FullName;
        }

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        if (request.PhoneNumber is not null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return MapUser(user);
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
            IsFirstLogin: user.IsFirstLogin,
            PhoneNumber: user.PhoneNumber);
    }

    private async Task<ApplicationUser> LinkOrCreateSocialUserAsync(
        string provider,
        SocialProfile socialProfile,
        CancellationToken cancellationToken)
    {
        var email = socialProfile.Email.Trim();
        var fullName = TruncateString(socialProfile.DisplayName, 150);
        var avatarUrl = TruncateString(socialProfile.PhotoUrl, 500);

        var externalAccount = await _dbContext.ExternalLoginAccounts
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == socialProfile.ProviderUserId, cancellationToken);

        if (externalAccount is not null)
        {
            var linkedUser = await _userManager.FindByIdAsync(externalAccount.UserId);
            if (linkedUser is null || !linkedUser.IsActive)
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Social login token is invalid.");
            }

            ApplySocialProfile(linkedUser, provider, socialProfile);
            var updateResult = await _userManager.UpdateAsync(linkedUser);
            if (!updateResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", updateResult.Errors.Select(x => x.Description)));
            }

            return linkedUser;
        }

        var user = await _userManager.FindByEmailAsync(email);
        user ??= await _userManager.FindByNameAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName ?? email,
                Role = "Patient",
                AvatarUrl = avatarUrl,
                AuthProvider = provider,
                IsActive = true,
                IsFirstLogin = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", createResult.Errors.Select(x => x.Description)));
                }
            }
            catch (DbUpdateException)
            {
                var existingUser = await _userManager.FindByEmailAsync(email)
                    ?? await _userManager.FindByNameAsync(email);

                if (existingUser is not null)
                {
                    ApplySocialProfile(existingUser, provider, socialProfile);
                    var updateResult = await _userManager.UpdateAsync(existingUser);
                    if (!updateResult.Succeeded)
                    {
                        throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", updateResult.Errors.Select(x => x.Description)));
                    }

                    await EnsurePatientRoleAsync(existingUser);
                    return existingUser;
                }

                throw new ApiException(HttpStatusCode.InternalServerError, "Social login could not create a new user record. Please check the database schema for the AspNetUsers table.");
            }

            await EnsurePatientRoleAsync(user);
            return user;
        }

        if (!user.IsActive)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "This account is inactive.");
        }

        ApplySocialProfile(user, provider, socialProfile);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        return user;
    }

    private async Task<AuthResponseDto> CompleteSocialLoginAsync(
        string provider,
        SocialProfile socialProfile,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await LinkOrCreateSocialUserAsync(provider, socialProfile, cancellationToken);
        if (!user.IsActive)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "This account is inactive.");
        }

        await UpsertExternalLoginAccountAsync(user, provider, socialProfile, cancellationToken);
        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    private async Task UpsertExternalLoginAccountAsync(
        ApplicationUser user,
        string provider,
        SocialProfile socialProfile,
        CancellationToken cancellationToken)
    {
        var providerEmail = socialProfile.Email.Trim();
        var providerDisplayName = TruncateString(socialProfile.DisplayName, 200) ?? socialProfile.DisplayName.Trim();
        var providerPhotoUrl = TruncateString(socialProfile.PhotoUrl, 500);

        var existing = await _dbContext.ExternalLoginAccounts
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Provider == provider, cancellationToken);

        if (existing is null)
        {
            _dbContext.ExternalLoginAccounts.Add(new ExternalLoginAccount
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = socialProfile.ProviderUserId,
                ProviderEmail = providerEmail,
                ProviderDisplayName = providerDisplayName,
                ProviderPhotoUrl = providerPhotoUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ProviderUserId = socialProfile.ProviderUserId;
            existing.ProviderEmail = providerEmail;
            existing.ProviderDisplayName = providerDisplayName;
            existing.ProviderPhotoUrl = providerPhotoUrl;
            existing.LastLoginAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplySocialProfile(ApplicationUser user, string provider, SocialProfile socialProfile)
    {
        var email = socialProfile.Email.Trim();
        var fullName = TruncateString(socialProfile.DisplayName, 150);
        var avatarUrl = TruncateString(socialProfile.PhotoUrl, 500);

        user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName;
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.AvatarUrl = avatarUrl ?? user.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(user.AuthProvider))
        {
            user.AuthProvider = provider;
            return;
        }

        if (user.AuthProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            user.AuthProvider = "Mixed";
            return;
        }

        if (!user.AuthProvider.Equals(provider, StringComparison.OrdinalIgnoreCase))
        {
            user.AuthProvider = "Mixed";
        }
    }

    private static string? GetClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsClaimTrue(string? value)
    {
        return bool.TryParse(value, out var parsed) && parsed;
    }

    private static string ResolveGoogleDisplayName(string? name, string? givenName, string? familyName, string fallbackEmail)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var nameParts = new[] { givenName, familyName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        return nameParts.Length > 0
            ? string.Join(" ", nameParts)
            : fallbackEmail;
    }

    private static string ResolveFacebookDisplayName(string? name, string fallbackEmail)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        return fallbackEmail;
    }

    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private async Task EnsurePatientRoleAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Patient"))
        {
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
        if (!roleResult.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        }
    }

    private async Task<IReadOnlyCollection<SecurityKey>> GetGoogleSigningKeysAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var jwksJson = await client.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs", cancellationToken);
        try
        {
            var jwkSet = new JsonWebKeySet(jwksJson);
            return jwkSet.Keys.Select(key => (SecurityKey)key).ToArray();
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Google signing keys could not be parsed.", ex);
        }
    }

    private async Task<SocialProfile> ValidateGoogleLoginAsync(string? idToken, string? accessToken, CancellationToken cancellationToken)
    {
        // If we have an idToken (JWT), validate it directly
        if (!string.IsNullOrWhiteSpace(idToken))
        {
            return await ValidateGoogleIdTokenAsync(idToken, cancellationToken);
        }

        // Fallback: use access_token to call Google UserInfo API (like Facebook's approach)
        return await ValidateGoogleAccessTokenAsync(accessToken, cancellationToken);
    }

    private async Task<SocialProfile> ValidateGoogleIdTokenAsync(string idToken, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
        {
            throw new ApiException(HttpStatusCode.InternalServerError, "Google login configuration is missing. Set Google:ClientId before starting the app.");
        }

        try
        {
            var signingKeys = await GetGoogleSigningKeysAsync(cancellationToken);
            if (signingKeys.Count == 0)
            {
                throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = GoogleTokenIssuers,
                ValidateAudience = true,
                ValidAudience = _googleAuthOptions.ClientId,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken
                || !string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.RsaSha256, StringComparison.Ordinal))
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
            }

            var providerUserId = GetClaimValue(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
            var email = GetClaimValue(principal, JwtRegisteredClaimNames.Email, ClaimTypes.Email);
            var emailVerified = GetClaimValue(principal, "email_verified");
            var name = GetClaimValue(principal, "name", ClaimTypes.Name);
            var givenName = GetClaimValue(principal, "given_name", ClaimTypes.GivenName);
            var familyName = GetClaimValue(principal, "family_name", ClaimTypes.Surname);
            var picture = GetClaimValue(principal, "picture");

            if (string.IsNullOrWhiteSpace(providerUserId)
                || string.IsNullOrWhiteSpace(email)
                || !IsClaimTrue(emailVerified))
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
            }

            return new SocialProfile(
                ProviderUserId: providerUserId,
                Email: email,
                DisplayName: ResolveGoogleDisplayName(name, givenName, familyName, email),
                PhotoUrl: picture,
                EmailVerified: true,
                GivenName: givenName,
                FamilyName: familyName);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Google token is expired.");
        }
        catch (SecurityTokenException)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
        }
        catch (InvalidOperationException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
        }
        catch (ArgumentException)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
        }
    }

    private async Task<SocialProfile> ValidateGoogleAccessTokenAsync(string? accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Access token is required for Google login.");
        }

        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
        {
            throw new ApiException(HttpStatusCode.InternalServerError, "Google login configuration is missing. Set Google:ClientId before starting the app.");
        }

        try
        {
            using var client = new HttpClient();
            var userInfoUrl = $"https://www.googleapis.com/oauth2/v3/userinfo?access_token={Uri.EscapeDataString(accessToken)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
            }

            var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken: cancellationToken);

            if (userInfo is null
                || string.IsNullOrWhiteSpace(userInfo.Sub)
                || string.IsNullOrWhiteSpace(userInfo.Email))
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
            }

            if (!userInfo.EmailVerified)
            {
                throw new ApiException(HttpStatusCode.Unauthorized, "Google account email is not verified.");
            }

            // Verify the token belongs to our app by checking the audience (azp claim)
            // The Google UserInfo API doesn't return azp, so we rely on the token validation above

            return new SocialProfile(
                ProviderUserId: userInfo.Sub,
                Email: userInfo.Email,
                DisplayName: ResolveGoogleDisplayName(userInfo.Name, userInfo.GivenName, userInfo.FamilyName, userInfo.Email),
                PhotoUrl: userInfo.Picture,
                EmailVerified: true,
                GivenName: userInfo.GivenName,
                FamilyName: userInfo.FamilyName);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Google sign-in validation is temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Google token is invalid.");
        }
    }

    private async Task<SocialProfile> ValidateFacebookLoginAsync(string? accessToken, string? expectedUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "AccessToken is required for Facebook login.");
        }

        if (string.IsNullOrWhiteSpace(_facebookAuthOptions.AppId) || string.IsNullOrWhiteSpace(_facebookAuthOptions.AppSecret))
        {
            throw new ApiException(HttpStatusCode.InternalServerError, "Facebook login configuration is missing. Set Facebook:AppId and Facebook:AppSecret before starting the app.");
        }

        using var client = new HttpClient();
        var appAccessToken = $"{_facebookAuthOptions.AppId}|{_facebookAuthOptions.AppSecret}";
        var debugTokenUrl = $"https://graph.facebook.com/{_facebookAuthOptions.GraphApiVersion}/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

        FacebookDebugTokenResponse? debugToken;
        try
        {
            debugToken = await client.GetFromJsonAsync<FacebookDebugTokenResponse>(debugTokenUrl, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Facebook sign-in validation is temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Facebook sign-in validation is temporarily unavailable.");
        }

        if (debugToken?.Data is null
            || !debugToken.Data.IsValid
            || !debugToken.Data.AppId.Equals(_facebookAuthOptions.AppId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(debugToken.Data.UserId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Facebook token is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(expectedUserId)
            && !debugToken.Data.UserId.Equals(expectedUserId, StringComparison.Ordinal))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Facebook user ID does not match the access token.");
        }

        var profileUrl = $"https://graph.facebook.com/{_facebookAuthOptions.GraphApiVersion}/me?fields=id,name,email,picture.width(200).height(200)&access_token={Uri.EscapeDataString(accessToken)}";
        FacebookProfileResponse? profile;
        try
        {
            profile = await client.GetFromJsonAsync<FacebookProfileResponse>(profileUrl, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Facebook sign-in validation is temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "Facebook sign-in validation is temporarily unavailable.");
        }

        if (profile is null
            || string.IsNullOrWhiteSpace(profile.Id))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Facebook profile could not be read.");
        }

        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Facebook account did not provide an email address. Please use Google Login or email/password.");
        }

        return new SocialProfile(
            ProviderUserId: profile.Id,
            Email: profile.Email,
            DisplayName: ResolveFacebookDisplayName(profile.Name, profile.Email),
            PhotoUrl: profile.Picture?.Data?.Url,
            EmailVerified: true);
    }

    private static string NormalizeProvider(string provider)
    {
        if (provider?.Equals("Google", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Google";
        }

        if (provider?.Equals("Facebook", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Facebook";
        }

        return provider ?? string.Empty;
    }

    private sealed record SocialProfile(
        string ProviderUserId,
        string Email,
        string DisplayName,
        string? PhotoUrl,
        bool EmailVerified,
        string? GivenName = null,
        string? FamilyName = null);

    private sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugTokenData? Data { get; init; }
    }

    private sealed class FacebookDebugTokenData
    {
        [JsonPropertyName("is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("app_id")]
        public string AppId { get; init; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; init; } = string.Empty;
    }

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Sub { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; init; }

        [JsonPropertyName("picture")]
        public string? Picture { get; init; }
    }

    private sealed class FacebookProfileResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; init; }
    }

    private sealed class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; init; }
    }

    private sealed class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}
