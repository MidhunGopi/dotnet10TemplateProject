using Microsoft.AspNetCore.Identity;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Custom role claim entity
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
    public string? Description { get; set; }
    public string? Group { get; set; }
    
    public virtual ApplicationRole Role { get; set; } = null!;
}
