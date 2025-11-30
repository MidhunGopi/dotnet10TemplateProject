using System.Text;
using System.Text.Json;
using DotNet10Template.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// RabbitMQ message broker service implementation
/// </summary>
public class RabbitMqService : IMessageBrokerService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ connection established");
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _jsonOptions));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug("Published message to queue {QueueName}", queueName);
    }

    public async Task PublishAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _jsonOptions));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug("Published message to exchange {ExchangeName} with routing key {RoutingKey}", exchangeName, routingKey);
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class
    {
        _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();

        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body), _jsonOptions);

                if (message != null)
                {
                    await handler(message);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogDebug("Message processed from queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer).GetAwaiter().GetResult();

        _logger.LogInformation("Subscribed to queue {QueueName}", queueName);
    }

    public void SubscribeWithExchange<T>(string exchangeName, string queueName, string routingKey, Func<T, Task> handler) where T : class
    {
        _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();

        _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            arguments: null).GetAwaiter().GetResult();

        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body), _jsonOptions);

                if (message != null)
                {
                    await handler(message);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from exchange {ExchangeName}", exchangeName);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer).GetAwaiter().GetResult();

        _logger.LogInformation("Subscribed to exchange {ExchangeName} with queue {QueueName}", exchangeName, queueName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }
}
