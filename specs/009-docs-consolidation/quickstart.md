# Quickstart Guide: Using the Consolidated Documentation

**Feature**: Documentation Consolidation and Reorganization
**Date**: 2025-11-10
**Audience**: Developers and AI Agents

## Overview

This guide helps you navigate and use the new consolidated documentation structure. Whether you're a human developer, an AI coding agent, or a contributor, you'll find everything you need to quickly locate information and contribute updates.

## 30-Second Tour

### Finding Information

**Start Here**: `docs/INDEX.md` - The master documentation index

**Quick Navigation**:

1. **Architecture & Patterns**: Need to understand how the system works?
    - → `docs/architecture/` for system design
    - → `docs/patterns/` for implementation patterns
2. **How-To Guides**: Need to do something specific?
    - → `docs/guides/` for step-by-step workflows
3. **Templates & Examples**: Need a code template?
    - → `docs/templates/` for boilerplate code
4. **Code Reviews**: Checking quality baselines?
    - → `docs/reviews/LATEST.md` for current quality state

**Search Tips**:

```bash
# Find all documentation about a topic
grep -r "profile management" docs/

# Find all patterns
ls docs/patterns/

# Search by tag
grep -r "tags: \[.*mvvm.*\]" docs/
```

## For AI Coding Agents

### Rapid Onboarding (< 30 seconds)

**Step 1**: Read `docs/INDEX.md` (10 seconds)

- Understand documentation structure
- Identify relevant categories

**Step 2**: Read `docs/architecture/overview.md` (10 seconds)

- Grasp system architecture (Clean Architecture + MVVM)
- Understand layer boundaries

**Step 3**: Read `docs/patterns/_index.md` (10 seconds)

- Scan available patterns
- Bookmark relevant patterns for current task

**Step 4**: Reference specific pattern as needed

- Follow links from index to detailed pattern documentation
- Check examples for implementation guidance

### Context Gathering Strategy

```python
# Efficient AI agent context gathering workflow
def gather_context_for_task(task_description):
    # Phase 1: Understand architecture (always read)
    read("docs/architecture/overview.md")
    read("docs/architecture/clean-architecture.md")

    # Phase 2: Identify relevant patterns (selective read)
    patterns_index = read("docs/patterns/_index.md")
    relevant_patterns = filter_by_tags(patterns_index, task_description)
    for pattern in relevant_patterns[:3]:  # Top 3 most relevant
        read(pattern)

    # Phase 3: Check for existing examples (verify understanding)
    if has_similar_feature(task_description):
        existing_code = find_similar_implementation()
        cross_reference_with_patterns(existing_code)

    # Phase 4: Template selection (if creating new code)
    template = select_template(task_description)
    read(template)

    return context
```

### Key Files for AI Agents

| File | Purpose | When to Read |
|------|---------|--------------|
| `docs/INDEX.md` | Master navigation | Every session (first step) |
| `docs/architecture/overview.md` | System architecture | Every session (onboarding) |
| `docs/patterns/_index.md` | Pattern catalog | When implementing features |
| `docs/guides/development-workflow.md` | Development process | Before making changes |
| `docs/templates/` | Code templates | When creating new files |
| `docs/reviews/LATEST.md` | Quality baseline | Before submitting work |

## For Human Developers

### Daily Workflow

**Morning**: Check for updates

```bash
git pull
# Read any updated documentation
git diff main docs/
```

**During Development**:

1. **Find Pattern**: Check `docs/patterns/` for relevant implementation pattern
2. **Copy Template**: Start from `docs/templates/` for new code
3. **Follow Guide**: Use `docs/guides/` for workflows (testing, building, etc.)
4. **Cross-Reference**: Click "Related Documentation" links for context

**Before Commit**:

1. Run validation scripts (see below)
2. Update documentation if you changed architecture/patterns
3. Link code review findings to affected patterns

### Common Tasks

#### Task: Implement a new feature

1. Read feature spec in `specs/XXX-feature-name/spec.md`
2. Check `docs/architecture/clean-architecture.md` for layer boundaries
3. Find similar feature: search `docs/patterns/` for related patterns
4. Copy template from `docs/templates/`
5. Follow pattern examples from `docs/patterns/examples/`
6. Write tests (see `docs/guides/testing-guide.md`)

#### Task: Fix a bug

1. Understand current behavior: read relevant pattern docs
2. Check code review history: `docs/reviews/` for known issues
3. Verify fix doesn't break architectural rules
4. Update pattern docs if bug revealed misunderstanding

#### Task: Onboard to project

1. Read `docs/guides/onboarding.md` (new developer guide)
2. Read `docs/architecture/overview.md` (system architecture)
3. Skim `docs/patterns/_index.md` (available patterns)
4. Build and run project (see `README.md`)
5. Read `docs/guides/development-workflow.md` (day-to-day process)

## Documentation Updates

### When to Update Documentation

**Always Update**:

- New architectural pattern introduced
- Existing pattern modified
- New coding standard adopted
- Breaking change in public API
- Constitutional principle affected

**Sometimes Update**:

- Bug fix revealed pattern misunderstanding
- Code review identified documentation gap
- New template created
- Example added for existing pattern

**Never Update** (auto-generated):

- Cross-reference links ("Related Documentation" sections)
- Validation reports
- Migration logs

### How to Update Documentation

**Step 1**: Find the right file

- Use `docs/INDEX.md` to locate category
- Check file frontmatter to verify it's the current version

**Step 2**: Edit with proper frontmatter

```markdown
---
title: "Your Document Title"
version: "1.1.0"  # Increment version (see semver rules below)
created: "2025-01-15"
last-updated: "2025-11-10"  # Update this date
status: "current"
tags: ["relevant", "tags"]
related:
  - docs/related/file.md  # Add cross-references
---

# Your Document Title

[Your content here]
```

**Step 3**: Follow semver for version updates

- **PATCH** (1.0.0 → 1.0.1): Typo fixes, clarifications, no semantic changes
- **MINOR** (1.0.0 → 1.1.0): New sections added, examples expanded, backward compatible
- **MAJOR** (1.0.0 → 2.0.0): Breaking changes, pattern completely redesigned

**Step 4**: Run validation

```bash
# Validate your changes
python scripts/validate-frontmatter.py docs/
find docs -name "*.md" -exec markdown-link-check {} \;

# Generate cross-references
python scripts/generate-cross-references.py docs/
```

**Step 5**: Commit with good message

```bash
git add docs/patterns/your-pattern.md
git commit -m "docs(patterns): Update profile management pattern to v1.2.0

- Added section on concurrent access handling
- Included new example for multi-threaded scenarios
- Fixed broken link to internal-method pattern

Refs: #123"
```

## Validation Scripts

### Quick Reference

```bash
# Check frontmatter completeness
python scripts/validate-frontmatter.py docs/

# Validate all links
find docs -name "*.md" -exec markdown-link-check {} \;

# Find orphaned files
python scripts/detect-orphans.py docs/ --report

# Detect duplicate content
python scripts/detect-duplicates.py docs/ --threshold=80

# Generate cross-references
python scripts/generate-cross-references.py docs/

# Full validation suite (run before commit)
./scripts/validate-all.sh
```

### CI/CD Integration

The validation scripts run automatically on every pull request:

- ✅ **Frontmatter validation**: Must pass (blocking)
- ✅ **Link validation**: Must pass (blocking)
- ⚠️ **Orphan detection**: Warning only (non-blocking)
- ⚠️ **Duplicate detection**: Warning only (non-blocking)

## Migration Notes

### Old Documentation Locations (Deprecated)

If you have old bookmarks, here's where files moved:

| Old Path | New Path | Stub Removal Date |
|----------|----------|-------------------|
| `.copilot-tracking/memory-bank/systemPatterns.md` | `docs/patterns/system-patterns.md` | 2026-02-15 |
| `AGENTS.md` | `docs/guides/ai-agent-guide.md` | 2026-02-15 |
| `PATTERNS_REFERENCE.md` | `docs/patterns/_index.md` | 2026-02-15 |
| `reviews/LATEST_REVIEW.md` | `docs/reviews/LATEST.md` | 2026-02-15 |

**Transition Period**: Old locations have redirect stubs until removal date (3 months).

**Update Your Workflows**:

```bash
# Old (deprecated)
cat .copilot-tracking/memory-bank/systemPatterns.md

# New (current)
cat docs/patterns/system-patterns.md
```

## Directory Structure Reference

```
docs/
├── INDEX.md                    # START HERE - Master index
├── architecture/               # System architecture & ADRs
│   ├── overview.md
│   ├── clean-architecture.md
│   └── decisions/             # Architecture Decision Records
├── patterns/                   # Implementation patterns
│   ├── _index.md              # Pattern catalog
│   ├── profile-management.md
│   └── examples/              # Code examples
├── guides/                     # How-to guides & workflows
│   ├── onboarding.md
│   ├── development-workflow.md
│   └── migration/             # Migration guides
├── templates/                  # Code templates
│   ├── viewmodel-template.cs
│   └── ui-integration/
├── reviews/                    # Code reviews
│   ├── LATEST.md              # Current quality baseline
│   └── archive/               # Historical reviews
└── archive/                    # Deprecated docs (2-year retention)
```

## Getting Help

**Documentation Issues**:

- Broken link? Run `markdown-link-check` and file an issue
- Missing documentation? Check `docs/INDEX.md` to confirm, then create PR
- Unclear pattern? Comment in related GitHub issue or PR

**Questions**:

- Architecture questions → `docs/architecture/overview.md` first
- Pattern questions → `docs/patterns/_index.md` to find pattern, then read detailed docs
- Workflow questions → `docs/guides/development-workflow.md`

## Best Practices

### For AI Agents

1. **Always start with INDEX.md** - Don't search blindly
2. **Read patterns before implementing** - Don't reinvent existing solutions
3. **Check related documentation** - Follow cross-reference links
4. **Verify against latest review** - Check `docs/reviews/LATEST.md` for quality baseline

### For Human Developers

1. **Update docs in same PR as code** - Keep documentation synchronized
2. **Link code reviews to patterns** - Create bidirectional references
3. **Version documentation properly** - Use semver for clarity
4. **Run validation before commit** - Catch issues early

### For Both

1. **Use relative links** - Links remain portable across branches
2. **Tag generously** - Improves searchability and categorization
3. **Prefer depth over breadth** - Detailed patterns better than many shallow docs
4. **Keep it simple** - YAGNI applies to documentation too

## Next Steps

**New to the project?**

→ Read `docs/guides/onboarding.md` for complete onboarding workflow

**Need to implement a feature?**

→ Read `docs/guides/development-workflow.md` for step-by-step process

**Want to understand architecture?**

→ Read `docs/architecture/overview.md` and follow related links

**Looking for a pattern?**

→ Browse `docs/patterns/_index.md` and search by tags
