---
title: S7Tools Documentation System
version: 1.0.3
created: '2025-11-10'
last-updated: '2026-03-17'
status: current
tags:
- documentation
- index
- navigation
- readme
related:
- docs/INDEX.md
- docs/guides/ai-agent-guide.md
- docs/guides/onboarding.md
---
# S7Tools Documentation

Welcome to the S7Tools consolidated documentation system! This directory contains all project documentation organized into clear, navigable categories.

## 🚀 Quick Start

**New Here?** Start with the [Master Documentation Index (INDEX.md)](INDEX.md)

**AI Coding Agent?** Follow the [30-second onboarding workflow](guides/ai-agent-guide.md)

**Human Developer?** Read the [Developer Onboarding Guide](guides/onboarding.md)

## 📂 Documentation Structure

```
docs/
├── INDEX.md                    # ⭐ START HERE - Master navigation index
├── architecture/               # System design and architectural decisions
│   ├── overview.md
│   ├── clean-architecture.md
│   ├── mvvm-patterns.md
│   └── decisions/             # Architecture Decision Records (ADRs)
├── patterns/                   # Implementation patterns and best practices
│   ├── _index.md              # Pattern catalog
│   ├── profile-management.md
│   ├── internal-method.md
│   └── examples/              # Code examples
├── guides/                     # Step-by-step guides and workflows
│   ├── onboarding.md
│   ├── ai-agent-guide.md
│   ├── development-workflow.md
│   └── migration/             # Migration guides
├── templates/                  # Code and documentation templates
│   ├── viewmodel-template.cs
│   ├── service-template.cs
│   └── ui-integration/
├── reviews/                    # Code reviews and quality reports
│   ├── LATEST.md              # Current quality baseline
│   └── archive/               # Historical reviews
└── archive/                    # Deprecated documentation (2-year retention)
```

## 🎯 Common Tasks

### Finding Information

| I Want To... | Go To... |
|--------------|----------|
| Understand system architecture | [architecture/overview.md](architecture/overview.md) |
| Find an implementation pattern | [patterns/_index.md](patterns/_index.md) |
| Learn the development workflow | [guides/development-workflow.md](guides/development-workflow.md) |
| Get a code template | [templates/](templates/) |
| Check current quality baseline | [reviews/LATEST.md](reviews/LATEST.md) |
| Search all documentation | [INDEX.md](INDEX.md) → Search Strategies |

### Updating Documentation

1. **Find the file**: Use [INDEX.md](INDEX.md) to navigate to the right category
2. **Edit with frontmatter**: All files require YAML frontmatter (see [guides/frontmatter-schema.md](guides/frontmatter-schema.md))
3. **Validate changes**: Run `./scripts/validate-all.sh` from repository root
4. **Commit**: Use descriptive commit messages

See [guides/contributing-to-docs.md](guides/contributing-to-docs.md) for detailed contribution guidelines.

## ✅ Quality Standards

All documentation in this directory follows these quality standards:

- **Metadata**: YAML frontmatter with version, dates, status, tags
- **Links**: All internal links validated automatically
- **Structure**: Maximum 3-level directory depth
- **Versioning**: Semantic versioning (MAJOR.MINOR.PATCH)
- **Cross-references**: Auto-generated bidirectional links

## 🤖 AI Agent Optimization

This documentation structure is optimized for AI coding agents:

- **30-second navigation**: Any pattern or standard accessible in < 30 seconds
- **Hierarchical organization**: Clear category structure for context gathering
- **Tagged content**: Easy filtering by topic, complexity, or technology
- **Examples included**: Every pattern has code examples
- **Entry point**: Single master index ([INDEX.md](INDEX.md))

See [guides/ai-agent-guide.md](guides/ai-agent-guide.md) for the complete AI agent onboarding workflow.

## 📊 Documentation Statistics

- **Categories**: 6 main categories (architecture, patterns, guides, templates, reviews, archive)
- **Max Depth**: 3 levels for navigability
- **Validation**: Automated link checking on every commit
- **Current Status**: 99.7% test pass rate (360/361 tests passing)

## 🔍 Search Tips

### By Command Line

```bash
# Search all documentation for a topic
grep -r "profile management" docs/

# Find by tag
grep -r "tags: \[.*mvvm.*\]" docs/

# List all patterns
ls docs/patterns/*.md

# Find current (non-deprecated) docs
grep -r "status: \"current\"" docs/
```

### By Category

- **Architecture**: `ls docs/architecture/`
- **Patterns**: `ls docs/patterns/`
- **Guides**: `ls docs/guides/`

## 🛠️ Maintenance Tools

### Validation Scripts

Located in `../scripts/`:

- `validate-frontmatter.py` - Check metadata completeness
- `detect-duplicates.py` - Find duplicate content
- `detect-orphans.py` - Find files with no incoming links
- `generate-cross-references.py` - Update "Related Documentation" sections
- `validate-all.sh` - Run all checks in one command

See [scripts/README.md](scripts/README.md) for detailed usage.

### CI/CD Integration

Documentation validation runs automatically on every PR:

- ✅ Frontmatter validation (blocking)
- ✅ Link validation (blocking)
- ⚠️ Orphan detection (warning)
- ⚠️ Duplicate detection (warning)

## 📞 Getting Help

**Documentation Issues?**

- Broken link → Run `markdown-link-check` and file an issue
- Missing content → Check [INDEX.md](INDEX.md), then create PR
- Unclear pattern → Comment on related GitHub issue

**Questions?**

- Architecture → [architecture/overview.md](architecture/overview.md)
- Patterns → [patterns/_index.md](patterns/_index.md)
- Workflow → [guides/development-workflow.md](guides/development-workflow.md)

## 🔗 Key Links

- **Master Index**: [INDEX.md](INDEX.md) - Start here!
- **Architecture Overview**: [architecture/overview.md](architecture/overview.md)
- **Pattern Catalog**: [patterns/_index.md](patterns/_index.md)
- **AI Agent Guide**: [guides/ai-agent-guide.md](guides/ai-agent-guide.md)
- **Developer Onboarding**: [guides/onboarding.md](guides/onboarding.md)
- **Latest Review**: [reviews/LATEST.md](reviews/LATEST.md)

---

**Documentation System Version**: 1.0.0

**Last Updated**: 2025-11-10

**Consolidated From**: `.copilot-tracking/`, root-level docs, `reviews/` (see [guides/migration/deprecated-patterns.md](guides/migration/deprecated-patterns.md) for migration details)

For questions about this documentation system, see [guides/memory-bank-usage.md](guides/memory-bank-usage.md).

## Related Documentation

- [Index](INDEX.md)
- [Overview](architecture/overview.md)
- [Ai Agent Guide](guides/ai-agent-guide.md)
- [Contributing To Docs](guides/contributing-to-docs.md)
- [Development Workflow](guides/development-workflow.md)
- [Frontmatter Schema](guides/frontmatter-schema.md)
- [Memory Bank Usage](guides/memory-bank-usage.md)
- [Deprecated Patterns](guides/migration/deprecated-patterns.md)
- [Onboarding](guides/onboarding.md)
- [_Index](patterns/_index.md)
- [2025 11 10 Quality Improvements](reviews/2025-11-10-quality-improvements.md)
- [Latest](reviews/LATEST.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
