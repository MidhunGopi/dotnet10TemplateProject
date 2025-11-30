namespace DotNet10Template.Domain.Common;

/// <summary>
/// Base entity with auditable properties
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
