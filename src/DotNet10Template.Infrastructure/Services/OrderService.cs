using AutoMapper;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Orders;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Order service implementation
/// </summary>
public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageBrokerService _messageBroker;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IRepository<Order> orderRepository,
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageBrokerService messageBroker,
        ICurrentUserService currentUserService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageBroker = messageBroker;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.AsQueryable()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            return Result<OrderDto>.Failure("Order not found");
        }

        var dto = _mapper.Map<OrderDto>(order);
        return Result<OrderDto>.Success(dto);
    }

    public async Task<Result<PaginatedList<OrderDto>>> GetAllAsync(PaginationParams parameters, CancellationToken cancellationToken = default)
    {
        var query = _orderRepository.AsQueryable()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        // Search by order number
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            query = query.Where(o => o.OrderNumber.Contains(parameters.SearchTerm));
        }

        // Sort
        query = parameters.SortBy?.ToLower() switch
        {
            "ordernumber" => parameters.SortDescending ? query.OrderByDescending(o => o.OrderNumber) : query.OrderBy(o => o.OrderNumber),
            "totalamount" => parameters.SortDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
            "status" => parameters.SortDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            _ => query.OrderByDescending(o => o.OrderDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<OrderDto>>(items);
        var result = new PaginatedList<OrderDto>(dtos, totalCount, parameters.PageNumber, parameters.PageSize);

        return Result<PaginatedList<OrderDto>>.Success(result);
    }

    public async Task<Result<IEnumerable<OrderDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.AsQueryable()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
        return Result<IEnumerable<OrderDto>>.Success(dtos);
    }

    public async Task<Result<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return Result<OrderDto>.Failure("Order must have at least one item");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                ShippingAddress = request.ShippingAddress,
                Notes = request.Notes,
                Status = OrderStatus.Pending
            };

            decimal totalAmount = 0;

            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product == null)
                {
                    return Result<OrderDto>.Failure($"Product {item.ProductId} not found");
                }

                if (!product.IsAvailable)
                {
                    return Result<OrderDto>.Failure($"Product {product.Name} is not available");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return Result<OrderDto>.Failure($"Insufficient stock for {product.Name}");
                }

                // Reduce stock
                product.StockQuantity -= item.Quantity;
                await _productRepository.UpdateAsync(product, cancellationToken);

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                order.OrderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            order.TotalAmount = totalAmount;
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish event
            await _messageBroker.PublishAsync("order.created", new
            {
                OrderId = order.Id,
                order.OrderNumber,
                order.UserId,
                order.TotalAmount
            }, cancellationToken);

            _logger.LogInformation("Order {OrderId} created for user {UserId}", order.Id, userId);

            // Reload with includes
            var createdOrder = await _orderRepository.AsQueryable()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstAsync(o => o.Id == order.Id, cancellationToken);

            var dto = _mapper.Map<OrderDto>(createdOrder);
            return Result<OrderDto>.Success(dto, "Order created successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating order for user {UserId}", userId);
            return Result<OrderDto>.Failure("An error occurred while creating the order");
        }
    }

    public async Task<Result<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.AsQueryable()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            return Result<OrderDto>.Failure("Order not found");
        }

        var previousStatus = order.Status;
        order.Status = request.Status;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = request.Notes;
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _messageBroker.PublishAsync("order.status.updated", new
        {
            OrderId = order.Id,
            order.OrderNumber,
            PreviousStatus = previousStatus.ToString(),
            NewStatus = request.Status.ToString()
        }, cancellationToken);

        _logger.LogInformation("Order {OrderId} status updated from {PreviousStatus} to {NewStatus}",
            id, previousStatus, request.Status);

        var dto = _mapper.Map<OrderDto>(order);
        return Result<OrderDto>.Success(dto, "Order status updated successfully");
    }

    public async Task<Result> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.AsQueryable()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            return Result.Failure("Order not found");
        }

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
        {
            return Result.Failure("Cannot cancel shipped or delivered orders");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Result.Failure("Order is already cancelled");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Restore stock
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    await _productRepository.UpdateAsync(product, cancellationToken);
                }
            }

            order.Status = OrderStatus.Cancelled;
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish event
            await _messageBroker.PublishAsync("order.cancelled", new
            {
                OrderId = order.Id,
                order.OrderNumber
            }, cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled", id);

            return Result.Success("Order cancelled successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return Result.Failure("An error occurred while cancelling the order");
        }
    }

    public async Task<Result<IEnumerable<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.AsQueryable()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
        return Result<IEnumerable<OrderDto>>.Success(dtos);
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
