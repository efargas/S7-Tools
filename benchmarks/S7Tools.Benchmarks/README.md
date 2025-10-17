# S7Tools Performance Benchmarks

This project contains performance benchmarks for S7Tools using BenchmarkDotNet.

## Purpose

Establish baseline performance metrics for critical operations:
- Profile CRUD operations
- Logging performance (circular buffer and notifications)
- Collection updates
- UI marshaling overhead (future)

## Running Benchmarks

### Run All Benchmarks
```bash
cd benchmarks/S7Tools.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark
```bash
dotnet run -c Release --filter "*ProfileCrud*"
dotnet run -c Release --filter "*Logging*"
```

### Run with Memory Diagnostics
```bash
dotnet run -c Release --memory
```

## Benchmark Results

Results are saved to `BenchmarkDotNet.Artifacts/results/` directory.

### Baseline Metrics (To Be Established)

| Operation | Mean Time | Allocated Memory | Notes |
|-----------|-----------|------------------|-------|
| Profile Create | TBD | TBD | First run establishes baseline |
| Profile Read | TBD | TBD | |
| Profile Update | TBD | TBD | |
| Profile Delete | TBD | TBD | |
| Log Entry Add | TBD | TBD | |
| Log Entry Filter | TBD | TBD | |

## Optimization Opportunities

After establishing baselines, analyze results for:
1. **Memory Allocations** - Identify excessive allocations
2. **Execution Time** - Find slow operations
3. **Concurrency** - Test thread-safe operations
4. **Scalability** - Test with varying data sizes

## Adding New Benchmarks

1. Create a new class in this project
2. Add `[MemoryDiagnoser]` attribute
3. Add `[SimpleJob]` or `[ShortRunJob]` attribute
4. Implement benchmark methods with `[Benchmark]` attribute
5. Use `[GlobalSetup]` and `[GlobalCleanup]` for initialization

Example:
```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MyBenchmarks
{
    [GlobalSetup]
    public void Setup() { }

    [Benchmark]
    public void MyOperation() { }
}
```

## Best Practices

- Run benchmarks in Release mode
- Close other applications to reduce noise
- Run multiple iterations for statistical significance
- Compare results over time to track performance trends
- Document any significant changes in performance

## Integration with CI/CD

Future: Integrate benchmarks into CI/CD pipeline to:
- Track performance regressions
- Alert on significant performance degradation
- Generate performance reports for each release

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices for .NET](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
