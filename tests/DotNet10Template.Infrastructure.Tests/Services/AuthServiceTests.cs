using System.Security.Claims;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Auth;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNet10Template.Infrastructure.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, contextAccessor.Object, userPrincipalFactory.Object,
            null, null, null, null);

        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
        _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object, null, null, null, null);

        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockEmailService = new Mock<IEmailService>();

        _mockConfiguration.Setup(x => x["JwtSettings:RefreshTokenExpirationDays"]).Returns("7");

        _authService = new AuthService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockRoleManager.Object,
            _mockJwtTokenService.Object,
            null!,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockEmailService.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "Password123!");

        _mockUserManager
            .Setup(x => x.Users)
            .Returns(new List<ApplicationUser>().AsQueryable());

        var result = await _authService.LoginAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            IsActive = false
        };

        var users = new List<ApplicationUser> { user }.AsQueryable();

        _mockUserManager
            .Setup(x => x.Users)
            .Returns(users);

        var result = await _authService.LoginAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Account is deactivated");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIncorrect_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "WrongPassword!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            IsActive = true
        };

        var users = new List<ApplicationUser> { user }.AsQueryable();

        _mockUserManager
            .Setup(x => x.Users)
            .Returns(users);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _authService.LoginAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLockedOut_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            IsActive = true
        };

        var users = new List<ApplicationUser> { user }.AsQueryable();

        _mockUserManager
            .Setup(x => x.Users)
            .Returns(users);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await _authService.LoginAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Account is locked. Please try again later.");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        var request = new RegisterRequest(
            "test@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            null
        );

        var existingUser = new ApplicationUser { Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        var result = await _authService.RegisterAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email is already registered");
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordValidationFails_ReturnsFailure()
    {
        var request = new RegisterRequest(
            "test@example.com",
            "weak",
            "weak",
            "John",
            "Doe",
            null
        );

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password is too weak" }
            ));

        var result = await _authService.RegisterAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Password is too weak");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();
        var request = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "NewPassword123!");

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.ChangePasswordAsync(userId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenPasswordsDoNotMatch_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();
        var request = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "DifferentPassword123!");
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        var result = await _authService.ChangePasswordAsync(userId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIncorrect_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();
        var request = new ChangePasswordRequest("WrongPassword!", "NewPassword123!", "NewPassword123!");
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Current password is incorrect" }
            ));

        var result = await _authService.ChangePasswordAsync(userId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Current password is incorrect");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenValidRequest_ReturnsSuccess()
    {
        var userId = Guid.NewGuid().ToString();
        var request = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "NewPassword123!");
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.ChangePasswordAsync(userId, request);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Password changed successfully");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserExists_SendsEmailAndReturnsSuccess()
    {
        var request = new ForgotPasswordRequest("test@example.com");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token-123");

        _mockEmailService
            .Setup(x => x.SendEmailAsync(user.Email, "Password Reset Request", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _authService.ForgotPasswordAsync(request);

        result.Succeeded.Should().BeTrue();
        _mockEmailService.Verify(x => x.SendEmailAsync(user.Email, "Password Reset Request", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ReturnsSuccessWithoutRevealingUserExistence()
    {
        var request = new ForgotPasswordRequest("nonexistent@example.com");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.ForgotPasswordAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Contain("If an account with that email exists");
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserNotFound_ReturnsFailure()
    {
        var request = new ResetPasswordRequest("test@example.com", "reset-token", "NewPassword123!", "NewPassword123!");

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.ResetPasswordAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid request");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordsDoNotMatch_ReturnsFailure()
    {
        var request = new ResetPasswordRequest("test@example.com", "reset-token", "NewPassword123!", "DifferentPassword!");
        var user = new ApplicationUser { Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var result = await _authService.ResetPasswordAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenInvalid_ReturnsFailure()
    {
        var request = new ResetPasswordRequest("test@example.com", "invalid-token", "NewPassword123!", "NewPassword123!");
        var user = new ApplicationUser { Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ResetPasswordAsync(user, request.Token, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Invalid token" }
            ));

        var result = await _authService.ResetPasswordAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenValidRequest_ReturnsSuccess()
    {
        var request = new ResetPasswordRequest("test@example.com", "valid-token", "NewPassword123!", "NewPassword123!");
        var user = new ApplicationUser { Email = "test@example.com" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ResetPasswordAsync(user, request.Token, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.ResetPasswordAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Password has been reset successfully");
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenUserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.RevokeTokenAsync(userId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenUserExists_RevokesTokenSuccessfully()
    {
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = Guid.Parse(userId),
            RefreshToken = "some-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RevokeTokenAsync(userId);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Token revoked successfully");
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenUserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();
        var token = "email-confirmation-token";

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.ConfirmEmailAsync(userId, token);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenTokenInvalid_ReturnsFailure()
    {
        var userId = Guid.NewGuid().ToString();
        var token = "invalid-token";
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Invalid token" }
            ));

        var result = await _authService.ConfirmEmailAsync(userId, token);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenValidRequest_ReturnsSuccess()
    {
        var userId = Guid.NewGuid().ToString();
        var token = "valid-token";
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.ConfirmEmailAsync(userId, token);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Email confirmed successfully");
    }
}
