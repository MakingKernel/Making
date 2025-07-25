# Making.Events

Event abstractions and base implementations for the Making framework.

## Overview

Making.Events provides a comprehensive event-driven architecture foundation for the Making framework. It includes abstractions for event handling, publishing, and local event bus implementation for building decoupled, reactive applications.

## Features

- **Event Abstractions**: Core interfaces for events, handlers, and publishers
- **Local Event Bus**: In-memory event bus implementation
- **Event Publishing**: Publish events to registered handlers
- **Event Subscription**: Subscribe to events with typed handlers
- **Dependency Injection**: Built-in DI container integration

## Installation

```bash
dotnet add package Making.Events
```

## Usage

### Define Events

```csharp
public class UserCreatedEvent : IEvent
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Create Event Handlers

```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event)
    {
        // Handle the user created event
        Console.WriteLine($"User {@event.UserName} was created at {@event.CreatedAt}");
        
        // Perform additional logic like sending emails, updating cache, etc.
    }
}
```

### Register Services

```csharp
services.AddMakingEvents();
services.AddScoped<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
```

### Publish Events

```csharp
public class UserService
{
    private readonly IEventPublisher _eventPublisher;
    
    public UserService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    
    public async Task CreateUserAsync(string userName)
    {
        // Create user logic...
        
        // Publish event
        await _eventPublisher.PublishAsync(new UserCreatedEvent
        {
            UserId = userId,
            UserName = userName,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

### Event Subscription

```csharp
public class NotificationService
{
    private readonly IEventSubscriber _eventSubscriber;
    
    public NotificationService(IEventSubscriber eventSubscriber)
    {
        _eventSubscriber = eventSubscriber;
    }
    
    public async Task SubscribeToEvents()
    {
        await _eventSubscriber.SubscribeAsync<UserCreatedEvent>(async @event =>
        {
            // Handle user created notification
            await SendWelcomeEmail(@event.UserId, @event.UserName);
        });
    }
}
```

## Requirements

- .NET Standard 2.0+
- Microsoft.Extensions.DependencyInjection
- Making.Core

## License

This project is part of the Making framework.