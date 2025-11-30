using Microsoft.AspNetCore.Identity;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Application user extending IdentityUser with custom properties
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
