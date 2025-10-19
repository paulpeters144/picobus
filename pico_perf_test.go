package picobus_test

import (
	"log"
	"testing"

	"github.com/paulpeters144/picobus"
)

func BenchmarkFireToMultipleSubscribers(b *testing.B) {
	const subCount = 1000
	bus := picobus.New()
	for range subCount {
		picobus.NewSub(bus, func(data TestEvent) {})
	}
	if bus.SubCount() != subCount {
		log.Fatal("subcount was not correct. expected:", subCount)
	}

	event := TestEvent{Value: 1}

	for b.Loop() {
		bus.Fire(event)
	}
}

func BenchmarkNewSub(b *testing.B) {
	bus := picobus.New()

	for b.Loop() {
		picobus.NewSub(bus, func(data TestEvent) {})
	}
}
