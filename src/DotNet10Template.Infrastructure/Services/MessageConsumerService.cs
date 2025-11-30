using DotNet10Template.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Background service for consuming RabbitMQ messages
/// </summary>
public class MessageConsumerService : BackgroundService
{
    private readonly IMessageBrokerService _messageBroker;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService(
        IMessageBrokerService messageBroker,
        ILogger<MessageConsumerService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Consumer Service starting...");

        // Subscribe to product events
        _messageBroker.Subscribe<ProductCreatedMessage>("product.created", async message =>
        {
            _logger.LogInformation("Product created: {ProductId} - {ProductName}", message.ProductId, message.Name);
            // Add any additional processing logic here
            await Task.CompletedTask;
        });

        _messageBroker.Subscribe<ProductUpdatedMessage>("product.updated", async message =>
        {
            _logger.LogInformation("Product updated: {ProductId} - {ProductName}", message.ProductId, message.Name);
            await Task.CompletedTask;
        });

        // Subscribe to order events
        _messageBroker.Subscribe<OrderCreatedMessage>("order.created", async message =>
        {
            _logger.LogInformation("Order created: {OrderId} - {OrderNumber} for user {UserId}", 
                message.OrderId, message.OrderNumber, message.UserId);
            // Send order confirmation email, update analytics, etc.
            await Task.CompletedTask;
        });

        _messageBroker.Subscribe<OrderStatusUpdatedMessage>("order.status.updated", async message =>
        {
            _logger.LogInformation("Order {OrderId} status changed from {PreviousStatus} to {NewStatus}",
                message.OrderId, message.PreviousStatus, message.NewStatus);
            // Send status update notification
            await Task.CompletedTask;
        });

        _messageBroker.Subscribe<OrderCancelledMessage>("order.cancelled", async message =>
        {
            _logger.LogInformation("Order cancelled: {OrderId} - {OrderNumber}", message.OrderId, message.OrderNumber);
            // Process refund, update inventory, etc.
            await Task.CompletedTask;
        });

        _logger.LogInformation("Message Consumer Service started");
        return Task.CompletedTask;
    }
}

#region Message Classes

public record ProductCreatedMessage(Guid ProductId, string Name);
public record ProductUpdatedMessage(Guid ProductId, string Name);
public record ProductDeletedMessage(Guid ProductId);

public record OrderCreatedMessage(Guid OrderId, string OrderNumber, Guid UserId, decimal TotalAmount);
public record OrderStatusUpdatedMessage(Guid OrderId, string OrderNumber, string PreviousStatus, string NewStatus);
public record OrderCancelledMessage(Guid OrderId, string OrderNumber);

#endregion
