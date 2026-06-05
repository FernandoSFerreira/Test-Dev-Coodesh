using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.NoSql.Context;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.NoSql.Repositories;

/// <summary>
/// Implementation of ISaleSnapshotRepository using MongoDB.
/// Stores denormalized, immutable sale records for historical purposes.
/// </summary>
public class SaleSnapshotRepository : ISaleSnapshotRepository
{
    private readonly MongoDbContext _context;

    /// <summary>
    /// Initializes a new instance of SaleSnapshotRepository.
    /// </summary>
    /// <param name="context">The MongoDB context.</param>
    public SaleSnapshotRepository(MongoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Saves a snapshot of a sale to MongoDB.
    /// Each save creates a new document (immutable history).
    /// </summary>
    /// <param name="snapshot">The sale snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveSnapshotAsync(SaleSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _context.SaleSnapshots.InsertOneAsync(snapshot, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves the latest snapshot of a sale by its sale identifier.
    /// Returns the most recent snapshot sorted by SnapshotCreatedAt descending.
    /// </summary>
    /// <param name="saleId">The sale identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest snapshot if found, null otherwise.</returns>
    public async Task<SaleSnapshot?> GetSnapshotAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SaleSnapshot>.Filter.Eq(s => s.SaleId, saleId);
        var sort = Builders<SaleSnapshot>.Sort.Descending(s => s.SnapshotCreatedAt);

        return await _context.SaleSnapshots
            .Find(filter)
            .Sort(sort)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
