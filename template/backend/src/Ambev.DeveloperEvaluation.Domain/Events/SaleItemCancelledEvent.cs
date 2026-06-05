using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Domain event raised when a specific item within a sale is cancelled.
/// Published to RabbitMQ for external consumers.
/// </summary>
public class SaleItemCancelledEvent : INotification
{
    /// <summary>
    /// Gets the identifier of the sale containing the cancelled item.
    /// </summary>
    public Guid SaleId { get; }

    /// <summary>
    /// Gets the identifier of the cancelled item.
    /// </summary>
    public Guid ItemId { get; }

    /// <summary>
    /// Gets the product name of the cancelled item.
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; }

    public SaleItemCancelledEvent(Guid saleId, Guid itemId, string productName)
    {
        SaleId = saleId;
        ItemId = itemId;
        ProductName = productName;
        OccurredAt = DateTime.UtcNow;
    }
}
