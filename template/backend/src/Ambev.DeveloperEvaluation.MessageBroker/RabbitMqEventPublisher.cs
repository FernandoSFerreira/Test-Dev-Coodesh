using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.MessageBroker.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ambev.DeveloperEvaluation.MessageBroker;

/// <summary>
/// Implementation of IEventPublisher using RabbitMQ.
/// Serializes domain events to JSON and publishes them to a topic exchange.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _settings = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Serializes and publishes the domain event to RabbitMQ.
    /// Uses the event type name (formatted) as the routing key.
    /// </summary>
    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : INotification
    {
        var eventType = typeof(T).Name;
        var routingKey = GetRoutingKey(eventType);

        try
        {
            using var channel = _connectionFactory.CreateChannel();
            
            var payload = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(payload);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new RabbitMQ.Client.AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = eventType;

            channel.BasicPublish(
                exchange: _settings.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Event {EventType} published successfully to exchange {Exchange} with routing key {RoutingKey}",
                eventType, _settings.Exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to RabbitMQ", eventType);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Converts PascalCase event class names to dot-separated routing keys.
    /// Example: "SaleCreatedEvent" -> "sale.created"
    /// </summary>
    private static string GetRoutingKey(string eventName)
    {
        // Remove the "Event" suffix if present
        if (eventName.EndsWith("Event"))
        {
            eventName = eventName[..^5];
        }

        // Convert PascalCase to dot.separated.lowercase
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < eventName.Length; i++)
        {
            var c = eventName[i];
            if (char.IsUpper(c) && i > 0)
            {
                stringBuilder.Append('.');
            }
            stringBuilder.Append(char.ToLower(c));
        }

        return stringBuilder.ToString();
    }
}
