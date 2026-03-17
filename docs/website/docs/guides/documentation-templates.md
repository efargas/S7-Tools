---
title: Documentation Templates Guide
version: 1.0.3
created: '2025-11-10'
last-updated: '2026-03-17'
status: current
type: tutorial
tags:
- documentation
- templates
- guide
- contribution
related:
- docs/guides/frontmatter-schema.md
- docs/guides/contributing-to-docs.md
- docs/guides/versioning-guide.md
- docs/templates/pattern-template.md
- docs/templates/adr-template.md
---
# Documentation Templates Guide

This guide explains how to use documentation templates effectively and when to use each type.

## Available Templates

### 1. Pattern Template

**Location**: [`templates/pattern-template.md`](../templates/pattern-template.md)

**When to use**: Documenting an implementation pattern or best practice

**Structure**:
- Problem statement
- Solution overview
- Implementation details
- Code examples
- Benefits and trade-offs
- Related patterns
- Anti-patterns

**Example**: [Profile Management Pattern](../patterns/profile-management.md)

### 2. ADR Template

**Location**: [`templates/adr-template.md`](../templates/adr-template.md)

**When to use**: Documenting an architectural decision

**Structure**:
- Title and status
- Context and problem statement
- Decision drivers
- Considered options
- Decision outcome
- Consequences

**Example**: [ADR-0001: UI Framework](../architecture/decisions/0001-ui-framework.md)

### 3. Guide Template

**Use for**: Step-by-step how-to guides and workflows

**Structure**:
- Overview and purpose
- Prerequisites
- Step-by-step instructions
- Common issues and solutions
- Related guides

**Example**: [Development Workflow Guide](development-workflow.md)

### 4. Code Templates

**Locations**:
- [`templates/viewmodel-template.md`](../templates/viewmodel-template.md) - MVVM ViewModels
- [`templates/service-template.md`](../templates/service-template.md) - Service classes
- [`templates/test-template.md`](../templates/test-template.md) - Unit tests

**When to use**: Creating new code files following established patterns

## Template Usage Workflow

### Step 1: Choose the Right Template

| Document Type | Template | Purpose |
|---------------|----------|---------|
| **Pattern** | pattern-template.md | Implementation best practice |
| **Architecture Decision** | adr-template.md | Major architectural choice |
| **How-To Guide** | (structured format) | Step-by-step workflow |
| **ViewModel** | viewmodel-template.cs | New ViewModel class |
| **Service** | service-template.cs | New service class |
| **Test** | test-template.cs | Unit test boilerplate |

### Step 2: Copy Template

```bash
# For documentation
cp docs/templates/pattern-template.md docs/patterns/my-new-pattern.md

# For code
cp docs/templates/viewmodel-template.cs src/S7Tools/ViewModels/MyViewModel.cs
```

### Step 3: Fill In Required Sections

**Documentation Templates**:
1. Update frontmatter (title, version, dates, tags)
2. Replace placeholder text with actual content
3. Add code examples (actual, not pseudo-code)
4. Link to related documentation
5. Ensure all sections are complete

**Code Templates**:
1. Replace namespace placeholders
2. Rename classes/methods
3. Implement required functionality
4. Remove unused sections
5. Add XML documentation comments

### Step 4: Validate

```bash
# Validate documentation frontmatter
python scripts/validate-frontmatter.py docs/

# Check links
find docs -name "*.md" -exec markdown-link-check {} \;

# Full validation
./scripts/validate-all.sh
```

## Frontmatter Requirements

All documentation files **MUST** include YAML frontmatter with these fields:

```yaml
---
title: "Document Title"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current" | "deprecated" | "draft"
tags: ["tag1", "tag2", "tag3"]
related:
  - path/to/related-doc.md
  - path/to/another-doc.md
supersedes: path/to/old-doc.md  # Optional, for deprecations
---
```

**Field Descriptions**:

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `title` | string | Yes | Human-readable title | "Profile Management Pattern" |
| `version` | semver | Yes | Document version (X.Y.Z) | "1.2.0" |
| `created` | date | Yes | Creation date (YYYY-MM-DD) | "2025-01-15" |
| `last-updated` | date | Yes | Last modification date | "2025-11-10" |
| `status` | enum | Yes | current, deprecated, draft | "current" |
| `tags` | array | Yes | Categorization tags (min 1) | ["patterns", "mvvm"] |
| `related` | array | No | Related documentation paths | ["docs/architecture/overview.md"] |
| `supersedes` | string | No | Deprecated predecessor path | "docs/archive/old-pattern.md" |

See [Frontmatter Schema](frontmatter-schema.md) for complete specification.

## Versioning Rules

Follow semantic versioning for documentation:

### PATCH (1.0.0 → 1.0.1)

**When**: Minor corrections without semantic changes

**Examples**:
- Typo fixes
- Formatting improvements
- Broken link fixes
- Grammar corrections

### MINOR (1.0.0 → 1.1.0)

**When**: Additions that are backward compatible

**Examples**:
- New sections added
- Additional examples
- Expanded explanations
- New related links

### MAJOR (1.0.0 → 2.0.0)

**When**: Breaking changes or complete rewrites

**Examples**:
- Pattern completely redesigned
- Approach fundamentally changed
- Previous version no longer valid
- Migration required

See [Versioning Guide](versioning-guide.md) for detailed rules.

## Best Practices

### 1. Write for Your Audience

- **Patterns**: For developers implementing features
- **Architecture**: For understanding system design
- **Guides**: For step-by-step task completion
- **ADRs**: For understanding decision context

### 2. Include Examples

Every pattern MUST include:
- ✅ At least one code example
- ✅ Real-world use case
- ✅ Before/after comparison (if applicable)

Avoid:
- ❌ Pseudo-code only
- ❌ Abstract explanations without examples
- ❌ "You should..." without showing how

### 3. Link Generously

Cross-reference related documentation:
- Link to prerequisite knowledge
- Link to related patterns
- Link to examples and templates
- Link to migration guides for deprecated content

### 4. Keep It Maintainable

- Use relative paths (not absolute)
- Tag content generously for search
- Update `last-updated` date when editing
- Increment version appropriately
- Run validation before committing

### 5. Follow the Structure

Don't deviate from template structure:
- Readers expect consistency
- Validation scripts rely on structure
- Cross-reference generation depends on patterns
- AI agents optimize for predictable structure

## Common Mistakes to Avoid

### ❌ Missing Frontmatter

```markdown
# My Pattern

This is a pattern...
```

**Problem**: Validation fails; no metadata for indexing

**Fix**: Always include complete frontmatter

### ❌ Incorrect Version Format

```yaml
version: "1.2"      # Wrong
version: "v1.2.0"   # Wrong
version: "1.2.0"    # Correct
```

### ❌ Future Dates

```yaml
created: "2025-12-25"  # Wrong if today is 2025-11-10
```

**Fix**: Use actual creation date

### ❌ Broken Relative Links

```markdown
[Pattern](pattern.md)  # Wrong
[Pattern](../patterns/pattern.md)  # Correct
```

### ❌ Empty Tags Array

```yaml
tags: []  # Wrong - must have at least one tag
```

### ❌ Absolute Paths

```markdown
[Link](/home/user/project/docs/pattern.md)  # Wrong
[Link](../patterns/pattern.md)  # Correct
```

## Template Customization

While templates provide structure, customize as needed:

### When to Customize

- **Add sections**: If pattern requires additional context
- **Remove sections**: If not applicable (mark as "N/A", don't delete header)
- **Reorder**: If logical flow improves (keep core structure)

### When NOT to Customize

- **Frontmatter fields**: All required fields must be present
- **Heading levels**: Keep consistent hierarchy
- **File naming**: Follow conventions (see [Code Style Guide](code-style.md))

## Validation Checklist

Before committing new documentation:

- [ ] Frontmatter complete and valid
- [ ] Version follows semver format
- [ ] Dates in ISO8601 format (YYYY-MM-DD)
- [ ] Status is one of: current, deprecated, draft
- [ ] At least one tag present
- [ ] Related links use relative paths
- [ ] All internal links resolve
- [ ] Code examples compile (if applicable)
- [ ] No placeholder text remaining
- [ ] Spell-checked and grammar-checked

Run validation:

```bash
# Full validation suite
./scripts/validate-all.sh

# Or individual checks
python scripts/validate-frontmatter.py docs/
find docs -name "*.md" -exec markdown-link-check {} \;
```

## Getting Help

**Template Questions**:
- Which template? → See "Template Usage Workflow" above
- Frontmatter format? → See [Frontmatter Schema](frontmatter-schema.md)
- Versioning rules? → See [Versioning Guide](versioning-guide.md)

**Validation Errors**:
- Run `python scripts/validate-frontmatter.py docs/` for detailed errors
- Check [scripts/README.md](../scripts/README.md) for script usage

**Contribution Process**:
- See [Contributing to Documentation](contributing-to-docs.md) for complete workflow

## Related Documentation

- [0001 Ui Framework](../architecture/decisions/0001-ui-framework.md)
- [_Index](_index.md)
- [Code Style](code-style.md)
- [Contributing To Docs](contributing-to-docs.md)
- [Development Workflow](development-workflow.md)
- [Frontmatter Schema](frontmatter-schema.md)
- [Versioning Guide](versioning-guide.md)
- [Profile Management](../patterns/profile-management.md)
- [Adr Template](../templates/adr-template.md)
- [Pattern Template](../templates/pattern-template.md)
- [Service Template](../templates/service-template.md)
- [Test Template](../templates/test-template.md)
- [Viewmodel Template](../templates/viewmodel-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
