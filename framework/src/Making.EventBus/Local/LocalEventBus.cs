using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Making.EventBus.Abstractions.EventBus;
using Microsoft.Extensions.DependencyInjection;

namespace Making.Events.Local;

/// <summary>
/// Local in-memory implementation of the event bus.
/// </summary>
public class LocalEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, List<Type>> _handlers = new();

    public LocalEventBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);

        if (_handlers.TryGetValue(eventType, out var handlerTypes))
        {
            var tasks = new List<Task>();

            foreach (var handlerType in handlerTypes)
            {
                var handler = _serviceProvider.GetService(handlerType);
                if (handler != null)
                {
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { @event })!;
                        tasks.Add(task);
                    }
                }
            }

            await Task.WhenAll(tasks);
        }
    }

    public async Task Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        _handlers.AddOrUpdate(
            eventType,
            new List<Type> { handlerType },
            (key, existingList) =>
            {
                if (!existingList.Contains(handlerType))
                {
                    existingList.Add(handlerType);
                }
                return existingList;
            });

        await Task.CompletedTask;
    }

    public async Task Unsubscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        if (_handlers.TryGetValue(eventType, out var handlerTypes))
        {
            handlerTypes.Remove(handlerType);
            if (handlerTypes.Count == 0)
            {
                _handlers.TryRemove(eventType, out _);
            }
        }

        await Task.CompletedTask;
    }
}