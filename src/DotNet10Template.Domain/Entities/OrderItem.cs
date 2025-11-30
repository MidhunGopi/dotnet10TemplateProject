using DotNet10Template.Domain.Common;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Order item entity for order line items
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    
    // Navigation properties
    public virtual Order? Order { get; set; }
    public virtual Product? Product { get; set; }
}
