---
title: "Old Pattern (Deprecated)"
version: "1.0.0"
created: "2024-01-15"
last-updated: "2025-11-10"
status: "deprecated"
deprecated-date: "2025-11-10"
superseded-by: "docs/.test-fixtures/superseding-doc.md"
removal-date: "2027-11-10"
tags: ["test", "deprecated", "superseded"]
related:
  - docs/.test-fixtures/superseding-doc.md
---

# ⚠️ DEPRECATED: Old Pattern

**This pattern is deprecated as of 2025-11-10.**

**Use instead**: [New Improved Pattern](./superseding-doc.md)

**Removal Date**: 2027-11-10 (2-year retention policy)

**Reason**: The old pattern had performance limitations and lacked support for async operations. The new pattern provides better performance and full async/await support.

## Migration Guide

This section explains how to migrate from the old pattern to the new one.

### Before (Old Pattern v1.0.0)

```csharp
// Old synchronous approach
public class OldPatternService
{
    public void ProcessData(Data input)
    {
        // Blocking operation
        var result = SomeBlockingOperation(input);
        SaveResult(result);
    }
}
```

### After (New Pattern v2.0.0)

```csharp
// New async approach
public class NewPatternService
{
    public async Task ProcessDataAsync(Data input)
    {
        // Non-blocking operation
        var result = await SomeAsyncOperation(input);
        await SaveResultAsync(result);
    }
}
```

### Migration Steps

1. **Update method signature** to return `Task` instead of `void`
2. **Add `async` keyword** to method declaration
3. **Replace blocking calls** with async equivalents:
   - `SomeBlockingOperation()` → `await SomeAsyncOperation()`
   - `SaveResult()` → `await SaveResultAsync()`
4. **Update callers** to use `await` when calling the method
5. **Run tests** to verify behavior unchanged

### Common Pitfalls

- **Don't forget `await`**: Using async methods without `await` can cause deadlocks
- **Don't use `.Result` or `.Wait()`**: These block the thread, defeating async benefits
- **Update entire call chain**: If you make one method async, callers must also be async

## Why Was This Deprecated?

- **Performance**: Blocking operations caused thread pool starvation under load
- **Scalability**: Limited to number of available threads
- **Modern Standards**: Async/await is the recommended pattern in .NET 8+

## Superseded By

See [New Improved Pattern](./superseding-doc.md) for the replacement documentation.

## Related Documentation

- [Superseding Doc](superseding-doc.md)
- [Versioned Doc](versioned-doc.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
