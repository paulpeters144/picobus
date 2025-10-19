package picobus

import (
	"fmt"
	"math"
	"math/rand"
	"reflect"
)

type (
	PicoSub interface {
		GetTopic() string
		Id() int
		Consume(data any) error
	}
	Subscription struct {
		bus *picoBus
		sub PicoSub
	}
	subscriber[T any] struct {
		topic   string
		id      int
		consume func(data T)
	}
)

func NewSub[T any](bus PicoBus, consume func(T)) *Subscription {
	id := rand.Intn(math.MaxInt32) + 1

	sub := &subscriber[T]{
		topic:   reflect.TypeOf((*T)(nil)).Elem().String(),
		id:      id,
		consume: consume,
	}

	b, ok := bus.(*picoBus)
	if !ok {
		panic("NewSub only works with *picoBus")
	}

	b.Subscribe(sub)

	return &Subscription{
		bus: b,
		sub: sub,
	}
}

func (s *subscriber[T]) GetTopic() string {
	return s.topic
}

func (s *subscriber[T]) Id() int {
	return s.id
}

func (s *subscriber[T]) Consume(data any) error {
	casted, ok := data.(T)
	if !ok {
		return fmt.Errorf("failed to cast %v to %v", data, s.topic)
	}
	s.consume(casted)
	return nil
}

func (s *Subscription) Dispose() {
	if s == nil || s.bus == nil || s.sub == nil {
		return
	}
	s.bus.Unsubscribe(s.sub)
	s.bus = nil
	s.sub = nil
}
