using System.Security.Claims;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Orders;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNet10Template.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PaginatedList<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams parameters, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetAllAsync(parameters, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.Errors.FirstOrDefault() });
        }

        // Check if user owns the order or is admin
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && result.Data!.UserId.ToString() != userId)
        {
            return Forbid();
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get current user's orders
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var result = await _orderService.GetUserOrdersAsync(userGuid, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Get orders by status (Admin only)
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByStatus(OrderStatus status, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrdersByStatusAsync(status, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var result = await _orderService.CreateAsync(userGuid, request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Errors.FirstOrDefault(), errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { message = result.Errors.FirstOrDefault() });
            }
            return BadRequest(new { message = result.Errors.FirstOrDefault(), errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        // First get the order to check ownership
        var orderResult = await _orderService.GetByIdAsync(id, cancellationToken);
        if (!orderResult.Succeeded)
        {
            return NotFound(new { message = orderResult.Errors.FirstOrDefault() });
        }

        // Check if user owns the order or is admin
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && orderResult.Data!.UserId.ToString() != userId)
        {
            return Forbid();
        }

        var result = await _orderService.CancelOrderAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Order cancelled successfully" });
    }
}
