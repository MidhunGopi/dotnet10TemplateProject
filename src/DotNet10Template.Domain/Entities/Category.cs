using DotNet10Template.Domain.Common;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Category entity for product categorization
/// </summary>
public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ParentCategoryId { get; set; }
    
    // Navigation properties
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
