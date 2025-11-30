using System.Security.Claims;

namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for JWT token service
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    DateTime GetAccessTokenExpiration();
}
