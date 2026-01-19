using Polly;
using Polly.Retry;
using GameStore.Common.Events;

namespace GameStore.Common.Messaging;

/// <summary>
/// EventBus com retry policy e circuit breaker
/// </summary>
public class ResilientEventBus : IEventBus
{
    private readonly IEventBus _innerEventBus;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ResilientEventBus(IEventBus innerEventBus)
    {
        _innerEventBus = innerEventBus;
        
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[EventBus] Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        await _retryPolicy.ExecuteAsync(async () => 
        {
            await _innerEventBus.PublishAsync(@event);
        });
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        _innerEventBus.Subscribe(handler);
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        _innerEventBus.Unsubscribe(handler);
    }

    public int GetHandlerCount<TEvent>() where TEvent : IDomainEvent
    {
        return _innerEventBus.GetHandlerCount<TEvent>();
    }
}
