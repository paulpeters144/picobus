# PicoBus

A **lightweight, thread-safe, in-memory event bus** for .NET — designed for simplicity, minimal overhead, and zero dependencies.

`PicoBus` allows decoupled communication between components in your application through an intuitive publish/subscribe model.

---

## 🚀 Features

- **Light & dependency-free** — no external libraries used or required.  
- **Thread-safe** using high-speed COW arrays for lock-free reads.  
- **Fluent API** for clean and expressive subscriptions.  
- **Tiny footprint** — less than 100 lines of C# code.  

---

## 📦 Installation

You can include this in your project manually or via a NuGet package (if you publish it):

```bash
dotnet add package PicoBus
```
---

## 💡 Quick Start

Here’s a simple example showing how to use `PicoBus` to publish and subscribe to events:

```csharp
using PicoBus;

// Create the event bus
var bus = new PicoBus();

// Define an event type
public class OrderPlaced
{
    public string OrderId { get; set; } = string.Empty;
}

// Subscribe to the event
var subscription = bus.CreateSub<OrderPlaced>();

subscription.OnMessage(order =>
{
    Console.WriteLine($"Order received: {order.OrderId}");
});

// Publish an event
bus.Fire(new OrderPlaced { OrderId = "ORD-001" });

// Unsubscribe when done
subscription.Dispose();
```

**Output:**
```
Order received: ORD-001
```

---

## ⚙️ API Overview

### `PicoBus`

| Member | Description |
|--------|--------------|
| `int SubCount` | Gets the total number of active subscriptions. |
| `Subscription<TEvent> CreateSub<TEvent>()` | Creates a new subscription for the specified event type. |
| `void Fire<TEvent>(TEvent eventData)` | Publishes an event to all subscribers of that type. |

---

### `Subscription<TEvent>`

| Member | Description |
|--------|--------------|
| `Guid Id` | Unique identifier for this subscription. |
| `bool IsActive` | Indicates whether the subscription is still active. |
| `OnMessage(Action<TEvent> handler)` | Sets the message handler using a fluent interface. |
| `Dispose()` | Deactivates the subscription and removes it from the bus. |

---

## 🧩 Thread Safety

`PicoBus` ensures **safe concurrent publishing and subscribing** across multiple threads.

---

## 🧹 Lifecycle Management

Each subscription is automatically tracked.  
When you call `Dispose()` on a `Subscription<TEvent>`, it:

1. Sets `IsActive` to `false`.  
2. Removes the subscription from the internal dictionary.  

This ensures your application won't leak handlers over time.

---

## 🧪 Example: Multiple Subscribers

```csharp
// Define the event as a simple, immutable record
public record StatusUpdate(string NewStatus);

var bus = new PicoBus();
var statusEvent = new StatusUpdate("System Online");

// Handler A subscribes and processes the event
var serviceSub1 = bus.CreateSub<StatusUpdate>();
serviceSub1.OnMessage(e => 
{
    Console.WriteLine($"[Service 1] Received: {e.NewStatus}");
});

// Handler B subscribes and processes the same event
var serviceSub2 = bus.CreateSub<StatusUpdate>();
serviceSub2.OnMessage(e => 
{
    Console.WriteLine($"[Service 2] Logging change...");
});

// Publish the single event
bus.Fire(statusEvent);
```

**Output:**
```
[Service 1] Received: System Online
[Service 2] Logging change...
```

---

## 📄 License

MIT License

Copyright (c) 2025 Paul Q. Peters

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.