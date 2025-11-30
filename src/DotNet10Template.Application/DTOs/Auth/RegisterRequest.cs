namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// User registration request DTO
/// </summary>
public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber = null
);
