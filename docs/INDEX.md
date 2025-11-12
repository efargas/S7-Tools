---
title: "S7Tools Documentation Index"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["index", "navigation", "documentation"]
---

# S7Tools Documentation Index

Welcome to the S7Tools Documentation System. This is the master index for all S7Tools documentation. Use this page as your entry point for rapid navigation to any architectural pattern, coding standard, or development guide.

## 🚀 Quick Start (30-Second Navigation)

### For AI Coding Agents

1. **Start Here**: Read [Architecture Overview](architecture/overview.md) (10 seconds)
2. **Browse Patterns**: Scan [Pattern Catalog](patterns/_index.md) (10 seconds)
3. **Locate Pattern**: Find relevant pattern for your task (5 seconds)
4. **Check Example**: Review [Pattern Examples](patterns/examples/) (5 seconds)

**Total Time**: < 30 seconds to locate any pattern or standard ✅

### For Human Developers

1. **New to Project?** → [Developer Onboarding Guide](guides/onboarding.md)
2. **Implementing Feature?** → [Architecture Overview](architecture/overview.md) → [Pattern Catalog](patterns/_index.md)
3. **Need Template?** → [Code Templates](templates/)
4. **Fixing Bug?** → [Latest Code Review](reviews/LATEST.md) → Related patterns
5. **Daily Workflow?** → [Development Workflow Guide](guides/development-workflow.md)

## 📚 Documentation Categories

### 🏛️ Architecture

**Purpose**: System design, architectural decisions, and structural patterns

**When to use**: Understanding system boundaries, layer responsibilities, or making architectural changes

| Document | Description |
|----------|-------------|
| [Overview](architecture/overview.md) | System architecture summary (Clean Architecture + MVVM) |
| [Clean Architecture](architecture/clean-architecture.md) | Layer boundaries and dependency rules |
| [MVVM Patterns](architecture/mvvm-patterns.md) | ReactiveUI and ViewModel patterns |
| [Dependency Injection](architecture/dependency-injection.md) | Service registration and DI patterns |
| [Architecture Decisions](architecture/decisions/) | ADRs documenting key decisions |

**Quick Links**:

- [ADR Index](architecture/decisions/_index.md)
- [Architecture Diagrams](architecture/diagrams.md)

### 🔧 Patterns

**Purpose**: Implementation patterns, best practices, and anti-patterns

**When to use**: Implementing features, solving common problems, or ensuring consistency

| Pattern Category | Key Patterns |
|------------------|--------------|
| **Profile Management** | [Unified Profile Pattern](patterns/profile-management.md) |
| **Thread Safety** | [Internal Method Pattern](patterns/internal-method.md) |
| **Service Coordination** | [Resource Coordinator](patterns/resource-coordination.md) |
| **Error Handling** | [Custom Exceptions](patterns/custom-exceptions.md) |
| **UI Components** | [Reusable Controls](patterns/reusable-controls.md) |

**Quick Links**:

- [Pattern Catalog](patterns/_index.md) - Browse all patterns with tags
- [Pattern Examples](patterns/examples/) - Code examples for each pattern
- [System Patterns Reference](patterns/system-patterns.md) - Comprehensive pattern guide

### 📖 Guides

**Purpose**: Step-by-step workflows, how-to guides, and development processes

**When to use**: Learning workflows, performing tasks, or onboarding

| Guide Type | Documents |
|------------|-----------|
| **Onboarding** | [Developer Onboarding](guides/onboarding.md), [AI Agent Guide](guides/ai-agent-guide.md) |
| **Development** | [Development Workflow](guides/development-workflow.md), [Code Style](guides/code-style.md) |
| **Testing** | [Testing Guide](guides/testing-guide.md) |
| **Documentation** | [Memory Bank Usage](guides/memory-bank-usage.md) |
| **Migration** | [Deprecated Patterns](guides/migration/deprecated-patterns.md) |

**Quick Links**:

- [AI Agent Guide](guides/ai-agent-guide.md) - For coding agents
- [Development Workflow](guides/development-workflow.md) - Daily workflow

### 📝 Templates

**Purpose**: Code templates, boilerplate, and scaffolding

**When to use**: Creating new files, following established patterns

| Template Type | Files |
|---------------|-------|
| **Code** | [ViewModel Template](templates/viewmodel-template.md), [Service Template](templates/service-template.md) |
| **Testing** | [Test Template](templates/test-template.md) |
| **Documentation** | [ADR Template](templates/adr-template.md), [Pattern Template](templates/pattern-template.md) |
| **UI Integration** | [UI Integration Templates](templates/ui-integration/) |

### 🔍 Reviews

**Purpose**: Code review findings, quality baselines, and improvement tracking

**When to use**: Checking quality standards, understanding current state

| Document | Description |
|----------|-------------|
| [Latest Review](reviews/LATEST.md) | Current quality baseline (symlink) |
| [Recent Reviews](reviews/) | All reviews chronologically |
| [Review Archive](reviews/archive/) | Historical reviews (>6 months) |

**Current Baseline**: 361 tests (360 passing, 1 skipped) = 99.7% pass rate | A+ grade (98/100)

### 📦 Archive

**Purpose**: Deprecated documentation with 2-year retention

**When to use**: Understanding deprecated approaches, migration context

- [Archive Index](archive/_index.md) - List of deprecated content with reasons

## 🔎 Search Strategies

### By Topic (grep)

```bash
# Find all documentation about a specific topic
grep -r "profile management" docs/

# Find by tag
grep -r "tags: \[.*mvvm.*\]" docs/

# Find patterns
ls docs/patterns/*.md
```

### By Category

```bash
# List all architecture docs
ls docs/architecture/

# List all patterns
ls docs/patterns/

# List all guides
ls docs/guides/
```

### By Status

```bash
# Find current (active) documentation
grep -r "status: \"current\"" docs/

# Find deprecated content
grep -r "status: \"deprecated\"" docs/
```

## 🎯 Common Tasks

### Task: Implement New Feature

1. Read [Architecture Overview](architecture/overview.md) - Understand layers
2. Check [Pattern Catalog](patterns/_index.md) - Find relevant patterns
3. Copy [ViewModel Template](templates/viewmodel-template.md) - Start with boilerplate
4. Follow [Testing Guide](guides/testing-guide.md) - Write tests first
5. Review [Latest Code Review](reviews/LATEST.md) - Check quality standards

### Task: Fix Bug

1. Understand current behavior - Read relevant pattern docs
2. Check [Code Review History](reviews/) - Known issues in that area
3. Verify fix doesn't break architectural rules
4. Update pattern docs if bug revealed misunderstanding

### Task: Add New Pattern

1. Copy [Pattern Template](templates/pattern-template.md)
2. Document problem, solution, examples
3. Add frontmatter with proper tags
4. Link from [Pattern Catalog](patterns/_index.md)
5. Create example in [patterns/examples/](patterns/examples/)

### Task: Onboard to Project

1. Read [Developer Onboarding](guides/onboarding.md) - Complete guide
2. Read [Architecture Overview](architecture/overview.md) - System understanding
3. Skim [Pattern Catalog](patterns/_index.md) - Available patterns
4. Read [Development Workflow](guides/development-workflow.md) - Daily process
5. Build project and run tests

## 🤖 AI Agent Optimization

### Context Gathering Priority (Fastest First)

1. **Architecture** → `docs/architecture/overview.md` (system structure)
2. **Patterns** → `docs/patterns/_index.md` (pattern catalog)
3. **Specific Pattern** → `docs/patterns/<pattern-name>.md` (detailed implementation)
4. **Examples** → `docs/patterns/examples/<pattern>-example.cs` (code samples)
5. **Templates** → `docs/templates/<type>-template.cs` (boilerplate)

### Key Files for Every Session

| File | Purpose | When to Read |
|------|---------|--------------|
| `docs/INDEX.md` | Navigation hub | **Every session** (start here) |
| `docs/architecture/overview.md` | System architecture | **Every session** (second step) |
| `docs/patterns/_index.md` | Pattern catalog | When implementing features |
| `docs/guides/development-workflow.md` | Development process | Before making changes |
| `docs/reviews/LATEST.md` | Quality baseline | Before submitting work |

### Search Optimization

```python
# Efficient AI agent context gathering
def gather_context(task_description):
    # Phase 1: Always read (foundation)
    read("docs/INDEX.md")
    read("docs/architecture/overview.md")

    # Phase 2: Pattern selection (targeted)
    patterns_index = read("docs/patterns/_index.md")
    relevant_patterns = filter_by_tags(patterns_index, task_description)
    for pattern in relevant_patterns[:3]:  # Top 3
        read(f"docs/patterns/{pattern}")

    # Phase 3: Examples (verification)
    for pattern in relevant_patterns[:3]:
        read(f"docs/patterns/examples/{pattern}-example.cs")

    return context
```

## 📊 Documentation Statistics

- **Total Categories**: 6 (architecture, patterns, guides, templates, reviews, archive)
- **Max Directory Depth**: 3 levels
- **Link Validation**: Automated on every commit
- **Metadata Standard**: YAML frontmatter with 8 required fields
- **Versioning**: Semantic versioning (MAJOR.MINOR.PATCH)

## 🔗 Related Resources

- [Project README](../README.md) - Project overview and setup
- [Contribution Guidelines](guides/contributing-to-docs.md) - How to update docs
- [Architecture Diagrams](architecture/diagrams.md) - Visual representations
- [Latest Code Review](reviews/LATEST.md) - Quality baseline

## 🛠️ Documentation Maintenance

### Updating Documentation

1. Find the right file using this index
2. Edit with proper [frontmatter schema](guides/frontmatter-schema.md)
3. Increment version following [semver rules](guides/versioning-guide.md)
4. Run validation: `./scripts/validate-all.sh docs/`
5. Commit with descriptive message

### Validation Scripts

```bash
# Validate frontmatter
python scripts/validate-frontmatter.py docs/

# Check for duplicates
python scripts/detect-duplicates.py docs/

# Find orphans
python scripts/detect-orphans.py docs/

# Full validation suite
./scripts/validate-all.sh docs/
```

See [scripts/README.md](../scripts/README.md) for detailed usage.

## 📞 Getting Help

**Documentation Issues**:

- Broken link? Run `markdown-link-check` and file an issue
- Missing documentation? Check this index, then create PR
- Unclear pattern? Comment on related GitHub issue

**Questions**:

- Architecture → Read [Overview](architecture/overview.md) first
- Patterns → Browse [Catalog](patterns/_index.md)
- Workflow → Check [Development Guide](guides/development-workflow.md)

---

**Last Updated**: 2025-11-10 | **Version**: 1.0.0 | **Status**: Current

**Navigation Tips**:

- Use Ctrl+F to search this page
- Click category headings to jump to sections
- Follow "Quick Links" for common destinations
- All paths are relative to repository root

## Related Documentation

- [_Index](architecture/_index.md)
- [Clean Architecture](architecture/clean-architecture.md)
- [_Index](architecture/decisions/_index.md)
- [Diagrams](architecture/diagrams.md)
- [Mvvm Patterns](architecture/mvvm-patterns.md)
- [Overview](architecture/overview.md)
- [Ai Agent Guide](guides/ai-agent-guide.md)
- [Code Style](guides/code-style.md)
- [Development Workflow](guides/development-workflow.md)
- [Memory Bank Usage](guides/memory-bank-usage.md)
- [Deprecated Patterns](guides/migration/deprecated-patterns.md)
- [Onboarding](guides/onboarding.md)
- [Testing Guide](guides/testing-guide.md)
- [Versioning Guide](guides/versioning-guide.md)
- [_Index](patterns/_index.md)
- [Custom Exceptions](patterns/custom-exceptions.md)
- [Internal Method](patterns/internal-method.md)
- [Profile Management](patterns/profile-management.md)
- [Resource Coordination](patterns/resource-coordination.md)
- [Reusable Controls](patterns/reusable-controls.md)
- [System Patterns](patterns/system-patterns.md)
- [2025 11 10 Quality Improvements](reviews/2025-11-10-quality-improvements.md)
- [Latest](reviews/LATEST.md)
- [_Index](reviews/_index.md)
- [Adr Template](templates/adr-template.md)
- [Pattern Template](templates/pattern-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
