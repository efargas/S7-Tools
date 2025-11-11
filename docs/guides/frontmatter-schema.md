---
title: "Frontmatter Schema Documentation"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-11"
status: "current"
tags: ["documentation", "schema", "metadata", "frontmatter"]
related:
  - docs/guides/documentation-templates.md
  - docs/guides/versioning-guide.md
  - docs/guides/contributing-to-docs.md
---

# Frontmatter Schema Documentation

This document defines the complete YAML frontmatter schema required for all S7Tools documentation files.

## Overview

All Markdown documentation files in the `docs/` directory **MUST** include valid YAML frontmatter at the beginning of the file. This metadata enables:

- Automated validation and quality checks
- Cross-reference generation
- Version tracking and history
- Search and categorization
- Deprecation management

## Required Schema

```yaml
---
title: "Document Title"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["tag1", "tag2"]
related:
  - path/to/related-doc.md
supersedes: path/to/old-doc.md  # Optional
---
```

## Field Specifications

### 1. title (Required)

**Type**: `string`

**Description**: Human-readable document title

**Validation Rules**:
- Must be non-empty
- Should be sentence case (not ALL CAPS)
- Should not exceed 80 characters
- Should not include version number (use `version` field)

**Examples**:
```yaml
title: "Profile Management Pattern"           # ✅ Good
title: "Clean Architecture Implementation"    # ✅ Good
title: "PROFILE MANAGEMENT"                   # ❌ Bad: ALL CAPS
title: "Profile Management Pattern v1.2.0"    # ❌ Bad: includes version
title: ""                                      # ❌ Bad: empty
```

### 2. version (Required)

**Type**: `SemanticVersion` (string in X.Y.Z format)

**Description**: Document version following semantic versioning

**Validation Rules**:
- Must match pattern: `^\d+\.\d+\.\d+$`
- Must have exactly 3 numeric components
- No `v` prefix
- No pre-release or build metadata

**Examples**:
```yaml
version: "1.0.0"      # ✅ Good: initial version
version: "1.2.3"      # ✅ Good: standard semver
version: "2.0.0"      # ✅ Good: major version
version: "1.2"        # ❌ Bad: missing patch version
version: "v1.2.0"     # ❌ Bad: 'v' prefix
version: "1.2.0-beta" # ❌ Bad: pre-release identifier
```

**Semantic Versioning Rules**:

| Change Type | Version Increment | Example |
|-------------|-------------------|---------|
| **PATCH** | 1.0.0 → 1.0.1 | Typo fixes, formatting |
| **MINOR** | 1.0.0 → 1.1.0 | New sections, examples |
| **MAJOR** | 1.0.0 → 2.0.0 | Complete rewrite, breaking changes |

See [Versioning Guide](versioning-guide.md) for detailed rules.

### 3. created (Required)

**Type**: `ISO8601Date` (string in YYYY-MM-DD format)

**Description**: Document creation date

**Validation Rules**:
- Must match pattern: `^\d{4}-\d{2}-\d{2}$`
- Must be valid calendar date
- Must not be in the future
- Must be on or before `last-updated` date

**Examples**:
```yaml
created: "2025-01-15"     # ✅ Good: valid date
created: "2025-11-10"     # ✅ Good: today's date
created: "2025-12-25"     # ❌ Bad: future date (if today is 2025-11-10)
created: "2025/11/10"     # ❌ Bad: wrong delimiter
created: "11-10-2025"     # ❌ Bad: wrong order (must be YYYY-MM-DD)
created: "2025-13-01"     # ❌ Bad: invalid month
```

### 4. last-updated (Required)

**Type**: `ISO8601Date` (string in YYYY-MM-DD format)

**Description**: Date of last modification

**Validation Rules**:
- Must match pattern: `^\d{4}-\d{2}-\d{2}$`
- Must be valid calendar date
- Must not be in the future
- Must be on or after `created` date
- Should be updated whenever content changes

**Examples**:
```yaml
created: "2025-01-15"
last-updated: "2025-11-10"  # ✅ Good: after creation date
last-updated: "2025-01-15"  # ✅ Good: same as creation (not yet updated)
last-updated: "2025-01-10"  # ❌ Bad: before creation date
```

### 5. status (Required)

**Type**: `enum` (one of: `current`, `deprecated`, `draft`)

**Description**: Document lifecycle status

**Validation Rules**:
- Must be one of the allowed values
- Case-sensitive (lowercase only)

**Allowed Values**:

| Status | Meaning | Use When |
|--------|---------|----------|
| `current` | Active, up-to-date | Standard published documentation |
| `deprecated` | Superseded by newer version | Content replaced, being phased out |
| `draft` | Work in progress | Incomplete or under review |

**Examples**:
```yaml
status: "current"      # ✅ Good: standard status
status: "deprecated"   # ✅ Good: for old content
status: "draft"        # ✅ Good: work in progress
status: "Current"      # ❌ Bad: wrong case
status: "archived"     # ❌ Bad: not an allowed value
status: "wip"          # ❌ Bad: use "draft" instead
```

**Deprecation Workflow**:

When deprecating a document:
1. Set `status: "deprecated"`
2. Add `supersedes` field pointing to replacement
3. Update `last-updated` date
4. Increment version (usually MAJOR)
5. Move to `docs/archive/` directory (after transition period)

### 6. tags (Required)

**Type**: `array<string>` (minimum 1 element)

**Description**: Categorization and search tags

**Validation Rules**:
- Must be non-empty array
- At least one tag required
- Tags should be lowercase
- Use hyphens for multi-word tags
- No special characters except hyphens

**Common Tags**:

| Category | Tags |
|----------|------|
| **Document Type** | `architecture`, `pattern`, `guide`, `template`, `review`, `adr` |
| **Technology** | `mvvm`, `reactiveui`, `clean-architecture`, `di`, `async`, `threading` |
| **Domain** | `profile-management`, `logging`, `ui`, `services`, `testing` |
| **Complexity** | `beginner`, `intermediate`, `advanced` |
| **Status** | `core`, `optional`, `experimental` |

**Examples**:
```yaml
tags: ["patterns", "profile-management", "reactive"]  # ✅ Good: relevant tags
tags: ["guide", "testing", "advanced"]                # ✅ Good: categorized
tags: ["Architecture", "MVVM"]                        # ❌ Bad: capital letters
tags: ["patterns"]                                     # ⚠️ OK but minimal
tags: []                                               # ❌ Bad: empty array
```

**Tag Naming Guidelines**:
- Use singular form: `pattern` not `patterns`
- Be specific: `profile-management` not `profiles`
- Avoid redundancy: Don't duplicate document type in tags
- Limit to 3-5 tags per document

### 7. related (Optional)

**Type**: `array<RelativePath>`

**Description**: Links to related documentation

**Validation Rules**:
- Array can be empty or omitted
- Paths must be relative to repository root
- Referenced files must exist
- No circular references (A relates to B relates to A)

**Examples**:
```yaml
related:
  - docs/patterns/profile-management.md
  - docs/architecture/clean-architecture.md
  - docs/guides/development-workflow.md
```

**Best Practices**:
- Link to prerequisite knowledge
- Link to related patterns/architectures
- Link to examples and templates
- Avoid linking to every vaguely related document (max 5-7 links)

### 8. supersedes (Optional)

**Type**: `RelativePath` (string)

**Description**: Path to deprecated predecessor document

**Validation Rules**:
- Only used when `status: "deprecated"`
- Referenced file must exist
- Should point to archived content

**Examples**:
```yaml
status: "deprecated"
supersedes: "docs/archive/old-profile-pattern.md"  # ✅ Good
```

**Deprecation Pattern**:

Old file (`docs/archive/old-profile-pattern.md`):
```yaml
status: "deprecated"
superseded-by: "docs/patterns/profile-management.md"
archived-date: "2025-11-10"
removal-date: "2027-11-10"
```

New file (`docs/patterns/profile-management.md`):
```yaml
status: "current"
supersedes: "docs/archive/old-profile-pattern.md"
```

## Complete Examples

### Standard Pattern Document

```yaml
---
title: "Profile Management Pattern"
version: "2.1.0"
created: "2025-01-15"
last-updated: "2025-11-10"
status: "current"
tags: ["pattern", "profile-management", "reactive", "di"]
related:
  - docs/architecture/clean-architecture.md
  - docs/patterns/internal-method.md
  - docs/guides/development-workflow.md
---
```

### Deprecated Document

```yaml
---
title: "Legacy Profile Pattern"
version: "1.5.0"
created: "2024-05-20"
last-updated: "2025-11-10"
status: "deprecated"
tags: ["pattern", "profile-management", "legacy"]
related:
  - docs/patterns/profile-management.md
superseded-by: "docs/patterns/profile-management.md"
archived-date: "2025-11-10"
removal-date: "2027-11-10"
---
```

### Draft Guide

```yaml
---
title: "Advanced Testing Strategies"
version: "0.1.0"
created: "2025-11-05"
last-updated: "2025-11-10"
status: "draft"
tags: ["guide", "testing", "advanced", "wip"]
related:
  - docs/guides/testing-guide.md
---
```

## Validation

### Automated Validation

Run frontmatter validation script:

```bash
python scripts/validate-frontmatter.py docs/
```

**Output Example**:

```
Validating frontmatter in docs/...

[ERROR] docs/patterns/profile-management.md
  - Missing required field: version
  - Invalid date format: last-updated (expected YYYY-MM-DD, got 2025/11/10)

[WARNING] docs/guides/onboarding.md
  - No related links specified (consider adding cross-references)

Summary:
  Files checked: 67
  Errors: 2
  Warnings: 1

Exit code: 1 (errors found)
```

### Manual Validation Checklist

Before committing documentation:

- [ ] All required fields present (title, version, created, last-updated, status, tags)
- [ ] Version follows semver (X.Y.Z)
- [ ] Dates in ISO8601 format (YYYY-MM-DD)
- [ ] `last-updated` >= `created`
- [ ] `status` is one of: current, deprecated, draft
- [ ] At least one tag present
- [ ] Tags are lowercase with hyphens
- [ ] Related paths use relative paths from repo root
- [ ] Related files exist
- [ ] If deprecated, `supersedes` field present

## Common Errors and Solutions

### Error: Missing Required Field

```
[ERROR] Missing required field: version
```

**Solution**: Add missing field to frontmatter

```yaml
version: "1.0.0"
```

### Error: Invalid Version Format

```
[ERROR] Invalid version format (expected X.Y.Z, got 1.2)
```

**Solution**: Use three-part semver

```yaml
version: "1.2.0"  # Not "1.2"
```

### Error: Invalid Date Format

```
[ERROR] Invalid date format: created (expected YYYY-MM-DD, got 11-10-2025)
```

**Solution**: Use ISO8601 format

```yaml
created: "2025-11-10"  # Not "11-10-2025"
```

### Error: Future Date

```
[ERROR] Date cannot be in the future: created
```

**Solution**: Use current or past date

```yaml
created: "2025-11-10"  # Not "2025-12-25"
```

### Error: Empty Tags

```
[ERROR] Tags array must contain at least one tag
```

**Solution**: Add relevant tags

```yaml
tags: ["pattern", "profile-management"]
```

### Error: Invalid Status

```
[ERROR] Status must be one of: current, deprecated, draft
```

**Solution**: Use allowed value

```yaml
status: "current"  # Not "active" or "published"
```

## Schema Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-10 | Initial schema specification |

## Related Documentation

- [Documentation Templates Guide](documentation-templates.md) - How to use templates
- [Versioning Guide](versioning-guide.md) - Semantic versioning for documentation
- [Contributing to Documentation](contributing-to-docs.md) - Contribution workflow
- [Pattern Template](../templates/pattern-template.md) - Pattern documentation template

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
