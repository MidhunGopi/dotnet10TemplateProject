namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for message broker service (RabbitMQ)
/// </summary>
public interface IMessageBrokerService
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default) where T : class;
    void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class;
    void SubscribeWithExchange<T>(string exchangeName, string queueName, string routingKey, Func<T, Task> handler) where T : class;
}
