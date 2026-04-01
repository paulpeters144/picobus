namespace PicoBus;

/// <summary>
/// Represents an active subscription to a specific event type within the PicoBus.
/// This class provides methods for setting the message handler and managing the subscription's lifecycle.
/// </summary>
/// <typeparam name="TEvent">The event type this subscription is listening for.</typeparam>
public sealed class Subscription<TEvent>
{
    private readonly Action<Guid> _onDispose;
    private Action<TEvent>? _handler;
    private volatile bool _isActive = true;

    /// <summary>
    /// Gets the unique identifier for this specific subscription instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets a value indicating whether this subscription is currently active and capable of receiving events.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscription{TEvent}"/> class.
    /// </summary>
    /// <param name="onDispose">An action supplied by the bus to remove the subscription when <see cref="Dispose"/> is called.</param>
    public Subscription(Action<Guid> onDispose)
    {
        _onDispose = onDispose;
    }

    /// <summary>
    /// Sets the required message handler for this subscription.
    /// This method can only be called once per subscription.
    /// </summary>
    /// <param name="handler">The action to execute when an event of type <typeparamref name="TEvent"/> is published.</param>
    /// <returns>The current <see cref="Subscription{TEvent}"/> instance (fluent API).</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided handler is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if this method is called more than once on the same subscription.</exception>
    public Subscription<TEvent> OnMessage(Action<TEvent> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        if (_handler != null)
        {
            var msg = "The Subscription's 'OnMessage()' method can only be set once.";
            throw new InvalidOperationException(msg);
        }
        _handler = handler;
        return this;
    }

    /// <summary>
    /// Executes the configured message handler with the provided event data, if the subscription is active and a handler is set.
    /// </summary>
    /// <param name="eventData">The event data to process.</param>
    internal void HandleEvent(TEvent eventData)
    {
        if (!_isActive) return;
        if (_handler == null) return;

       _handler(eventData);
    }

    /// <summary>
    /// Disposes of the subscription, setting <see cref="IsActive"/> to false and notifying the bus to remove this subscription.
    /// Once disposed, this subscription will no longer receive events.
    /// </summary>
    public void Dispose()
    {
        if (!_isActive) return;
     
        _isActive = false;
        _onDispose.Invoke(Id);
    }
}