using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a sale transaction in the system.
/// Contains business rules for discount calculation, item management, and cancellation.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>
    /// Gets the unique sale number identifier (e.g., "SALE-20260605-XXXX").
    /// Auto-generated upon creation.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets the date when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets the external customer identifier.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets the denormalized customer name for display purposes.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the external branch identifier where the sale was made.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets the denormalized branch name for display purposes.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of items in this sale.
    /// </summary>
    public List<SaleItem> Items { get; set; } = new();

    /// <summary>
    /// Gets the total amount of the sale (sum of all non-cancelled items' totals).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Indicates whether the entire sale has been cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets the date and time when the sale was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets the date and time of the last update to the sale.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the Sale class.
    /// Auto-generates the SaleNumber and sets the creation timestamp.
    /// </summary>
    public Sale()
    {
        CreatedAt = DateTime.UtcNow;
        SaleDate = DateTime.UtcNow;
        SaleNumber = GenerateSaleNumber();
    }

    /// <summary>
    /// Adds an item to the sale, applying discount rules based on quantity.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="quantity">The quantity of items (must be between 1 and 20).</param>
    /// <param name="unitPrice">The unit price of the product.</param>
    /// <returns>The created SaleItem with discount applied.</returns>
    /// <exception cref="BusinessRuleException">Thrown when quantity exceeds 20 items.</exception>
    public SaleItem AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ValidateQuantityLimit(quantity);

        var discount = CalculateItemDiscount(quantity);
        var itemTotal = quantity * unitPrice * (1 - discount);

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = Id,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Discount = discount,
            TotalAmount = itemTotal,
            IsCancelled = false
        };

        Items.Add(item);
        RecalculateTotal();

        return item;
    }

    /// <summary>
    /// Calculates the discount percentage based on the quantity.
    /// </summary>
    /// <param name="quantity">The quantity of items.</param>
    /// <returns>The discount as a decimal (e.g., 0.10 for 10%).</returns>
    public static decimal CalculateItemDiscount(int quantity)
    {
        return quantity switch
        {
            >= 10 and <= 20 => 0.20m, // 20% discount
            >= 4 and < 10 => 0.10m,   // 10% discount
            _ => 0m                    // No discount
        };
    }

    /// <summary>
    /// Cancels the entire sale.
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels a specific item in the sale and recalculates the total.
    /// </summary>
    /// <param name="itemId">The identifier of the item to cancel.</param>
    /// <exception cref="DomainException">Thrown when the item is not found.</exception>
    public void CancelItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Sale item with ID {itemId} not found in this sale.");

        item.IsCancelled = true;
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates the total amount based on all non-cancelled items.
    /// </summary>
    public void RecalculateTotal()
    {
        TotalAmount = Items
            .Where(i => !i.IsCancelled)
            .Sum(i => i.TotalAmount);
    }

    /// <summary>
    /// Performs validation of the sale entity using the SaleValidator rules.
    /// </summary>
    /// <returns>A ValidationResultDetail with validation results.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    /// <summary>
    /// Validates that the quantity does not exceed the maximum allowed (20 items).
    /// </summary>
    private static void ValidateQuantityLimit(int quantity)
    {
        if (quantity > 20)
            throw new BusinessRuleException($"Cannot sell more than 20 identical items. Requested: {quantity}.");
    }

    /// <summary>
    /// Generates a unique sale number with date prefix.
    /// </summary>
    private static string GenerateSaleNumber()
    {
        return $"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
