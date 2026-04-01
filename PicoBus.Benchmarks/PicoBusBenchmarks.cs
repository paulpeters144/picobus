using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PicoBus;

namespace PicoBus.Benchmarks;

[MemoryDiagnoser]
public class PicoBusBenchmarks
{
    private PicoBus _bus1;
    private PicoBus _bus10;
    private PicoBus _bus100;
    private readonly TestEvent _event = new();

    public class TestEvent { }

    [GlobalSetup]
    public void Setup()
    {
        _bus1 = new PicoBus();
        _bus1.CreateSub<TestEvent>().OnMessage(_ => { });

        _bus10 = new PicoBus();
        for (int i = 0; i < 10; i++) _bus10.CreateSub<TestEvent>().OnMessage(_ => { });

        _bus100 = new PicoBus();
        for (int i = 0; i < 100; i++) _bus100.CreateSub<TestEvent>().OnMessage(_ => { });
    }

    [Benchmark]
    public void Fire_1_Sub() => _bus1.Fire(_event);

    [Benchmark]
    public void Fire_10_Subs() => _bus10.Fire(_event);

    [Benchmark]
    public void Fire_100_Subs() => _bus100.Fire(_event);

    [Benchmark]
    public void Fire_Concurrent_100_Subs()
    {
        Parallel.For(0, 10, _ =>
        {
            _bus100.Fire(_event);
        });
    }

    [Benchmark]
    public void CreateAndDispose()
    {
        var bus = new PicoBus();
        var sub = bus.CreateSub<TestEvent>();
        sub.Dispose();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<PicoBusBenchmarks>();
    }
}
