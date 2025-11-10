---
title: "Documentation Versioning Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "versioning", "semver", "documentation"]
related:
  - docs/guides/memory-bank-usage.md
  - docs/guides/development-workflow.md
  - docs/.metadata/schema.json
---

# Documentation Versioning Guide

This guide explains when and how to increment version numbers for S7Tools documentation following [Semantic Versioning](https://semver.org/) principles adapted for documentation.

## Quick Reference

| Change Type | Version Bump | Examples |
|-------------|--------------|----------|
| **PATCH** | x.y.Z | Typos, broken links, code formatting, clarifications |
| **MINOR** | x.Y.0 | New sections, expanded examples, new workflows (backward compatible) |
| **MAJOR** | X.0.0 | Breaking changes, pattern redesign, incompatible with previous version |

## Semantic Versioning for Documentation

Documentation uses **MAJOR.MINOR.PATCH** format:

```
v2.1.3
│ │ │
│ │ └─ PATCH version (backward compatible bug fixes/clarifications)
│ └─── MINOR version (backward compatible additions)
└───── MAJOR version (incompatible or breaking changes)
```

### PATCH Version (x.y.Z)

Increment PATCH when making backward-compatible fixes:

**Examples**:
- Fix typos or grammar
- Fix broken links
- Correct code formatting
- Add missing punctuation
- Update outdated dates
- Clarify existing content without changing meaning
- Fix markdown rendering issues

**Version History Entry**:
```markdown
### v1.2.3 (2025-11-10)

**PATCH**: Documentation fixes

**Changes**:
- Fixed typo in example code
- Corrected broken link to architecture diagram
- Updated last-updated date

**Migration Notes**: No changes required.
```

### MINOR Version (x.Y.0)

Increment MINOR when adding backward-compatible content:

**Examples**:
- Add new section to existing document
- Expand examples with additional scenarios
- Add new workflow or procedure
- Include new diagrams or screenshots
- Add FAQ entries
- Document new feature (without removing old ones)
- Add troubleshooting steps

**Version History Entry**:
```markdown
### v1.3.0 (2025-11-10)

**MINOR**: Added advanced usage section

**Changes**:
- Added "Advanced Patterns" section with 3 new examples
- Included performance comparison table
- Added troubleshooting guide for edge cases
- Expanded related documentation links

**Migration Notes**: Existing implementations remain valid. New patterns are optional.
```

### MAJOR Version (X.0.0)

Increment MAJOR when making breaking or incompatible changes:

**Examples**:
- Complete rewrite of pattern approach
- Remove deprecated methods/patterns
- Change recommended architecture
- Invalidate previous implementations
- Rename core concepts
- Restructure document significantly
- Supersede previous approach

**Version History Entry**:
```markdown
### v2.0.0 (2025-11-10)

**MAJOR**: Pattern redesign with async/await

**Breaking Changes**:
- Removed synchronous `ProcessData()` method
- Changed parameter order in `Configure()` method
- Updated namespace from `Old.Pattern` to `New.Pattern`
- Deprecated `ILegacyInterface` (removed in v2.0)

**Migration Guide**:

**Before (v1.x)**:
\```csharp
var service = new OldService();
service.ProcessData(param1, param2);
\```

**After (v2.x)**:
\```csharp
var service = new NewService();
await service.ProcessDataAsync(param2, param1); // Note: parameter order changed
\```

**Affected Code**:
- All implementations of `IServiceInterface`
- Test fixtures using `ProcessData()`

**Deprecation Timeline**:
- v1.5.0 (2025-08-01): `ProcessData()` marked `[Obsolete]`
- v2.0.0 (2025-11-10): `ProcessData()` removed completely

**Migration Notes**: Breaking changes require code updates. See migration guide above.
```

## Pre-Release Versions

Use pre-release identifiers for drafts or beta documentation:

```
1.0.0-alpha.1    # Alpha release
1.0.0-beta.2     # Beta release
1.0.0-rc.1       # Release candidate
```

**When to use**:
- Document is in draft status
- Pattern is experimental
- Awaiting review or validation

**Frontmatter**:
```yaml
version: "1.0.0-beta.1"
status: "draft"
```

## Build Metadata

Add build metadata for internal tracking (optional):

```
1.0.0+20251110.001    # Build on 2025-11-10, build #001
2.1.3+hotfix          # Hotfix build
```

## Version History Section

Every documentation file must maintain a Version History section:

```markdown
## Version History

### v2.1.0 (2025-11-10)

**MINOR**: Added new section

**Changes**:
- List of changes
- Another change

**Migration Notes**: Backward compatible.

### v2.0.0 (2025-09-15)

**MAJOR**: Breaking change

**Breaking Changes**:
- What broke
- Why it broke

**Migration Guide**: [detailed steps]

### v1.0.0 (2025-01-15)

**MAJOR**: Initial release

**Features**:
- Initial features
```

## Workflow: Making Documentation Changes

### 1. Determine Version Bump

Ask yourself:
- **Does this break existing implementations?** → MAJOR
- **Does this add new content without breaking old?** → MINOR
- **Does this just fix errors without adding content?** → PATCH

### 2. Update Frontmatter

```yaml
---
version: "2.1.0"  # ← Increment this
last-updated: "2025-11-10"  # ← Update date
---
```

### 3. Add Version History Entry

Add entry at the **top** of Version History section:

```markdown
## Version History

### v2.1.0 (2025-11-10)  # ← New entry here

**MINOR**: Added advanced patterns

**Changes**:
- Your changes here

**Migration Notes**: Backward compatible.

### v2.0.0 (2025-09-15)  # ← Previous entries below
...
```

### 4. Make Content Changes

Edit the document content as needed.

### 5. Validate

```bash
# Validate frontmatter and versioning
python scripts/validate-frontmatter.py docs/

# Check links
markdown-link-check docs/your-file.md
```

### 6. Commit

```bash
git add docs/your-file.md
git commit -m "docs: update pattern-name to v2.1.0

- Added advanced patterns section
- Included performance benchmarks
- Expanded examples

Version: MINOR (backward compatible)"
```

## Deprecation Workflow

When deprecating a pattern or document:

### 1. Update Frontmatter

```yaml
---
title: "Old Pattern (Deprecated)"
version: "1.0.0"  # Keep original version
status: "deprecated"  # ← Change status
deprecated-date: "2025-11-10"  # ← Add date
superseded-by: "docs/patterns/new-pattern.md"  # ← Link replacement
removal-date: "2027-11-10"  # ← 2-year retention
---
```

### 2. Add Deprecation Notice

```markdown
# ⚠️ DEPRECATED: Old Pattern

**This pattern is deprecated as of 2025-11-10.**

**Use instead**: [New Pattern](./new-pattern.md)

**Removal Date**: 2027-11-10 (2-year retention policy)

**Reason**: The old pattern had X limitation. The new pattern provides Y benefit.

## Migration Guide

### Before (Old Pattern v1.0.0)
\```csharp
// Old code
\```

### After (New Pattern v2.0.0)
\```csharp
// New code
\```

### Migration Steps
1. Step 1
2. Step 2
```

### 3. Update Replacement Document

In the new document's frontmatter:

```yaml
---
title: "New Pattern"
version: "2.0.0"
supersedes: "docs/patterns/old-pattern.md"  # ← Link to old
---
```

## Version Queries

Find documentation by version:

```bash
# Find all v2.x.x documents
grep -r "^version: \"2\." docs/ --include="*.md"

# Find documents updated in November 2025
grep -r "last-updated: \"2025-11" docs/ --include="*.md"

# Find deprecated documents
grep -r "status: \"deprecated\"" docs/ --include="*.md"
```

## Best Practices

✅ **DO**:
- Increment version with every content change
- Update `last-updated` field
- Add version history entry
- Use MAJOR for breaking changes
- Include migration guides for MAJOR versions
- Keep version history in reverse chronological order

❌ **DON'T**:
- Skip version bumps for "small" changes
- Forget to update version history
- Use MAJOR for backward-compatible additions
- Remove old version history entries
- Use arbitrary version numbers

## Examples

### Example 1: Fixing a Typo

**Change**: Fix "teh" → "the"

**Version**: `1.2.3` → `1.2.4` (PATCH)

**Version History**:
```markdown
### v1.2.4 (2025-11-10)

**PATCH**: Fixed typo

**Changes**:
- Corrected "teh" to "the" in examples section

**Migration Notes**: No changes required.
```

### Example 2: Adding New Section

**Change**: Add "Performance Optimization" section

**Version**: `1.2.4` → `1.3.0` (MINOR)

**Version History**:
```markdown
### v1.3.0 (2025-11-10)

**MINOR**: Added performance optimization guide

**Changes**:
- Added "Performance Optimization" section
- Included 5 optimization techniques
- Added benchmarking examples

**Migration Notes**: Existing implementations remain valid.
```

### Example 3: Redesigning Pattern

**Change**: Async/await migration

**Version**: `1.3.0` → `2.0.0` (MAJOR)

**Version History**:
```markdown
### v2.0.0 (2025-11-10)

**MAJOR**: Migrated to async/await pattern

**Breaking Changes**:
- Removed synchronous methods
- Changed interface signatures
- Updated namespace

**Migration Guide**: See full guide above.

**Migration Notes**: Breaking changes require code updates.
```

## Tooling

### Validation

```bash
# Check version format
python scripts/validate-frontmatter.py docs/

# Expected output for valid versions:
# ✓ All validations passed
```

### Schema

See `docs/.metadata/schema.json` for the complete frontmatter JSON Schema.

## Related Documentation

- [Deprecated_Property_Migration](../DEPRECATED_PROPERTY_MIGRATION.md)
- [Index](../INDEX.md)
- [Development Workflow](development-workflow.md)
- [Memory Bank Usage](memory-bank-usage.md)
- [_Index](migration/_index.md)
- [Breaking Changes](migration/breaking-changes.md)
- [Deprecated Patterns](migration/deprecated-patterns.md)
- [Pattern Template](../templates/pattern-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
