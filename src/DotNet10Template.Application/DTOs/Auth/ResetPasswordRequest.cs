namespace DotNet10Template.Application.DTOs.Auth;

/// <summary>
/// Reset password request DTO
/// </summary>
public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmNewPassword
);

/// <summary>
/// Forgot password request DTO
/// </summary>
public record ForgotPasswordRequest(
    string Email
);
