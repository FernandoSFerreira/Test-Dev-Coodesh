using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Interface for publishing domain events to an external message broker (RabbitMQ).
/// Events are serialized to JSON and published to a topic exchange.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the message broker.
    /// </summary>
    /// <typeparam name="T">The type of domain event implementing INotification.</typeparam>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : INotification;
}
