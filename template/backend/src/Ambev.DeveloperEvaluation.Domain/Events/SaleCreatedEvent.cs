using Ambev.DeveloperEvaluation.Domain.Entities;
using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Domain event raised when a new sale is created.
/// Published to RabbitMQ for external consumers.
/// </summary>
public class SaleCreatedEvent : INotification
{
    /// <summary>
    /// Gets the sale that was created.
    /// </summary>
    public Sale Sale { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; }

    public SaleCreatedEvent(Sale sale)
    {
        Sale = sale;
        OccurredAt = DateTime.UtcNow;
    }
}
