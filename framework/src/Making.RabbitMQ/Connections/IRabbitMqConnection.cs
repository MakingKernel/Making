using RabbitMQ.Client;

namespace Making.RabbitMQ.Connections;

/// <summary>
/// Defines the interface for RabbitMQ connection management.
/// </summary>
public interface IRabbitMqConnection : IDisposable
{
    /// <summary>
    /// Gets whether the connection is open.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    /// <returns>The created channel.</returns>
    Task<IChannel> CreateModelAsync();

    /// <summary>
    /// Tries to connect to RabbitMQ.
    /// </summary>
    /// <returns>True if connection was successful, false otherwise.</returns>
    Task<bool> TryConnect();
}