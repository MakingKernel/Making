# Making.Events.RabbitMQ

RabbitMQ event bus implementation for the Making framework.

## Overview

Making.Events.RabbitMQ provides a distributed event bus implementation using RabbitMQ for the Making framework. It enables event-driven communication across microservices and distributed applications with reliable message delivery and persistence.

## Features

- **Distributed Event Bus**: RabbitMQ-based event bus for cross-service communication
- **Reliable Messaging**: Persistent queues and acknowledgments
- **Dead Letter Queues**: Handle failed message processing
- **Exchange Management**: Automatic exchange and queue setup
- **Serialization**: JSON-based event serialization
- **Connection Management**: Resilient connection handling with retry logic

## Installation

```bash
dotnet add package Making.Events.RabbitMQ
```

## Usage

### Configuration

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "ExchangeName": "mark.events",
    "QueueName": "mark.events.queue",
    "RetryCount": 3,
    "RetryDelayMs": 1000
  }
}
```

### Register Services

```csharp
services.AddMarkRabbitMQEvents(configuration);
```

### Define Events

```csharp
public class OrderCreatedEvent : IEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Event Handlers

```csharp
public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Process order created event
        Console.WriteLine($"Processing order {@event.OrderId} for customer {@event.CustomerId}");
        
        // Send confirmation email, update inventory, etc.
    }
}
```

### Publishing Events

```csharp
public class OrderService
{
    private readonly IEventBus _eventBus;
    
    public OrderService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        // Create order logic...
        
        // Publish event to RabbitMQ
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

### Subscribing to Events

```csharp
public class InventoryService
{
    private readonly IEventBus _eventBus;
    
    public InventoryService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public async Task SubscribeToOrderEvents()
    {
        await _eventBus.SubscribeAsync<OrderCreatedEvent>(async @event =>
        {
            // Update inventory when order is created
            await UpdateInventoryAsync(@event.OrderId);
        });
    }
}
```

## Requirements

- .NET Standard 2.0+
- RabbitMQ Server
- RabbitMQ.Client
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting
- System.Text.Json
- Making.Events
- Making.RabbitMQ

## License

This project is part of the Making framework.