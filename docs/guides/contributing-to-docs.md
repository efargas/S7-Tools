---
title: "Contributing to Documentation"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-11"
status: "current"
type: "tutorial"
tags: ["documentation", "contribution", "guide", "workflow"]
related:
  - docs/guides/documentation-templates.md
  - docs/guides/frontmatter-schema.md
  - docs/guides/versioning-guide.md
  - docs/guides/code-style.md
---

# Contributing to Documentation

This guide explains how to contribute to the S7Tools documentation system.

## Quick Start

### Before You Begin

1. Read [Documentation Templates Guide](documentation-templates.md) to understand templates
2. Read [Frontmatter Schema](frontmatter-schema.md) to understand metadata requirements
3. Familiarize yourself with the [documentation structure](../INDEX.md)

### Basic Workflow

1. **Find the right file** using [INDEX.md](../INDEX.md)
2. **Edit with proper frontmatter** (see [Frontmatter Schema](frontmatter-schema.md))
3. **Validate your changes** (`./scripts/validate-all.sh`)
4. **Commit with descriptive message**
5. **Create pull request** (if applicable)

## When to Update Documentation

### Always Update

✅ **Required** in these cases:

- New architectural pattern introduced
- Existing pattern modified or enhanced
- New coding standard adopted
- Breaking change in public API
- Constitutional principle affected
- Major refactoring completed

### Sometimes Update

⚠️ **Consider** in these cases:

- Bug fix revealed pattern misunderstanding
- Code review identified documentation gap
- New code template created
- Example added for existing pattern
- Minor clarification needed

### Never Update

❌ **Don't edit** these:

- Auto-generated sections ("Related Documentation")
- Validation reports (run scripts instead)
- Migration logs (use tracking scripts)
- Archived content (except to fix critical errors)

## Contribution Workflow

### Step 1: Determine Change Type

| Change | Action | Version Impact |
|--------|--------|----------------|
| **Typo/formatting** | Edit directly | PATCH (1.0.0 → 1.0.1) |
| **New section** | Edit + add content | MINOR (1.0.0 → 1.1.0) |
| **Major rewrite** | Consider new file + deprecate old | MAJOR (1.0.0 → 2.0.0) |
| **New pattern** | Create from template | New 1.0.0 |

### Step 2: Edit the File

**For Existing Files**:

```bash
# Edit the file
vim docs/patterns/profile-management.md

# Update frontmatter
# - Increment version appropriately
# - Update last-updated date
# - Ensure all required fields present
```

**For New Files**:

```bash
# Copy appropriate template
cp docs/templates/pattern-template.md docs/patterns/my-new-pattern.md

# Fill in all sections
# Replace all placeholder text
# Add proper frontmatter
```

### Step 3: Validate Changes

Run full validation suite:

```bash
./scripts/validate-all.sh
```

Or run individual checks:

```bash
# Frontmatter validation (BLOCKING)
python scripts/validate-frontmatter.py docs/

# Link validation (BLOCKING)
find docs -name "*.md" -exec markdown-link-check {} \;

# Orphan detection (WARNING)
python scripts/detect-orphans.py docs/ --report

# Duplicate detection (WARNING)
python scripts/detect-duplicates.py docs/ --threshold=80
```

**Fix any errors before committing**.

### Step 4: Update Cross-References

If you added a new file or changed relationships:

```bash
# Regenerate cross-references
python scripts/generate-cross-references.py docs/
```

This updates "Related Documentation" sections automatically.

### Step 5: Commit Changes

Use descriptive commit messages:

```bash
git add docs/patterns/my-pattern.md
git commit -m "docs(patterns): Add profile management pattern v1.0.0

- Documented StandardProfileManager<T> usage
- Added examples for gap-filling IDs
- Included ViewModel integration best practices

Refs: #123"
```

**Commit Message Format**:

```
docs(category): Brief description

- Detailed change 1
- Detailed change 2
- Detailed change 3

Refs: #issue-number
```

**Categories**: `architecture`, `patterns`, `guides`, `templates`, `reviews`

### Step 6: Create Pull Request (Optional)

If working in a team:

1. Push to feature branch: `git push origin feature/add-new-pattern`
2. Create PR with descriptive title and body
3. Link to related issues
4. Wait for CI/CD validation to pass
5. Address review comments
6. Merge when approved

## Editing Guidelines

### Frontmatter Requirements

All documentation **MUST** include valid frontmatter:

```yaml
---
title: "Document Title"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["tag1", "tag2"]
related:
  - path/to/related.md
---
```

See [Frontmatter Schema](frontmatter-schema.md) for complete specification.

### Versioning Rules

Follow semantic versioning:

| Type | Increment | When |
|------|-----------|------|
| **PATCH** | 1.0.0 → 1.0.1 | Typos, formatting, clarifications |
| **MINOR** | 1.0.0 → 1.1.0 | New sections, examples, backward compatible |
| **MAJOR** | 1.0.0 → 2.0.0 | Breaking changes, complete rewrite |

See [Versioning Guide](versioning-guide.md) for detailed rules.

### Writing Style

**DO**:
- ✅ Use clear, concise language
- ✅ Include code examples for patterns
- ✅ Link to related documentation
- ✅ Use headings for structure
- ✅ Use tables for comparisons
- ✅ Use lists for sequences
- ✅ Use code blocks with language tags

**DON'T**:
- ❌ Write wall-of-text paragraphs
- ❌ Use jargon without explanation
- ❌ Include pseudo-code without real examples
- ❌ Create orphan files (no incoming links)
- ❌ Use absolute file paths
- ❌ Forget to update `last-updated` date

### Code Examples

**Requirements**:
- Must compile (if applicable)
- Must demonstrate actual usage
- Must include context (not isolated snippets)
- Must follow project coding standards

**Example Structure**:

```csharp
// ✅ Good: Complete, compilable example with context
public class UserProfileViewModel : ReactiveObject
{
    private readonly IProfileService _profileService;
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public UserProfileViewModel(IProfileService profileService)
    {
        _profileService = profileService;
        // ... initialization
    }
}
```

```csharp
// ❌ Bad: Incomplete snippet without context
public string Name { get; set; }
```

### Links and References

**Use Relative Paths**:

```markdown
[Profile Pattern](../patterns/profile-management.md)  # ✅ Good
[Profile Pattern](/docs/patterns/profile-management.md)  # ❌ Bad: absolute
```

**Link Generously**:
- Link to prerequisite knowledge
- Link to related patterns
- Link to examples and templates
- Link to architecture docs
- Link to migration guides

**Avoid**:
- Circular dependencies (A links to B links to A)
- Broken links (validate before committing)
- External links that may break (use sparingly)

## Creating New Documentation

### New Pattern

1. Copy template: `cp docs/templates/pattern-template.md docs/patterns/my-pattern.md`
2. Fill in all sections (don't leave placeholders)
3. Add frontmatter with version `1.0.0`
4. Create example in `docs/patterns/examples/my-pattern-example.cs`
5. Link from [Pattern Catalog](../patterns/_index.md)
6. Run validation: `./scripts/validate-all.sh`
7. Commit and create PR

### New Guide

1. Create file in appropriate `docs/guides/` subdirectory
2. Follow guide structure (Overview, Prerequisites, Steps, Troubleshooting)
3. Add frontmatter with version `1.0.0`
4. Link from [INDEX.md](../INDEX.md)
5. Run validation
6. Commit

### New ADR

1. Copy template: `cp docs/templates/adr-template.md docs/architecture/decisions/NNNN-title.md`
2. Use next number in sequence (e.g., 0003)
3. Fill in all ADR sections
4. Add frontmatter
5. Link from [ADR Index](../architecture/decisions/_index.md)
6. Run validation
7. Commit

### New Template

1. Create file in `docs/templates/`
2. Add extensive comments explaining usage
3. Include placeholder text with `[REPLACE THIS]` markers
4. Add frontmatter
5. Link from [Templates Index](../templates/_index.md)
6. Run validation
7. Commit

## Deprecating Documentation

When content becomes outdated:

### Step 1: Create Replacement

Create new version of the content with improvements.

### Step 2: Update Old Document

```yaml
---
title: "Old Pattern Name"
version: "1.5.0"
created: "2024-01-15"
last-updated: "2025-11-10"
status: "deprecated"  # Changed from "current"
tags: ["pattern", "legacy", "deprecated"]
related:
  - docs/patterns/new-pattern.md
superseded-by: "docs/patterns/new-pattern.md"  # Added
archived-date: "2025-11-10"  # Added
removal-date: "2027-11-10"  # Added (2 years later)
---

# ⚠️ DEPRECATED: Old Pattern Name

**This pattern has been deprecated and replaced by [New Pattern](new-pattern.md).**

**Deprecation Date**: 2025-11-10
**Removal Date**: 2027-11-10 (2-year retention period)

## Migration Guide

To migrate from this pattern to the new pattern:

1. Read [New Pattern](new-pattern.md) documentation
2. Update your code following these steps...
3. Test thoroughly

[Original content below is outdated...]
```

### Step 3: Move to Archive

After transition period (typically 3 months):

```bash
git mv docs/patterns/old-pattern.md docs/archive/old-pattern.md
```

### Step 4: Update Archive Index

Add entry to `docs/archive/_index.md` with:
- Deprecation reason
- Replacement link
- Removal date

## Quality Checklist

Before committing, verify:

- [ ] Frontmatter complete and valid
- [ ] Version incremented appropriately
- [ ] `last-updated` date is today
- [ ] All required fields present
- [ ] No placeholder text remaining
- [ ] Code examples compile (if applicable)
- [ ] All internal links work
- [ ] Related documentation linked
- [ ] No spelling/grammar errors
- [ ] Validation passes: `./scripts/validate-all.sh`

## CI/CD Integration

Documentation validation runs automatically on every PR:

### Blocking Gates (Must Pass)

- ✅ **Frontmatter Validation**: All required fields present and valid
- ✅ **Link Validation**: All internal links resolve

### Warning Gates (Non-Blocking)

- ⚠️ **Orphan Detection**: Files with no incoming links
- ⚠️ **Duplicate Detection**: Similar content across files

**View Results**: Check GitHub Actions workflow output

## Common Issues

### Issue: Validation Fails with "Missing Required Field"

**Solution**: Add missing frontmatter field

```yaml
version: "1.0.0"  # Add this
```

### Issue: Link Validation Fails

**Solution**: Fix broken links or update to correct paths

```bash
# Find broken links
find docs -name "*.md" -exec markdown-link-check {} \;

# Fix paths
[Pattern](../patterns/profile-management.md)  # Correct relative path
```

### Issue: Orphan File Detected

**Solution**: Link from relevant documentation or INDEX.md

```markdown
# In docs/patterns/_index.md
- [My New Pattern](my-new-pattern.md)
```

### Issue: Duplicate Content Detected

**Solution**: Consolidate duplicates or differentiate content

```bash
# Check duplicates
python scripts/detect-duplicates.py docs/ --threshold=80
```

## Getting Help

**Documentation Questions**:
- Template usage → [Documentation Templates Guide](documentation-templates.md)
- Frontmatter format → [Frontmatter Schema](frontmatter-schema.md)
- Versioning rules → [Versioning Guide](versioning-guide.md)

**Validation Errors**:
- Run `python scripts/validate-frontmatter.py docs/` for details
- Check [scripts/README.md](../../scripts/README.md) for script usage

**Workflow Questions**:
- General development → [Development Workflow](development-workflow.md)
- Code style → [Code Style Guide](code-style.md)

## Related Documentation

- [Index](../INDEX.md)
- [Readme](../README.md)
- [_Index](../architecture/decisions/_index.md)
- [Validation_Quick_Reference](VALIDATION_QUICK_REFERENCE.md)
- [_Index](_index.md)
- [Archive Management](archive-management.md)
- [Code Style](code-style.md)
- [Development Workflow](development-workflow.md)
- [Documentation Templates](documentation-templates.md)
- [Frontmatter Schema](frontmatter-schema.md)
- [Versioning Guide](versioning-guide.md)
- [_Index](../patterns/_index.md)
- [Profile Management](../patterns/profile-management.md)
- [_Index](../templates/_index.md)
- [Guide Template](../templates/guide-template.md)
- [Pattern Template](../templates/pattern-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
