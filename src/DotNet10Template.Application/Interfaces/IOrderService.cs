using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Orders;
using DotNet10Template.Domain.Entities;

namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for order service
/// </summary>
public interface IOrderService
{
    Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<OrderDto>>> GetAllAsync(PaginationParams parameters, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<OrderDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
}
