using DotNet10Template.Domain.Entities;

namespace DotNet10Template.Application.DTOs.Orders;

/// <summary>
/// Order response DTO
/// </summary>
public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    string? UserName,
    DateTime OrderDate,
    OrderStatus Status,
    decimal TotalAmount,
    string? ShippingAddress,
    string? Notes,
    List<OrderItemDto> Items,
    DateTime CreatedAt
);

/// <summary>
/// Order item response DTO
/// </summary>
public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
