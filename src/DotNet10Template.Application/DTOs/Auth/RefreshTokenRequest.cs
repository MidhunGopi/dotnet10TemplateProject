namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// Refresh token request DTO
/// </summary>
public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);
