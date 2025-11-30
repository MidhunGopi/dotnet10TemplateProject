namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// Authentication response DTO
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

/// <summary>
/// User information DTO
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? ProfilePictureUrl,
    IEnumerable<string> Roles
);
