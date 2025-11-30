namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// Change password request DTO
/// </summary>
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);
