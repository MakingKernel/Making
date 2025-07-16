using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using Mark.RabbitMQ.Connections;

namespace Mark.Events.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the event bus.
/// </summary>
public class RabbitMqEventBus : IEventBus, IHostedService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly string _exchangeName = "mark_events";
    private readonly string _queueName = "mark_events_queue";
    private IChannel? _consumerChannel;

    public RabbitMqEventBus(
        IRabbitMqConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqEventBus> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        if (!IsConnected)
        {
            _logger.LogError("RabbitMQ connection is not available");
            return;
        }

        using var channel = await _connection.CreateModelAsync();

        var eventType = typeof(TEvent);
        var routingKey = eventType.FullName!;

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = eventType.AssemblyQualifiedName
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}",
            eventType.Name, routingKey);
    }

    public async Task Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        if (!IsConnected)
        {
            _logger.LogError("RabbitMQ connection is not available");
            return;
        }

        var eventType = typeof(TEvent);
        var routingKey = eventType.FullName!;

        await _consumerChannel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: routingKey);

        _logger.LogInformation("Subscribed handler {HandlerType} to event {EventType} with routing key {RoutingKey}",
            typeof(THandler).Name, eventType.Name, routingKey);
    }

    public async Task Unsubscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        if (!IsConnected)
        {
            _logger.LogError("RabbitMQ connection is not available");
            return;
        }

        var eventType = typeof(TEvent);
        var routingKey = eventType.FullName!;

        await _consumerChannel.QueueUnbindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: routingKey);

        _logger.LogInformation(
            "Unsubscribed handler {HandlerType} from event {EventType} with routing key {RoutingKey}",
            typeof(THandler).Name, eventType.Name, routingKey);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            _logger.LogError("RabbitMQ connection is not available");
            return;
        }

        _consumerChannel = await _connection.CreateModelAsync();

        await _consumerChannel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: "topic",
            durable: true,
            autoDelete: false, cancellationToken: cancellationToken);

        await _consumerChannel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            await ProcessEvent(ea);
            return;
        };

        await _consumerChannel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ event bus started and listening on queue {QueueName}", _queueName);

        return;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumerChannel?.Dispose();
        _logger.LogInformation("RabbitMQ event bus stopped");
        return Task.CompletedTask;
    }

    private async Task ProcessEvent(BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var eventType = Type.GetType(eventArgs.BasicProperties.Type);
            if (eventType == null)
            {
                _logger.LogError("Could not resolve event type: {TypeName}",
                    eventArgs.BasicProperties.Type);
                return;
            }

            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var @event = JsonSerializer.Deserialize(message, eventType);
            if (@event == null)
            {
                _logger.LogError("Could not deserialize event: {Message}", message);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var handlerTypes = scope.ServiceProvider.GetServices(typeof(IEventHandler<>).MakeGenericType(eventType));

            foreach (var handler in handlerTypes)
            {
                if (handler != null)
                {
                    var handleMethod = handler.GetType().GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new[] { @event })!;
                        await task;
                    }
                }
            }

            await _consumerChannel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event");
            await _consumerChannel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private bool IsConnected => _connection.IsConnected;
}