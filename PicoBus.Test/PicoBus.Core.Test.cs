using PicoBus.Core;

namespace PicoBus.Test;

public record UserCreated(string Name);
public record OrderPlaced(int OrderId);

public class PicoBusTests
{
    [Fact]
    public void Fire_PublishesToSingleSubscriber_Successful()
    {
        var bus = new PicoBus.Core.PicoBus();
        var eventData = new UserCreated("Alice");
        UserCreated? receivedEvent = null;

        bus.CreateSub<UserCreated>().OnMessage(e => receivedEvent = e);

        bus.Fire(eventData);

        Assert.Equal(eventData, receivedEvent);
    }

    [Fact]
    public void Fire_PublishesToMultipleSubscribers_Successful()
    {
        var bus = new PicoBus.Core.PicoBus();
        var eventData = new OrderPlaced(101);
        int callCount = 0;

        bus.CreateSub<OrderPlaced>().OnMessage(_ => callCount++);
        bus.CreateSub<OrderPlaced>().OnMessage(_ => callCount++);
        bus.CreateSub<OrderPlaced>().OnMessage(_ => callCount++);

        bus.Fire(eventData);

        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Fire_NoSubscriberForEventType_DoesNothing()
    {
        var bus = new PicoBus.Core.PicoBus();
        int callCount = 0;

        bus.CreateSub<OrderPlaced>().OnMessage(_ => callCount++);

        bus.Fire(new UserCreated("Bob"));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Subscription_Dispose_RemovesSubscriberFromBus()
    {
        var bus = new PicoBus.Core.PicoBus();
        var eventData = new UserCreated("Charlie");
        int callCount = 0;

        var subscription = bus.CreateSub<UserCreated>().OnMessage(_ => callCount++);

        bus.Fire(eventData);
        Assert.Equal(1, callCount);

        subscription.Dispose();

        bus.Fire(eventData);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Subscription_IsActive_BecomesFalseAfterDispose()
    {
        var bus = new PicoBus.Core.PicoBus();
        var subscription = bus.CreateSub<UserCreated>().OnMessage(_ => { });

        Assert.True(subscription.IsActive);

        subscription.Dispose();

        Assert.False(subscription.IsActive);
    }

    [Fact]
    public void Subscription_DisposeTwice_RemovesSubscriptionOnlyOnce()
    {
        Guid removedId = Guid.Empty;
        bool removed = false;
        var subscription = new Subscription<UserCreated>(id =>
        {
            if (!removed)
            {
                removedId = id;
                removed = true;
            }
        });

        subscription.Dispose();
        subscription.Dispose();

        Assert.True(removed);
        Assert.False(subscription.IsActive);
    }

    [Fact]
    public void Fire_HandlerIsNotSet_DoesNotThrowException()
    {
        var bus = new PicoBus.Core.PicoBus();

        bus.CreateSub<UserCreated>();

        var exception = Record.Exception(() => bus.Fire(new UserCreated("Dave")));

        Assert.Null(exception);
    }

    [Fact]
    public void Fire_SubscriptionBeforeOnMessage_IsHandledWhenHandlerIsSet()
    {
        var bus = new PicoBus.Core.PicoBus();
        var eventData = new UserCreated("Eve");
        UserCreated? receivedEvent = null;

        var subscription = bus.CreateSub<UserCreated>();

        bus.Fire(eventData);
        Assert.Null(receivedEvent);

        subscription.OnMessage(e => receivedEvent = e);

        bus.Fire(eventData);

        Assert.Equal(eventData, receivedEvent);
    }

    [Fact]
    public void CreateSub_ReturnsUniqueId()
    {
        var bus = new PicoBus.Core.PicoBus();

        var sub1 = bus.CreateSub<UserCreated>();
        var sub2 = bus.CreateSub<UserCreated>();

        Assert.NotEqual(sub1.Id, sub2.Id);
    }

    [Fact]
    public async Task Fire_HandlesConcurrentSubscriptions_Safely()
    {
        var bus = new PicoBus.Core.PicoBus();
        int finalCount = 0;
        int numTasks = 10;

        for (int i = 0; i < 100; i++)
        {
            bus.CreateSub<OrderPlaced>().OnMessage(_ => Interlocked.Increment(ref finalCount));
        }

        var tasks = new Task[numTasks];
        for (int i = 0; i < numTasks; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var sub = bus.CreateSub<OrderPlaced>().OnMessage(_ => { });
                    bus.Fire(new OrderPlaced(j));
                    sub.Dispose();
                }
            });
        }

        await Task.WhenAll(tasks);

        var exception = Record.Exception(() => bus.Fire(new OrderPlaced(999)));

        Assert.Null(exception);
        Assert.True(finalCount >= 1000);
    }

    [Fact]
    public void SubCount_InitialCountIsZero()
    {
        var bus = new PicoBus.Core.PicoBus();

        Assert.Equal(0, bus.SubCount);
    }

    [Fact]
    public void SubCount_IncrementsAfterSubscription()
    {
        var bus = new PicoBus.Core.PicoBus();

        bus.CreateSub<UserCreated>().OnMessage(_ => { });

        Assert.Equal(1, bus.SubCount);

        bus.CreateSub<OrderPlaced>().OnMessage(_ => { });

        Assert.Equal(2, bus.SubCount);
    }

    [Fact]
    public void SubCount_DecrementsAfterDispose()
    {
        var bus = new PicoBus.Core.PicoBus();

        var subA = bus.CreateSub<UserCreated>().OnMessage(_ => { });
        var subB = bus.CreateSub<OrderPlaced>().OnMessage(_ => { });
        var subC = bus.CreateSub<UserCreated>().OnMessage(_ => { });

        Assert.Equal(3, bus.SubCount);

        subB.Dispose();

        Assert.Equal(2, bus.SubCount);

        subA.Dispose();
        subC.Dispose();

        Assert.Equal(0, bus.SubCount);
    }

    [Fact]
    public void Clear_RemovesAllSubscriptionsAndResetsCount()
    {
        var bus = new PicoBus.Core.PicoBus();

        var sub1 = bus.CreateSub<OrderPlaced>();
        sub1.OnMessage(e => { /* handler 1 */ });

        var sub2 = bus.CreateSub<OrderPlaced>();
        sub2.OnMessage(e => { /* handler 2 */ });

        var sub3 = bus.CreateSub<OrderPlaced>();
        sub3.OnMessage(e => { /* handler 3 */ });

        Assert.Equal(3, bus.SubCount);

        bus.Clear();

        Assert.Equal(0, bus.SubCount);
    }

    [Fact]
    public void OnMessage_ThrowsArgumentNullException_WhenHandlerIsNull()
    {
        var bus = new PicoBus.Core.PicoBus();
        var subscription = bus.CreateSub<UserCreated>();

        Action<UserCreated>? nullHandler = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            subscription.OnMessage(nullHandler!);
        });
    }

    [Fact]
    public void OnMessage_ThrowsInvalidOperationException_WhenCalledTwice()
    {
        var bus = new PicoBus.Core.PicoBus();
        var subscription = bus.CreateSub<UserCreated>();

        subscription.OnMessage(_ => { });

        Assert.Throws<InvalidOperationException>(() =>
        {
            subscription.OnMessage(_ => { /* Second attempt */ });
        });
    }
}