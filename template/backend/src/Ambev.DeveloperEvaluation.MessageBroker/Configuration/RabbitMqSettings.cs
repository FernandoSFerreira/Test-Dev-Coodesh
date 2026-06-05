namespace Ambev.DeveloperEvaluation.MessageBroker.Configuration;

/// <summary>
/// POCO class for RabbitMQ connection settings.
/// Binds to the "RabbitMQ" section in appsettings.json.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// Gets or sets the hostname of the RabbitMQ server.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port of the RabbitMQ server.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange name to publish events to.
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the exchange (e.g., "topic", "direct").
    /// </summary>
    public string ExchangeType { get; set; } = string.Empty;
}
