---
title: "Versioned Documentation Example"
version: "2.1.0"
created: "2025-01-15"
last-updated: "2025-11-10"
status: "current"
tags: ["test", "versioning", "example"]
related:
  - docs/.test-fixtures/complete-doc.md
---

# Versioned Documentation Example

This is a test fixture demonstrating proper versioning and version history tracking.

## Current Content

This section represents the current state of the documentation at version 2.1.0.

### Feature Description

The latest features are documented here with examples and best practices.

## Version History

### v2.1.0 (2025-11-10)

**MINOR**: Added new section on advanced usage patterns

**Changes**:
- Added "Advanced Patterns" section with 3 new examples
- Enhanced troubleshooting guide with common edge cases
- Fixed broken link to related documentation
- Updated cross-references to reflect new architecture

**Migration Notes**: No breaking changes. Existing implementations remain valid.

### v2.0.0 (2025-09-15)

**MAJOR**: Complete rewrite of core pattern approach

**Breaking Changes**:
- Removed deprecated `OldMethod()` in favor of `NewMethod()`
- Changed parameter order in `ConfigureService()` method
- Updated namespace from `S7Tools.Old` to `S7Tools.New`

**Migration Guide**:

**Before (v1.x)**:
```csharp
// Old approach
var service = new OldService();
service.OldMethod(param1, param2);
```

**After (v2.x)**:
```csharp
// New approach
var service = new NewService();
service.NewMethod(param2, param1); // Note: parameter order changed
```

**Affected Code**:
- All implementations of `IServiceInterface`
- Test fixtures using `OldMethod()`
- Configuration in `appsettings.json`

**Deprecation Timeline**:
- v1.5.0 (2025-08-01): `OldMethod()` marked `[Obsolete]` with warning
- v2.0.0 (2025-09-15): `OldMethod()` removed completely

### v1.5.0 (2025-08-01)

**MINOR**: Deprecation warnings added

**Changes**:
- Marked `OldMethod()` as `[Obsolete("Use NewMethod() instead. Will be removed in v2.0.0")]`
- Added migration examples to documentation
- Created side-by-side comparison guide

**Migration Notes**: Start migrating to `NewMethod()`. `OldMethod()` still functional but will be removed in v2.0.0.

### v1.4.1 (2025-07-20)

**PATCH**: Bug fix release

**Changes**:
- Fixed null reference exception in edge case
- Corrected typo in example code
- Updated outdated screenshot

**Migration Notes**: Drop-in replacement, no code changes required.

### v1.4.0 (2025-06-10)

**MINOR**: Performance improvements

**Changes**:
- Optimized algorithm for 30% speed improvement
- Added caching layer for frequently accessed data
- Documented performance benchmarks

**Migration Notes**: Backward compatible. Performance gains automatic upon upgrade.

### v1.3.0 (2025-05-01)

**MINOR**: New feature addition

**Changes**:
- Added optional `EnableLogging` parameter
- Introduced diagnostic mode for troubleshooting
- Expanded examples section

**Migration Notes**: New features are opt-in. Existing code works unchanged.

### v1.2.0 (2025-04-15)

**MINOR**: Documentation enhancements

**Changes**:
- Added "Common Pitfalls" section
- Included real-world usage examples
- Created troubleshooting flowchart

### v1.1.0 (2025-03-01)

**MINOR**: Initial pattern refinements

**Changes**:
- Clarified threading requirements
- Added async/await examples
- Documented exception handling

### v1.0.0 (2025-01-15)

**MAJOR**: Initial release

**Features**:
- Core pattern implementation
- Basic examples
- API reference

## Superseding Documentation

This document does **not** supersede any previous documentation.

If this document were deprecated, the frontmatter would include:
```yaml
status: "deprecated"
supersedes: docs/old/previous-version.md
```

And a deprecation notice would appear here.

## Related Documentation

- [Complete Doc](complete-doc.md)
- [Deprecated Doc](deprecated-doc.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
