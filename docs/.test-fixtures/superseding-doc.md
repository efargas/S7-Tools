---
title: "New Improved Pattern"
version: "2.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["test", "supersedes", "async", "performance"]
supersedes: "docs/.test-fixtures/deprecated-doc.md"
related:
  - docs/.test-fixtures/deprecated-doc.md
  - docs/.test-fixtures/versioned-doc.md
---

# New Improved Pattern

This document supersedes [Old Pattern (Deprecated)](./deprecated-doc.md) with better practices.

## What This Supersedes

**Supersedes**: [Old Pattern (Deprecated)](./deprecated-doc.md) (v1.0.0)

**Reason**: The old pattern used synchronous blocking operations which caused performance issues under load. This new pattern provides:
- ✅ Full async/await support
- ✅ 3x better throughput under concurrent load
- ✅ Non-blocking I/O operations
- ✅ Cancellation token support

## Pattern Overview

This pattern demonstrates modern .NET async/await best practices for I/O-bound operations.

### Core Implementation

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class NewPatternService : IPatternService
{
    private readonly IDataRepository _repository;
    private readonly ILogger<NewPatternService> _logger;

    public NewPatternService(
        IDataRepository repository,
        ILogger<NewPatternService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> ProcessDataAsync(
        Data input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Non-blocking operation with cancellation support
            var result = await _repository
                .FetchDataAsync(input, cancellationToken)
                .ConfigureAwait(false);

            await _repository
                .SaveResultAsync(result, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Data processed successfully: {Id}",
                result.Id);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data");
            throw;
        }
    }
}
```

## Key Features

### 1. Async/Await Throughout

All I/O operations use `async`/`await` to avoid blocking threads:

```csharp
// ✅ Good - Non-blocking
var data = await _repository.GetDataAsync(id);

// ❌ Bad - Blocks thread
var data = _repository.GetData(id);
```

### 2. Cancellation Token Support

All async methods accept `CancellationToken` for graceful cancellation:

```csharp
public async Task<Result> ProcessAsync(
    Data input,
    CancellationToken cancellationToken = default)
{
    // Pass token to all async calls
    await SomeOperationAsync(input, cancellationToken);
}
```

### 3. ConfigureAwait(false)

Library code uses `ConfigureAwait(false)` to avoid context switching:

```csharp
// ✅ Good - Avoids unnecessary context switches
var result = await operation.ConfigureAwait(false);

// ⚠️ OK for UI code that needs context
var result = await operation; // or ConfigureAwait(true)
```

### 4. Structured Logging

Uses structured logging for better observability:

```csharp
_logger.LogInformation(
    "Processing {Count} items for user {UserId}",
    items.Count,
    userId);
```

## Performance Comparison

| Metric | Old Pattern (v1.0) | New Pattern (v2.0) | Improvement |
|--------|-------------------|-------------------|-------------|
| **Throughput** | 100 req/sec | 300 req/sec | 3x |
| **Thread Usage** | 1 per request | Shared pool | 10x efficiency |
| **Latency (p95)** | 500ms | 150ms | 70% reduction |
| **Memory** | 2MB per request | 0.5MB per request | 75% reduction |

## Migration Guide

See the [Old Pattern (Deprecated)](./deprecated-doc.md) documentation for detailed migration steps.

**Quick Migration Checklist**:
- [ ] Change method signature to return `Task`
- [ ] Add `async` keyword
- [ ] Replace blocking calls with async equivalents
- [ ] Add `CancellationToken` parameter
- [ ] Use `ConfigureAwait(false)` in library code
- [ ] Update unit tests to be async
- [ ] Run performance benchmarks

## Testing

Example unit test for the new pattern:

```csharp
[Fact]
public async Task ProcessDataAsync_ValidInput_ReturnsResult()
{
    // Arrange
    var input = new Data { Id = 1, Value = "test" };
    var expectedResult = new Result { Id = 1, Success = true };

    _mockRepository
        .Setup(r => r.FetchDataAsync(input, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedResult);

    // Act
    var result = await _service.ProcessDataAsync(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedResult.Id, result.Id);
    Assert.True(result.Success);
}

[Fact]
public async Task ProcessDataAsync_CancellationRequested_ThrowsOperationCanceledException()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.Cancel();
    var input = new Data { Id = 1 };

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => _service.ProcessDataAsync(input, cts.Token));
}
```

## Best Practices

1. **Always use async for I/O**: File, network, database operations
2. **Never block on async code**: Don't use `.Result` or `.Wait()`
3. **Pass CancellationToken**: Enable graceful shutdown
4. **Use ConfigureAwait**: In library code, use `ConfigureAwait(false)`
5. **Handle exceptions**: Wrap in try/catch, log appropriately

## Anti-Patterns to Avoid

❌ **Blocking on async code**:
```csharp
var result = SomeAsyncMethod().Result; // Deadlock risk!
```

❌ **Async void (except event handlers)**:
```csharp
public async void ProcessData() // Can't await or catch exceptions
```

❌ **Not propagating CancellationToken**:
```csharp
public async Task ProcessAsync(CancellationToken token)
{
    await SomeOperation(); // Should pass token!
}
```

## Related Documentation

- [Deprecated Doc](deprecated-doc.md)
- [Versioned Doc](versioned-doc.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
