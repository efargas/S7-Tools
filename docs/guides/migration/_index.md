---
title: "Migration Guides Index"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "migration", "index", "deprecation"]
related:
  - docs/guides/versioning-guide.md
  - docs/guides/development-workflow.md
---

# Migration Guides Index

This directory contains migration guides for transitioning between different versions of patterns, architectures, and APIs in S7Tools.

## Quick Navigation

| Guide | Purpose | Status |
|-------|---------|--------|
| [Deprecated Patterns](./deprecated-patterns.md) | List of deprecated patterns with replacements | Current |
| [Breaking Changes](./breaking-changes.md) | History of breaking changes by version | Current |

## When to Use Migration Guides

**Use these guides when**:
- Upgrading to a new major version of a pattern
- Migrating from deprecated pattern to current replacement
- Understanding breaking changes between versions
- Planning refactoring work

**Migration workflow**:
1. Identify current pattern/version in use
2. Check [Deprecated Patterns](./deprecated-patterns.md) for replacement
3. Review [Breaking Changes](./breaking-changes.md) for impact
4. Follow specific migration guide with before/after examples
5. Test thoroughly after migration

## Deprecation Policy

S7Tools follows a **2-year retention policy** for deprecated documentation:

```
Deprecation Date → Warning Period → Removal Date
                   (2 years)
```

**Timeline**:
- **T+0 months**: Pattern marked `status: deprecated`
- **T+6 months**: Warnings added to build output (if applicable)
- **T+12 months**: Deprecated pattern moved to archive
- **T+24 months**: Documentation removed completely

**Example**:
```yaml
deprecated-date: "2025-11-10"
removal-date: "2027-11-10"  # 2 years later
```

## Migration Guide Structure

Each migration guide follows this structure:

### 1. Overview
- What changed and why
- Impact assessment
- Estimated migration effort

### 2. Breaking Changes
- List of incompatible changes
- Affected components/files

### 3. Migration Steps
- Step-by-step instructions
- Before/after code examples
- Configuration changes

### 4. Testing
- How to verify migration success
- Test cases to run
- Common issues

### 5. Rollback Plan
- How to revert if needed
- Known risks

## Active Migrations

Currently, there are **no active deprecations** in S7Tools documentation.

All patterns and guides are current and maintained.

## Completed Migrations

### Documentation Reorganization (2025-11-10)

**Status**: ✅ Complete

**Changes**:
- Consolidated scattered documentation into `docs/` directory
- Migrated 10 files with git history preserved
- Added frontmatter metadata to all documents

**Impact**: No code changes required. Documentation paths updated.

**Migration**: See [migration-log.json](../.metadata/migration-log.json)

## Planned Migrations

None currently planned.

## Migration Support

**Need help with migration?**

1. **Check Documentation**:
   - Review the specific migration guide
   - Check [Versioning Guide](../versioning-guide.md)
   - Search [Pattern Index](../../patterns/_index.md)

2. **Review Examples**:
   - Look for real-world usage in codebase
   - Check test files for patterns
   - See deprecated pattern documentation for side-by-side comparison

3. **Test Migration**:
   - Create feature branch
   - Migrate incrementally
   - Run full test suite after each step
   - Verify functionality unchanged

## Contributing Migration Guides

When deprecating a pattern:

1. **Update Original Pattern**:
   ```yaml
   status: "deprecated"
   deprecated-date: "YYYY-MM-DD"
   superseded-by: "docs/patterns/new-pattern.md"
   removal-date: "YYYY-MM-DD"  # deprecated-date + 2 years
   ```

2. **Add to Deprecated Patterns List**:
   - Update [deprecated-patterns.md](./deprecated-patterns.md)
   - Include migration guide section

3. **Document Breaking Changes**:
   - Update [breaking-changes.md](./breaking-changes.md)
   - Add timeline and impact assessment

4. **Create Migration Examples**:
   - Before/after code snippets
   - Configuration changes
   - Test updates

## Version History

### v1.0.0 (2025-11-10)

**MAJOR**: Initial migration guides index

**Features**:
- Migration guides directory structure
- Deprecation policy documentation
- Migration workflow guidelines

**Migration Notes**: N/A (initial release)

## Related Documentation

- [Versioning Guide](../versioning-guide.md)
- [Deprecated Patterns](./deprecated-patterns.md)
- [Breaking Changes](./breaking-changes.md)
- [Development Workflow](../development-workflow.md)

---

**Maintenance**: Keep this index updated when adding new migration guides or completing migrations.
