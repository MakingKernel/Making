using System.Threading.Tasks;

namespace Making.Events;

/// <summary>
/// Defines the interface for an event handler that processes events of a specific type.
/// </summary>
/// <typeparam name="TEvent">The type of the event to handle.</typeparam>
public interface IEventHandler<TEvent> where TEvent : class
{
    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(TEvent @event);
}