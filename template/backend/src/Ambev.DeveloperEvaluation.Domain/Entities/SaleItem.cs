using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents an individual item within a sale.
/// Contains product information, quantity, pricing, and discount details.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Gets the identifier of the parent sale.
    /// </summary>
    public Guid SaleId { get; set; }

    /// <summary>
    /// Gets the external product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets the denormalized product name for display purposes.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the quantity of items purchased.
    /// Must be between 1 and 20.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets the unit price of the product at the time of sale.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets the discount applied to this item as a decimal (e.g., 0.10 for 10%).
    /// Calculated based on quantity rules.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Gets the total amount for this item after discount.
    /// Calculated as: Quantity * UnitPrice * (1 - Discount).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Indicates whether this item has been cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Navigation property back to the parent Sale.
    /// </summary>
    public Sale? Sale { get; set; }
}
