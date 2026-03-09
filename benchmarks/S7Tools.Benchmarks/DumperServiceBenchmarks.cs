using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace S7Tools.Benchmarks;

[MemoryDiagnoser]
public class DumperServiceBenchmarks
{
    private ILogger _logger = null!;
    private byte[] _data = null!;
    private ReadOnlySequence<byte> _buffer;

    [GlobalSetup]
    public void Setup()
    {
        _logger = NullLogger.Instance;
        _data = new byte[1024];
        new Random(42).NextBytes(_data);
        _buffer = new ReadOnlySequence<byte>(_data);
    }

    [Benchmark(Baseline = true)]
    public void CurrentLogging()
    {
        var buffer = _buffer;
        // This mimics line 231-234 in DumperService.cs
        var hexDump = BitConverter.ToString(buffer.Slice(0, Math.Min(buffer.Length, 16)).ToArray());
        _logger.LogTrace("Buffer state: Length={Len}, Head=[{Hex}]", buffer.Length, hexDump);
    }

    [Benchmark]
    public void OptimizedLogging()
    {
        var buffer = _buffer;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var hexDump = BitConverter.ToString(buffer.Slice(0, Math.Min(buffer.Length, 16)).ToArray());
            _logger.LogTrace("Buffer state: Length={Len}, Head=[{Hex}]", buffer.Length, hexDump);
        }
    }
}
