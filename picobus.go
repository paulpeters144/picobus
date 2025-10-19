package picobus

import (
	"reflect"
	"sync"
)

type PicoBus interface {
	Subscribe(subscriber PicoSub)
	Unsubscribe(subscriber PicoSub)
	Fire(data any) error
	SubCount() int
	TopicCount() int
	Clear()
}

type picoBus struct {
	mu          sync.RWMutex
	subscribers map[string][]PicoSub
}

func New() PicoBus {
	return &picoBus{
		subscribers: make(map[string][]PicoSub),
	}
}

func (eb *picoBus) Clear() {
	eb.mu.Lock()
	defer eb.mu.Unlock()
	eb.subscribers = make(map[string][]PicoSub)
}

func (eb *picoBus) Subscribe(sub PicoSub) {
	topic := sub.GetTopic()

	eb.mu.Lock()
	defer eb.mu.Unlock()

	if _, ok := eb.subscribers[topic]; !ok {
		eb.subscribers[topic] = make([]PicoSub, 0)
	}

	eb.subscribers[topic] = append(eb.subscribers[topic], sub)
}

func (eb *picoBus) Unsubscribe(sub PicoSub) {
	topic := sub.GetTopic()

	eb.mu.Lock()
	defer eb.mu.Unlock()

	if subs, ok := eb.subscribers[topic]; ok {
		for i := range subs {
			if subs[i].Id() == sub.Id() {
				eb.subscribers[topic] = append(subs[:i], subs[i+1:]...)
				break
			}
		}
		if len(eb.subscribers[topic]) == 0 {
			delete(eb.subscribers, topic)
		}
	}
}

func (eb *picoBus) Fire(data interface{}) error {
	topic := reflect.TypeOf(data).String()

	eb.mu.RLock()
	defer eb.mu.RUnlock()

	if subs, ok := eb.subscribers[topic]; ok {
		for _, sub := range subs {
			if err := sub.Consume(data); err != nil {
				return err
			}
		}
	}
	return nil
}

func (eb *picoBus) TopicCount() int {
	eb.mu.RLock()
	defer eb.mu.RUnlock()
	return len(eb.subscribers)
}

func (eb *picoBus) SubCount() int {
	eb.mu.RLock()
	defer eb.mu.RUnlock()

	count := 0
	for _, subs := range eb.subscribers {
		count += len(subs)
	}
	return count
}
