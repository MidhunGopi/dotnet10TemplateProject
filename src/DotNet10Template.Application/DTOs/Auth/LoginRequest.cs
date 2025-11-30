namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// User login request DTO
/// </summary>
public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);
