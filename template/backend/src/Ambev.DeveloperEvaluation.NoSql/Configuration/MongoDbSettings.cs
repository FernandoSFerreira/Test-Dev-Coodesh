namespace Ambev.DeveloperEvaluation.NoSql.Configuration;

/// <summary>
/// POCO class for MongoDB connection settings.
/// Binds to the "ConnectionStrings" section in appsettings.json.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// Example: "mongodb://developer:ev%40luAt10n@localhost:27017"
    /// </summary>
    public string MongoConnection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDB database name.
    /// Example: "developer_evaluation"
    /// </summary>
    public string MongoDatabase { get; set; } = string.Empty;
}
