package picobus_test

import (
	"sync"
	"testing"

	"github.com/paulpeters144/picobus"
	"github.com/stretchr/testify/assert"
)

type TestEvent struct{ Value int }
type AnotherTestEvent struct{ Name string }

func TestCorePublishSubscribe(t *testing.T) {
	bus := picobus.New()
	receivedValue := 0

	subscription := picobus.NewSub(bus, func(data TestEvent) {
		receivedValue = data.Value
	})

	testValue := 42
	event := TestEvent{Value: testValue}

	assert.NoError(t, bus.Fire(event))
	assert.Equal(t, testValue, receivedValue)

	subscription.Dispose()

	assert.Equal(t, 0, bus.SubCount())
}

func TestPicoBusUnit(t *testing.T) {
	t.Run("sub count increments correctly", func(t *testing.T) {
		bus := picobus.New()

		picobus.NewSub(bus, func(data TestEvent) {})
		picobus.NewSub(bus, func(data TestEvent) {})
		picobus.NewSub(bus, func(data TestEvent) {})

		assert.Equal(t, 3, bus.SubCount())
	})

	t.Run("topic count increments correctly", func(t *testing.T) {
		bus := picobus.New()

		picobus.NewSub(bus, func(data TestEvent) {})
		assert.Equal(t, 1, bus.TopicCount())

		picobus.NewSub(bus, func(data AnotherTestEvent) {})
		assert.Equal(t, 2, bus.TopicCount())
	})

	t.Run("dispose decrements sub count", func(t *testing.T) {
		bus := picobus.New()

		sub1 := picobus.NewSub(bus, func(data TestEvent) {})
		picobus.NewSub(bus, func(data TestEvent) {})
		assert.Equal(t, 2, bus.SubCount())

		sub1.Dispose()
		assert.Equal(t, 1, bus.SubCount())
	})

	t.Run("topic count decrements when last sub is removed", func(t *testing.T) {
		bus := picobus.New()

		sub1 := picobus.NewSub(bus, func(data TestEvent) {})
		sub2 := picobus.NewSub(bus, func(data TestEvent) {})
		assert.Equal(t, 1, bus.TopicCount())

		sub1.Dispose()
		assert.Equal(t, 1, bus.TopicCount())

		sub2.Dispose()
		assert.Equal(t, 0, bus.TopicCount())
	})

	t.Run("clear resets sub and topic counts", func(t *testing.T) {
		bus := picobus.New()

		picobus.NewSub(bus, func(data TestEvent) {})
		picobus.NewSub(bus, func(data AnotherTestEvent) {})

		assert.Equal(t, 2, bus.SubCount())
		assert.Equal(t, 2, bus.TopicCount())

		bus.Clear()

		assert.Equal(t, 0, bus.SubCount())
		assert.Equal(t, 0, bus.TopicCount())
	})

	t.Run("fire on empty bus is safe and returns nil", func(t *testing.T) {
		bus := picobus.New()

		err := bus.Fire(TestEvent{Value: 100})
		assert.Nil(t, err)
		assert.Equal(t, 0, bus.SubCount())
	})

	t.Run("fire delivers event to multiple subscribers", func(t *testing.T) {
		bus := picobus.New()
		var wg sync.WaitGroup
		wg.Add(3)

		counter := 0
		consumeFunc := func(data TestEvent) {
			counter += data.Value
			wg.Done()
		}

		picobus.NewSub(bus, consumeFunc)
		picobus.NewSub(bus, consumeFunc)
		picobus.NewSub(bus, consumeFunc)

		bus.Fire(TestEvent{Value: 1})
		wg.Wait()

		assert.Equal(t, 3, counter)
	})

	t.Run("fire to non-matching type returns error from consumer", func(t *testing.T) {
		bus := picobus.New()

		var expectFalse bool
		sub := picobus.NewSub(bus, func(data TestEvent) {
			expectFalse = true
		})

		bus.Fire(AnotherTestEvent{Name: "Wrong Type"})

		assert.Equal(t, expectFalse, false)

		sub.Dispose()
	})

	t.Run("fire only affects targeted topic", func(t *testing.T) {
		bus := picobus.New()
		testValue := 99
		otherValue := 0

		picobus.NewSub(bus, func(data TestEvent) {
			otherValue = data.Value
		})

		picobus.NewSub(bus, func(data AnotherTestEvent) {
			t.Error("Subscriber for AnotherTestEvent incorrectly fired")
		})

		bus.Fire(TestEvent{Value: testValue})

		assert.Equal(t, testValue, otherValue)
	})

	t.Run("dispose of nil subscription is safe", func(t *testing.T) {
		var sub *picobus.Subscription

		assert.NotPanics(t, func() { sub.Dispose() })
	})

	t.Run("dispose of disposed subscription is safe", func(t *testing.T) {
		bus := picobus.New()
		sub := picobus.NewSub(bus, func(data TestEvent) {})

		sub.Dispose()

		assert.NotPanics(t, func() { sub.Dispose() })
		assert.Equal(t, 0, bus.SubCount())
	})
}
