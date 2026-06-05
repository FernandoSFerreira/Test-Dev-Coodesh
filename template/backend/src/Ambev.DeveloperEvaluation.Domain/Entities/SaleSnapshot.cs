namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a denormalized, immutable snapshot of a sale stored in MongoDB.
/// Preserves the exact state of the sale at the time of creation/update,
/// including all product prices and discounts, even if they change later.
/// </summary>
public class SaleSnapshot
{
    /// <summary>
    /// Gets the MongoDB document identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the identifier of the original sale in PostgreSQL.
    /// </summary>
    public Guid SaleId { get; set; }

    /// <summary>
    /// Gets the unique sale number.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets the date when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets the customer name at the time of the snapshot.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the branch identifier.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets the branch name at the time of the snapshot.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the total amount of the sale at the time of the snapshot.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Indicates whether the sale was cancelled at the time of the snapshot.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets the timestamp when this snapshot was created.
    /// </summary>
    public DateTime SnapshotCreatedAt { get; set; }

    /// <summary>
    /// Gets the list of item snapshots in this sale.
    /// </summary>
    public List<SaleItemSnapshot> Items { get; set; } = new();

    /// <summary>
    /// Creates a snapshot from a Sale entity.
    /// </summary>
    /// <param name="sale">The sale entity to snapshot.</param>
    /// <returns>An immutable snapshot of the sale.</returns>
    public static SaleSnapshot FromSale(Sale sale)
    {
        return new SaleSnapshot
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerName,
            BranchId = sale.BranchId,
            BranchName = sale.BranchName,
            TotalAmount = sale.TotalAmount,
            IsCancelled = sale.IsCancelled,
            SnapshotCreatedAt = DateTime.UtcNow,
            Items = sale.Items.Select(i => new SaleItemSnapshot
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                TotalAmount = i.TotalAmount,
                IsCancelled = i.IsCancelled
            }).ToList()
        };
    }
}

/// <summary>
/// Represents a denormalized, immutable snapshot of a sale item stored in MongoDB.
/// </summary>
public class SaleItemSnapshot
{
    /// <summary>
    /// Gets the product identifier at the time of the snapshot.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets the product name at the time of the snapshot.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the quantity purchased.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets the unit price at the time of the snapshot.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets the discount applied at the time of the snapshot.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Gets the total amount for this item at the time of the snapshot.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Indicates whether this item was cancelled at the time of the snapshot.
    /// </summary>
    public bool IsCancelled { get; set; }
}
