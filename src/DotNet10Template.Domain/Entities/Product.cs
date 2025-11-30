using DotNet10Template.Domain.Common;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Sample product entity for CRUD operations demonstration
/// </summary>
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? SKU { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Guid? CategoryId { get; set; }
    
    // Navigation property
    public virtual Category? Category { get; set; }
}
