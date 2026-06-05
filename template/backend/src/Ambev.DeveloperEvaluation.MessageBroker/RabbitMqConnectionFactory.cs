using Ambev.DeveloperEvaluation.MessageBroker.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Ambev.DeveloperEvaluation.MessageBroker;

/// <summary>
/// Singleton factory responsible for managing the connection to RabbitMQ.
/// </summary>
public class RabbitMqConnectionFactory : IDisposable
{
    private readonly IConnection _connection;
    private readonly RabbitMqSettings _settings;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance and establishes the connection to RabbitMQ.
    /// Declares the exchange if it does not exist.
    /// </summary>
    /// <param name="options">The RabbitMQ settings.</param>
    public RabbitMqConnectionFactory(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        // In a real production scenario, we should handle connection retries (e.g., with Polly)
        _connection = factory.CreateConnection();

        // Ensure the exchange exists
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: _settings.Exchange,
            type: _settings.ExchangeType,
            durable: true,
            autoDelete: false);
    }

    /// <summary>
    /// Creates a new channel (model) on the existing connection.
    /// It's recommended to have one channel per thread/operation.
    /// </summary>
    /// <returns>A new IModel instance.</returns>
    public IModel CreateChannel()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqConnectionFactory));

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        if (_connection is { IsOpen: true })
        {
            _connection.Close();
        }
        
        _connection?.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}
