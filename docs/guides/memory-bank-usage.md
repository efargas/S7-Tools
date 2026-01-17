---
title: "Documentation Maintenance Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
type: "tutorial"
tags: ["guide", "documentation", "maintenance", "memory-bank"]
related:
  - docs/INDEX.md
  - docs/guides/development-workflow.md
  - docs/architecture/overview.md
---

# Documentation Maintenance Guide

This guide explains how to maintain the consolidated documentation structure (formerly "Memory Bank") in the `docs/` directory.

## Documentation Structure Overview

```
docs/                           # Single source of truth for all documentation
├── INDEX.md                    # Master index - start here
├── architecture/               # System architecture & decisions
│   ├── overview.md
│   ├── clean-architecture.md
│   ├── mvvm-patterns.md
│   ├── diagrams.md
│   └── decisions/             # Architecture Decision Records (ADRs)
├── patterns/                   # Implementation patterns
│   ├── _index.md
│   ├── profile-management.md
│   └── examples/              # Code examples
├── guides/                     # How-to guides
│   ├── onboarding.md
│   ├── development-workflow.md
│   ├── testing-guide.md
│   └── migration/
├── templates/                  # Code templates
├── reviews/                    # Code reviews
└── archive/                    # Deprecated docs
```

## When to Update Documentation

### Always Update

Update documentation in the **same PR** as code changes when:

✅ New architectural pattern introduced
✅ Existing pattern modified significantly
✅ New coding standard adopted
✅ Breaking change in public API
✅ Constitutional principle affected
✅ New reusable component created

### Sometimes Update

Consider updating when:

⚠️ Bug fix revealed pattern misunderstanding
⚠️ Code review identified documentation gap
⚠️ New template created
⚠️ Example added for existing pattern

### Never Update Manually

These are auto-generated:

❌ Cross-reference links ("Related Documentation" sections)
❌ Validation reports
❌ Migration logs
❌ Generated indexes

## Documentation Workflow

### 1. Find the Right File

Use the master index to locate the appropriate category:

```bash
# Open master index
cat docs/INDEX.md

# Search for topic
grep -r "profile management" docs/

# List files in category
ls docs/patterns/
```

**Categories**:
- **architecture/**: System design, ADRs, architectural patterns
- **patterns/**: Implementation patterns, best practices
- **guides/**: How-to guides, workflows, onboarding
- **templates/**: Code templates, boilerplate
- **reviews/**: Code reviews, quality reports

### 2. Check Current Version

```bash
# Read frontmatter to check version and status
head -15 docs/patterns/profile-management.md
```

Verify:
- File has `status: current` (not `deprecated`)
- Version is appropriate for your changes
- No `supersedes` field (unless this is a replacement)

### 3. Edit with Proper Frontmatter

Every documentation file MUST have YAML frontmatter:

```markdown
---
title: "Document Title"
version: "1.0.0"              # Semantic versioning
created: "2025-01-15"          # ISO8601 date
last-updated: "2025-11-10"     # Update this!
status: "current"              # current | deprecated | draft
tags: ["relevant", "tags"]     # For search/filtering
related:                       # Cross-references
  - docs/related/file.md
  - docs/another/file.md
supersedes: docs/old/file.md   # Optional: if replacing
---

# Document Title

Content starts here...
```

### 4. Version Bumping Rules

Follow [Semantic Versioning](https://semver.org/) for documentation:

| Change Type | Version Bump | Example |
|-------------|--------------|---------|
| **PATCH** | Typos, clarifications, no semantic change | 1.0.0 → 1.0.1 |
| **MINOR** | New sections, expanded examples, backward compatible | 1.0.0 → 1.1.0 |
| **MAJOR** | Breaking changes, pattern redesign, incompatible | 1.0.0 → 2.0.0 |

```bash
# Patch: Fix typo
version: "1.0.1"

# Minor: Add new section
version: "1.1.0"

# Major: Complete rewrite
version: "2.0.0"
```

### 5. Run Validation

```bash
# Validate frontmatter
python scripts/validate-frontmatter.py docs/

# Check links
find docs -name "*.md" -exec markdown-link-check {} \;

# Generate cross-references (auto-updates "Related" sections)
python scripts/generate-cross-references.py docs/

# Full validation suite
./scripts/validate-all.sh
```

### 6. Commit with Documentation

```bash
# Stage code and documentation together
git add src/ docs/

# Commit with documentation flag
git commit -m "feat(profiles): Add duplicate detection

- Implement MD5 hash comparison
- Add UI warning for duplicates
- Update profile-management.md pattern (v1.2.0)
- Add example to docs/patterns/examples/

Refs: #123"
```

## Common Documentation Tasks

### Task: Document a New Pattern

1. **Create Pattern File**
   ```bash
   # Use naming convention: lowercase-with-hyphens.md
   touch docs/patterns/my-new-pattern.md
   ```

2. **Add Frontmatter and Content**
   ```markdown
   ---
   title: "My New Pattern"
   version: "1.0.0"
   created: "2025-11-10"
   last-updated: "2025-11-10"
   status: "current"
   tags: ["patterns", "specific-domain"]
   related:
     - docs/architecture/overview.md
   ---

   # My New Pattern

   ## Problem
   What problem does this pattern solve?

   ## Solution
   How does it solve it?

   ## Implementation
   Code examples...

   ## Benefits
   Why use this pattern?

   ## Anti-Patterns
   What to avoid?
   ```

3. **Update Pattern Index**
   ```bash
   # Add entry to docs/patterns/_index.md
   vim docs/patterns/_index.md
   ```

4. **Generate Cross-References**
   ```bash
   python scripts/generate-cross-references.py docs/patterns/
   ```

### Task: Update Existing Pattern

1. **Read Current Version**
   ```bash
   cat docs/patterns/profile-management.md | head -20
   ```

2. **Make Changes**
   - Update `last-updated` field
   - Bump `version` appropriately (PATCH/MINOR/MAJOR)
   - Add/modify content

3. **Document Changes**
   ```markdown
   ## Version History

   ### v1.2.0 (2025-11-10)
   - Added section on concurrent access handling
   - Included new example for multi-threaded scenarios
   - Fixed broken link to internal-method pattern

   ### v1.1.0 (2025-10-15)
   - Added gap-filling ID assignment section
   - Expanded ViewModel integration examples
   ```

4. **Validate**
   ```bash
   python scripts/validate-frontmatter.py docs/patterns/profile-management.md
   ```

### Task: Deprecate a Pattern

1. **Create New Replacement** (if applicable)
   ```bash
   # Create improved version
   touch docs/patterns/improved-pattern.md
   ```

2. **Update Old Pattern**
   ```markdown
   ---
   title: "Old Pattern (Deprecated)"
   version: "1.0.0"
   status: "deprecated"           # Change to deprecated
   tags: ["patterns", "deprecated"]
   ---

   # ⚠️ DEPRECATED: Old Pattern

   **This pattern is deprecated as of 2025-11-10.**

   **Use instead**: [Improved Pattern](./improved-pattern.md)

   **Reason**: Old approach had X limitation; new pattern provides Y benefit.

   ## Migration Guide

   How to migrate from old to new...
   ```

3. **Link New Pattern**
   ```markdown
   ---
   title: "Improved Pattern"
   supersedes: docs/patterns/old-pattern.md
   ---
   ```

4. **Update References**
   - Find all links to old pattern: `grep -r "old-pattern.md" docs/`
   - Update to point to new pattern
   - Run cross-reference generator

### Task: Add Code Example

1. **Create Example File**
   ```bash
   touch docs/patterns/examples/profile-manager-example.cs
   ```

2. **Add Frontmatter** (for .md files) or header comment (for code files)
   ```csharp
   // Example: Profile Manager Implementation
   // Pattern: Profile Management (docs/patterns/profile-management.md)
   // Created: 2025-11-10

   using System;
   using S7Tools.Core.Models;

   public class Example
   {
       // Example implementation...
   }
   ```

3. **Link from Pattern**
   ```markdown
   ## Examples

   See [Profile Manager Example](./examples/profile-manager-example.cs) for a complete implementation.
   ```

## Cross-Reference Management

### Automatic Cross-References

The cross-reference generator analyzes:
- `related` field in frontmatter
- Inline Markdown links to other docs
- `supersedes` relationships

And generates:
- "Related Documentation" sections at end of files
- Bidirectional links (A→B implies B→A)
- Orphan detection reports

### Running Cross-Reference Generator

```bash
# Generate for all docs
python scripts/generate-cross-references.py docs/

# Dry run (preview without changes)
python scripts/generate-cross-references.py docs/ --dry-run

# Generate relationship graph (JSON)
python scripts/generate-cross-references.py docs/ --output=docs/.metadata/graph.json
```

### Manual Cross-References

Add to `related` field in frontmatter:

```yaml
related:
  - docs/architecture/clean-architecture.md  # Related architecture
  - docs/patterns/internal-method.md         # Related pattern
  - docs/guides/development-workflow.md      # Related workflow
```

**Rules**:
- Use relative paths from repository root
- Start with `docs/`
- Include file extension `.md`
- Maximum 5-7 related docs per file

## Validation and Quality

### Pre-Commit Validation

```bash
# Check all documentation before commit
./scripts/validate-all.sh

# Or run individual checks
python scripts/validate-frontmatter.py docs/
find docs -name "*.md" -exec markdown-link-check {} \;
python scripts/detect-orphans.py docs/ --exclude="INDEX.md,README.md"
```

### Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| **LINK-001** | Error | All internal links must resolve |
| **META-001** | Error | All files must have complete frontmatter |
| **META-002** | Error | Version must be semver format |
| **META-003** | Error | Dates must be ISO8601 format |
| **STRUCT-001** | Error | Max 3-level directory depth |
| **CONTENT-001** | Warning | Duplicate content detection |

### CI/CD Integration

Validation runs automatically on every PR:

```yaml
# GitHub Actions workflow
- name: Validate Documentation
  run: |
    python scripts/validate-frontmatter.py docs/
    find docs -name "*.md" -exec markdown-link-check {} \;
```

**Blocking Gates**:
- ✅ Frontmatter validation
- ✅ Link validation

**Warning Gates**:
- ⚠️ Orphan detection
- ⚠️ Duplicate content

## Search and Discovery

### Finding Documentation

```bash
# Search by topic
grep -r "profile management" docs/

# Search by tag
grep -r "tags:.*mvvm" docs/

# List all patterns
ls docs/patterns/

# Find files modified recently
find docs/ -name "*.md" -mtime -7

# Search cross-references
python scripts/generate-cross-references.py docs/ --output=graph.json
cat docs/.metadata/graph.json | jq '.edges[] | select(.source | contains("profile"))'
```

### Navigation Tips

**For AI Agents**:
1. Start at `docs/INDEX.md`
2. Identify category (architecture/patterns/guides)
3. Read category `_index.md` for overview
4. Follow cross-references to related docs

**For Humans**:
1. Use `grep` for keyword search
2. Browse category directories
3. Check `_index.md` files for catalog
4. Follow "Related Documentation" links

## Migration and Archive

### Moving Files

```bash
# ALWAYS use git mv to preserve history
git mv docs/old/location.md docs/new/location.md

# Record migration
python scripts/track-migration.py add \
  --old=docs/old/location.md \
  --new=docs/new/location.md

# Update all references
grep -r "old/location.md" docs/
# ... manually update found references ...
```

### Archiving Old Documentation

When documentation becomes outdated:

1. **Move to Archive**
   ```bash
   git mv docs/patterns/old-pattern.md docs/archive/old-pattern.md
   ```

2. **Update Frontmatter**
   ```yaml
   status: "deprecated"
   archived: "2025-11-10"
   removal-date: "2027-11-10"  # 2 years retention
   replacement: "docs/patterns/new-pattern.md"
   ```

3. **Update Archive Index**
   ```bash
   vim docs/archive/_index.md
   # Add entry with deprecation reason
   ```

4. **Create Redirect Stub** (optional, for transition period)
   ```markdown
   # [MOVED] Old Pattern

   This file has moved to: [New Pattern](../patterns/new-pattern.md)

   **Removal Date**: 2026-02-10 (3 months)
   ```

## Best Practices

### DO

✅ Update documentation in same PR as code
✅ Use semantic versioning for docs
✅ Add complete frontmatter to all files
✅ Run validation before commit
✅ Link related documentation
✅ Include examples for patterns
✅ Document "why" not just "what"
✅ Keep directory depth ≤ 3 levels

### DON'T

❌ Skip frontmatter metadata
❌ Hardcode absolute paths in links
❌ Create orphaned files
❌ Duplicate content across files
❌ Use inconsistent naming
❌ Ignore broken links
❌ Commit without validation
❌ Delete old files (archive instead)

## Troubleshooting

### Validation Errors

```bash
# Error: Missing frontmatter
# Fix: Add YAML frontmatter at file top

# Error: Broken link
# Fix: Update link path or create missing file

# Error: Invalid version format
# Fix: Use X.Y.Z format (e.g., 1.2.3)

# Error: Orphaned file
# Fix: Add link from relevant doc or add to index
```

### Git History Issues

```bash
# Verify history preserved after migration
git log --follow docs/new/location.md

# If history lost, use git filter-branch (advanced)
# Better: Always use git mv, never rm + add
```

## References

- [Master Index](../INDEX.md)
- [Development Workflow](./development-workflow.md)
- [Architecture Overview](../architecture/overview.md)
- [Pattern Catalog](../patterns/_index.md)

## Tools

- **Validation**: `scripts/validate-frontmatter.py`
- **Link Checking**: `markdown-link-check`
- **Cross-References**: `scripts/generate-cross-references.py`
- **Orphan Detection**: `scripts/detect-orphans.py`
- **Migration Tracking**: `scripts/track-migration.py`
- **Full Suite**: `scripts/validate-all.sh`

---

*Last Updated*: 2025-11-10

## Related Documentation

- [Index](../INDEX.md)
- [Readme](../README.md)
- [Overview](../architecture/overview.md)
- [_Index](_index.md)
- [Development Workflow](development-workflow.md)
- [Versioning Guide](versioning-guide.md)
- [_Index](../patterns/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
