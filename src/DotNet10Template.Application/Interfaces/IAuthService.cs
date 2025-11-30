using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Auth;

namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for authentication service
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
}
