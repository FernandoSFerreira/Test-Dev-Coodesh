using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Implementation of ISaleRepository using Entity Framework Core.
/// Provides CRUD operations and paginated listing for sales.
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of SaleRepository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new sale in the database.
    /// </summary>
    /// <param name="sale">The sale to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sale.</returns>
    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    /// <summary>
    /// Retrieves a sale by its unique identifier, including all items.
    /// </summary>
    /// <param name="id">The unique identifier of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale if found, null otherwise.</returns>
    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Updates an existing sale in the database.
    /// </summary>
    /// <param name="sale">The sale with updated data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sale.</returns>
    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    /// <summary>
    /// Deletes a sale from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the sale to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the sale was deleted, false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Lists sales with pagination and optional ordering.
    /// Includes all sale items in the result.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="size">The page size.</param>
    /// <param name="orderBy">The field to order by (optional). Prefix with "-" for descending.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with the paginated items and total count.</returns>
    public async Task<(IEnumerable<Sale> Items, int TotalCount)> ListAsync(
        int page = 1,
        int size = 10,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales
            .Include(s => s.Items)
            .AsQueryable();

        // Apply ordering
        query = ApplyOrdering(query, orderBy);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Applies ordering to the query based on the orderBy parameter.
    /// Supports: saleDate, saleNumber, customerName, branchName, totalAmount.
    /// Prefix with "-" for descending order (e.g., "-saleDate").
    /// </summary>
    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> query, string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return query.OrderByDescending(s => s.CreatedAt);

        var descending = orderBy.StartsWith('-');
        var field = descending ? orderBy[1..] : orderBy;

        return field.ToLowerInvariant() switch
        {
            "saledate" => descending ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate),
            "salenumber" => descending ? query.OrderByDescending(s => s.SaleNumber) : query.OrderBy(s => s.SaleNumber),
            "customername" => descending ? query.OrderByDescending(s => s.CustomerName) : query.OrderBy(s => s.CustomerName),
            "branchname" => descending ? query.OrderByDescending(s => s.BranchName) : query.OrderBy(s => s.BranchName),
            "totalamount" => descending ? query.OrderByDescending(s => s.TotalAmount) : query.OrderBy(s => s.TotalAmount),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };
    }
}
