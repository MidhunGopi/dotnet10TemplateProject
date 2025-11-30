namespace DotNet10Template.Domain.Interfaces;

/// <summary>
/// Interface for current user service
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
