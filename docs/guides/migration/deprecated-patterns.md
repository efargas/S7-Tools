---
title: "Deprecated Patterns and Replacements"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "migration", "deprecated", "patterns"]
related:
  - docs/guides/migration/_index.md
  - docs/guides/migration/breaking-changes.md
  - docs/patterns/_index.md
---

# Deprecated Patterns and Replacements

This document lists all deprecated patterns in S7Tools with their recommended replacements and migration guides.

## Current Status

**Active Deprecations**: 0
**Last Updated**: 2025-11-10

Currently, all S7Tools patterns are **current and maintained**. This document will be updated when patterns are deprecated.

## Deprecation Lifecycle

```
Current → Deprecated (Warning) → Archived → Removed
          ↓                     ↓           ↓
          T+0                  T+12mo      T+24mo
```

**Stages**:
1. **Current**: Pattern is actively maintained and recommended
2. **Deprecated**: Pattern is discouraged, replacement available, 2-year warning period
3. **Archived**: Pattern moved to `docs/archive/`, read-only reference
4. **Removed**: Documentation deleted after 2-year retention

## How to Check for Deprecations

### In Code

Look for `[Obsolete]` attributes:

```csharp
[Obsolete("Use NewMethod() instead. Will be removed in v3.0.0")]
public void OldMethod() { }
```

### In Documentation

Check frontmatter for `status: deprecated`:

```yaml
---
status: "deprecated"
deprecated-date: "2025-11-10"
superseded-by: "docs/patterns/new-pattern.md"
removal-date: "2027-11-10"
---
```

### With Validator

```bash
# Find all deprecated documentation
python scripts/validate-frontmatter.py docs/ | grep "deprecated"

# Or search directly
grep -r "status: \"deprecated\"" docs/ --include="*.md"
```

## Deprecated Patterns Registry

### No Active Deprecations

✅ All patterns are current as of 2025-11-10.

---

## Future Deprecations (Planned)

None currently planned.

---

## Completed Deprecations (Historical)

### Example: Old Synchronous Pattern → Async Pattern (Future)

**Status**: Not yet deprecated (example template)

**Original Pattern**: Synchronous blocking operations
**Replacement**: Async/await pattern
**Deprecated**: N/A
**Removal**: N/A

**Why Deprecated**: Blocking operations cause thread pool starvation

**Migration Timeline**:
- **v1.5.0**: New async pattern introduced, old pattern still works
- **v2.0.0**: Old pattern marked `[Obsolete]` with warning
- **v2.5.0**: Old pattern moved to archive
- **v3.0.0**: Old pattern removed completely

**Migration Effort**: Medium (2-4 hours per service)

**Impact**:
- All service classes using old pattern
- Test fixtures
- Configuration files

**Migration Guide**:

#### Before (Old Pattern)

```csharp
public class OldPatternService
{
    public Result ProcessData(Data input)
    {
        // Blocking operation
        var result = SomeBlockingCall(input);
        SaveToDatabase(result);
        return result;
    }
}
```

#### After (New Pattern)

```csharp
public class NewPatternService
{
    public async Task<Result> ProcessDataAsync(
        Data input,
        CancellationToken cancellationToken = default)
    {
        // Non-blocking operation
        var result = await SomeAsyncCall(input, cancellationToken);
        await SaveToDatabaseAsync(result, cancellationToken);
        return result;
    }
}
```

#### Migration Steps

1. **Add async to method signature**
   ```csharp
   // Change return type
   Result → Task<Result>
   void → Task
   ```

2. **Add CancellationToken parameter**
   ```csharp
   public async Task<Result> ProcessDataAsync(
       Data input,
       CancellationToken cancellationToken = default)
   ```

3. **Replace blocking calls with async equivalents**
   ```csharp
   SomeBlockingCall() → await SomeAsyncCall()
   SaveToDatabase() → await SaveToDatabaseAsync()
   ```

4. **Update callers**
   ```csharp
   // Before
   var result = service.ProcessData(data);

   // After
   var result = await service.ProcessDataAsync(data);
   ```

5. **Update tests**
   ```csharp
   [Fact]
   public async Task ProcessDataAsync_ValidInput_ReturnsResult()
   {
       var result = await _service.ProcessDataAsync(data);
       Assert.NotNull(result);
   }
   ```

#### Validation

After migration, verify:

```bash
# Run tests
dotnet test

# Check for remaining blocking calls
grep -r "\.Result\|\.Wait()" src/ --include="*.cs"

# Check for missing async/await
grep -r "Task<" src/ --include="*.cs" | grep -v "async"
```

#### Common Issues

**Issue**: Deadlock when using `.Result` or `.Wait()`

**Solution**: Always use `await`, never block on async code

```csharp
// ❌ Bad - Can deadlock
var result = SomeAsyncMethod().Result;

// ✅ Good - Non-blocking
var result = await SomeAsyncMethod();
```

**Issue**: Missing CancellationToken propagation

**Solution**: Pass token to all async calls

```csharp
// ❌ Bad - Token not propagated
await SomeAsyncCall();

// ✅ Good - Token propagated
await SomeAsyncCall(cancellationToken);
```

**Issue**: Forgetting ConfigureAwait in library code

**Solution**: Use `ConfigureAwait(false)` in library code

```csharp
// Library code
var result = await operation.ConfigureAwait(false);

// UI code (Avalonia ViewModels)
var result = await operation; // or ConfigureAwait(true)
```

---

## Template for New Deprecations

When deprecating a pattern, copy this template:

```markdown
### Pattern Name → Replacement Pattern (YYYY-MM-DD)

**Status**: Deprecated

**Original Pattern**: Brief description
**Replacement**: Link to new pattern
**Deprecated**: YYYY-MM-DD
**Removal**: YYYY-MM-DD (deprecated + 2 years)

**Why Deprecated**: Reason for deprecation

**Migration Timeline**:
- **vX.Y.Z**: Timeline item
- **vX.Y.Z**: Timeline item

**Migration Effort**: Low | Medium | High (estimated time)

**Impact**:
- Component 1
- Component 2

**Migration Guide**: [Link to detailed guide or inline instructions]
```

## Deprecation Best Practices

When deprecating a pattern:

1. ✅ **Provide Clear Replacement**: Always have a better alternative ready
2. ✅ **Give Advance Notice**: Announce in release notes before deprecating
3. ✅ **Include Migration Guide**: Provide before/after examples
4. ✅ **Set Timeline**: 2-year warning period minimum
5. ✅ **Update Documentation**: Mark old pattern as deprecated
6. ✅ **Add Code Warnings**: Use `[Obsolete]` attribute if applicable
7. ✅ **Maintain Both**: Keep old pattern working during transition
8. ✅ **Track Usage**: Search codebase for pattern usage
9. ✅ **Test Migration**: Verify migration guide works
10. ✅ **Communicate**: Announce in changelog and team channels

## Resources

- [Migration Guides Index](./migration/_index.md)
- [Breaking Changes History](./breaking-changes.md)
- [Versioning Guide](../versioning-guide.md)
- [Pattern Index](../../patterns/_index.md)

## Version History

### v1.0.0 (2025-11-10)

**MAJOR**: Initial deprecated patterns registry

**Features**:
- Deprecation lifecycle documentation
- Example migration template
- Validation instructions

**Migration Notes**: N/A (initial release)

## Related Documentation

- [Attribute_Based_Display](../../ATTRIBUTE_BASED_DISPLAY.md)
- [Index](../../INDEX.md)
- [Project_Architecture_Blueprint](../../Project_Architecture_Blueprint.md)
- [Project_Folders_Structure_Blueprint](../../Project_Folders_Structure_Blueprint.md)
- [_Index](_index.md)
- [Breaking Changes](breaking-changes.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
