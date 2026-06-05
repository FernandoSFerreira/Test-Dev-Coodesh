using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.NoSql.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.NoSql.Context;

/// <summary>
/// Wrapper around IMongoDatabase that provides typed access to MongoDB collections.
/// Registered as a Singleton in the DI container.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of MongoDbContext with the configured settings.
    /// </summary>
    /// <param name="settings">MongoDB connection settings from appsettings.json.</param>
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.MongoConnection);
        _database = client.GetDatabase(settings.Value.MongoDatabase);
    }

    /// <summary>
    /// Gets the "sale_snapshots" collection for storing sale snapshots.
    /// </summary>
    public IMongoCollection<SaleSnapshot> SaleSnapshots
        => _database.GetCollection<SaleSnapshot>("sale_snapshots");
}
