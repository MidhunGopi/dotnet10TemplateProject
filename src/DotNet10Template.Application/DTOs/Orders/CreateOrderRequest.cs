namespace DotNet10Template.Application.DTOs.Orders;

/// <summary>
/// Create order request DTO
/// </summary>
public record CreateOrderRequest(
    string? ShippingAddress,
    string? Notes,
    List<CreateOrderItemRequest> Items
);

/// <summary>
/// Create order item request DTO
/// </summary>
public record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity
);
