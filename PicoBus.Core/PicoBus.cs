using System.Collections.Concurrent;

namespace PicoBusCore;

/// <summary>
/// A very lightweight, thread-safe, in-memory event bus optimized for high-performance dispatch.
/// </summary>
public sealed class PicoBus
{
    /// <summary>
    /// Gets the total number of active subscriptions across all event types.
    /// </summary>
    public int SubCount
    {
        get
        {
            return _subscriptions.Values.Sum(subList => subList.Length);
        }
    }

    // Uses a Copy-On-Write (COW) array for each event type to ensure lock-free, high-speed iteration during Fire.
    private readonly ConcurrentDictionary<Type, object[]> _subscriptions = new();
    private readonly object _writeLock = new();

    /// <summary>
    /// Creates a new subscription object for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to. Must be a class.</typeparam>
    /// <returns>A <see cref="Subscription{TEvent}"/> object used to configure the message handler and manage the subscription lifecycle.</returns>
    public Subscription<TEvent> CreateSub<TEvent>() where TEvent : class
    {
        var eventType = typeof(TEvent);
        var subscription = new Subscription<TEvent>(onDispose: RemoveSubscription<TEvent>);

        lock (_writeLock)
        {
            _subscriptions.AddOrUpdate(eventType, _ => [subscription], (_, current) =>
                {
                    var next = new object[current.Length + 1];
                    Array.Copy(current, next, current.Length);
                    next[current.Length] = subscription;
                    return next;
                });
        }

        return subscription;
    }

    /// <summary>
    /// Publishes an event to all active subscribers of that event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being published. Must be a class.</typeparam>
    /// <param name="eventData">The event object containing the data to be published.</param>
    public void Fire<TEvent>(TEvent eventData) where TEvent : class
    {
        if (_subscriptions.TryGetValue(typeof(TEvent), out var subList))
        {
            // Lock-free iteration on the current array snapshot
            for (int i = 0; i < subList.Length; i++)
            {
                // Direct cast is safe as we control the array contents in CreateSub
                ((Subscription<TEvent>)subList[i]).HandleEvent(eventData);
            }
        }
    }

    /// <summary>
    /// Clears all active subscriptions from the bus, resetting it to an empty state.
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
    }

    private void RemoveSubscription<TEvent>(Guid subscriptionId) where TEvent : class
    {
        var eventType = typeof(TEvent);

        lock (_writeLock)
        {
            if (_subscriptions.TryGetValue(eventType, out var current))
            {
                var index = -1;
                for (int i = 0; i < current.Length; i++)
                {
                    if (((Subscription<TEvent>)current[i]).Id == subscriptionId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    if (current.Length == 1)
                    {
                        _subscriptions.TryRemove(eventType, out _);
                    }
                    else
                    {
                        var next = new object[current.Length - 1];
                        if (index > 0) Array.Copy(current, 0, next, 0, index);
                        if (index < current.Length - 1) Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                        _subscriptions.TryUpdate(eventType, next, current);
                    }
                }
            }
        }
    }
}
