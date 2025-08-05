namespace Making.EventBus.Abstractions.EventBus;


public interface IEventNameProvider
{
    string GetName(Type eventType);
}
