using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Auth;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Account is deactivated");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Result<AuthResponse>.Failure("Account is locked. Please try again later.");
        }

        if (!result.Succeeded)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        var claims = await GetUserClaimsAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Save refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"));
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.ProfilePictureUrl,
            user.UserRoles.Select(ur => ur.Role.Name!));

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiration(),
            userDto));
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("Email is already registered");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false // Require email confirmation
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<AuthResponse>.Failure(errors);
        }

        // Assign default role
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "User", Description = "Default user role" });
        }
        await _userManager.AddToRoleAsync(user, "User");

        // Generate email confirmation token
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        // TODO: Send email confirmation (implement email sending)

        // Generate tokens for immediate login
        var claims = await GetUserClaimsAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"));
        await _userManager.UpdateAsync(user);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.ProfilePictureUrl,
            new[] { "User" });

        _logger.LogInformation("User {Email} registered successfully", user.Email);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiration(),
            userDto));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return Result<AuthResponse>.Failure("Invalid access token");
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Result<AuthResponse>.Failure("Invalid access token");
        }

        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Invalid or expired refresh token");
        }

        var claims = await GetUserClaimsAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"));
        await _userManager.UpdateAsync(user);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.ProfilePictureUrl,
            user.UserRoles.Select(ur => ur.Role.Name!));

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiration(),
            userDto));
    }

    public async Task<Result> RevokeTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        return Result.Success("Token revoked successfully");
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return Result.Failure("Passwords do not match");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure(errors);
        }

        _logger.LogInformation("User {UserId} changed password successfully", userId);

        return Result.Success("Password changed successfully");
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user doesn't exist
            return Result.Success("If an account with that email exists, a password reset link has been sent.");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with reset link
        await _emailService.SendEmailAsync(
            user.Email!,
            "Password Reset Request",
            $"Your password reset token is: {resetToken}");

        _logger.LogInformation("Password reset requested for {Email}", request.Email);

        return Result.Success("If an account with that email exists, a password reset link has been sent.");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result.Failure("Invalid request");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return Result.Failure("Passwords do not match");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure(errors);
        }

        _logger.LogInformation("Password reset successfully for {Email}", request.Email);

        return Result.Success("Password has been reset successfully");
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure(errors);
        }

        _logger.LogInformation("Email confirmed for {UserId}", userId);

        return Result.Success("Email confirmed successfully");
    }

    private async Task<IEnumerable<Claim>> GetUserClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        return claims;
    }
}
