# PicoBus

`PicoBus` is a lightweight, type-safe event bus for Go.  
It allows publishing events (`Fire`) and subscribing handlers for specific data types.  
Subscribers receive events of the type they are interested in, with optional unsubscription and clearing support.

---

## Features

- Type-safe subscriptions using generics.
- Fire events to all subscribers of a type.
- Simple, zero-dependency API.

---

## Installation

```bash
go get github.com/paulpeters144/picobus
```

---

## Basic Concepts

### PicoBus Interface

```go
type PicoBus interface {
    Subscribe(subscriber PicoSub)
    Unsubscribe(subscriber PicoSub)
    Fire(data any) error
    SubCount() int
    TopicCount() int
    Clear()
}
```

### PicoSub Interface

```go
type PicoSub interface {
    GetTopic() string
    Id() int
    Consume(data any) error
}
```

---

## Usage Example

```go
package main

import (
    "fmt"
    "github.com/paulpeters144/picobus"
)

type MyEvent struct { Message string }

func main() {
    bus := picobus.New()

    // Subscribe to MyEvent
    sub := picobus.NewSub[MyEvent](bus, func(e MyEvent) {
        fmt.Println("Received event:", e.Message)
    })

    // Fire an event
    bus.Fire(MyEvent{Message: "Hello, PicoBus!"})

    fmt.Println("Subscribers:", bus.SubCount())  // 1
    fmt.Println("Topics:", bus.TopicCount())     // 1

    // Dispose subscription
    sub.Dispose()
    fmt.Println("Subscribers after dispose:", bus.SubCount())  // 0
}
```

---

## Notes

- Subscriptions are type-safe. You can only subscribe to a specific type.
- The topic is derived from the Go type of the event.
- `Fire` delivers events to all subscribers of that type.
- Use `Subscription.Dispose()` to remove a subscriber.
- `Clear()` removes all subscribers.
- Thread-safe: multiple goroutines can safely `Fire`, `Subscribe`, and `Unsubscribe`.

---

## License

MIT License
Copyright (c) 2025 Paul Q. Peters

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.