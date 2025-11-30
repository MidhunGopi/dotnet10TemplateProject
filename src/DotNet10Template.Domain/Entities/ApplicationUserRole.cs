using Microsoft.AspNetCore.Identity;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Junction entity for user-role many-to-many relationship
/// </summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationRole Role { get; set; } = null!;
}
