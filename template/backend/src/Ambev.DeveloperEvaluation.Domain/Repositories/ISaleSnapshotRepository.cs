using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Repository interface for sale snapshot operations (MongoDB).
/// Manages denormalized, immutable sale records for historical purposes.
/// </summary>
public interface ISaleSnapshotRepository
{
    /// <summary>
    /// Saves a snapshot of a sale to MongoDB.
    /// </summary>
    /// <param name="snapshot">The sale snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSnapshotAsync(SaleSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the latest snapshot of a sale by its sale identifier.
    /// </summary>
    /// <param name="saleId">The sale identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot if found, null otherwise.</returns>
    Task<SaleSnapshot?> GetSnapshotAsync(Guid saleId, CancellationToken cancellationToken = default);
}
