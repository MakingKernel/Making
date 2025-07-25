# Making.RabbitMQ

RabbitMQ client utilities and extensions for the Making framework.

## Overview

Making.RabbitMQ provides essential RabbitMQ client utilities and connection management for the Making framework. It offers a robust foundation for building messaging solutions with connection pooling, retry logic, and configuration management.

## Features

- **Connection Management**: Robust RabbitMQ connection handling with automatic reconnection
- **Configuration Support**: Easy configuration through options pattern
- **Connection Pooling**: Efficient connection reuse and management
- **Retry Logic**: Built-in retry mechanisms for failed operations
- **Logging Integration**: Comprehensive logging for debugging and monitoring
- **Dependency Injection**: Full DI container integration

## Installation

```bash
dotnet add package Making.RabbitMQ
```

## Usage

### Configuration

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "HostName": "localhost",
    "Port": 5672,
    "RequestedHeartbeat": 60,
    "NetworkRecoveryInterval": 10,
    "AutomaticRecoveryEnabled": true,
    "TopologyRecoveryEnabled": true
  }
}
```

### Register Services

```csharp
services.AddMarkRabbitMQ(configuration);
```

### Using RabbitMQ Connection

```csharp
public class MessageService
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<MessageService> _logger;
    
    public MessageService(IRabbitMqConnection connection, ILogger<MessageService> logger)
    {
        _connection = connection;
        _logger = logger;
    }
    
    public async Task SendMessageAsync(string queueName, string message)
    {
        using var channel = _connection.CreateChannel();
        
        // Declare queue
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        // Prepare message
        var body = Encoding.UTF8.GetBytes(message);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        
        // Publish message
        channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);
        
        _logger.LogInformation("Message sent to queue {QueueName}", queueName);
    }
    
    public async Task<string> ReceiveMessageAsync(string queueName)
    {
        using var channel = _connection.CreateChannel();
        
        // Declare queue
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        // Get message
        var result = channel.BasicGet(queueName, autoAck: true);
        
        if (result != null)
        {
            var message = Encoding.UTF8.GetString(result.Body.ToArray());
            _logger.LogInformation("Message received from queue {QueueName}", queueName);
            return message;
        }
        
        return null;
    }
}
```

### Advanced Usage with Options

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ConnectionString = "amqp://localhost:5672";
            options.AutomaticRecoveryEnabled = true;
            options.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            options.RequestedHeartbeat = TimeSpan.FromSeconds(60);
        });
        
        services.AddMarkRabbitMQ();
    }
}
```

## Requirements

- .NET Standard 2.0+
- RabbitMQ Server
- RabbitMQ.Client
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Options.ConfigurationExtensions
- Microsoft.Extensions.Logging
- System.Text.Json
- Making.Core

## License

This project is part of the Making framework.