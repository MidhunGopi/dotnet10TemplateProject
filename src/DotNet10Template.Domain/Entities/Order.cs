using DotNet10Template.Domain.Common;

namespace DotNet10Template.Domain.Entities;

/// <summary>
/// Order entity for order management
/// </summary>
public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
