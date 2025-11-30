using DotNet10Template.Domain.Entities;

namespace DotNet10Template.Application.DTOs.Orders;

/// <summary>
/// Update order status request DTO
/// </summary>
public record UpdateOrderStatusRequest(
    OrderStatus Status,
    string? Notes = null
);
