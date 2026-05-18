using ClinicApp.Application.Common.Models.Authentication;
using ClinicApp.Application.Features.Auth.Dtos;

namespace ClinicApp.Application.Common.Interfaces.Authentication;

public interface IJwtTokenService
{
    GeneratedToken GenerateAccessToken(AuthUserDto user, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
