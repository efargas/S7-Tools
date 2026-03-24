---
title: "Architecture Documentation - Category Index"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["architecture", "index", "navigation"]
related:
  - "docs/architecture/overview.md"
  - "docs/patterns/_index.md"
supersedes: []
---

# Architecture Documentation

## Overview

This directory contains comprehensive architectural documentation for S7Tools, covering system design, architectural decisions, and implementation patterns.

## Quick Navigation

### Start Here

**New to the project?** → [Architecture Overview](overview.md)

**Looking for specific pattern?** → [Clean Architecture](clean-architecture.md) or [MVVM Patterns](mvvm-patterns.md)

**Need visual reference?** → [Architecture Diagrams](diagrams.md)

**Understanding decisions?** → [Architecture Decision Records](decisions/_index.md)

## Documents in This Category

### Core Architecture

| Document | Purpose | Quick Description |
|----------|---------|-------------------|
| [Overview](overview.md) | Complete system architecture | Vision, architecture style, tech stack, layers, components, patterns, UX philosophy, and core workflows (650+ lines) |
| [Clean Architecture](clean-architecture.md) | Layer boundaries and dependencies | Clean Architecture principles, layer architecture, dependency flow rules, patterns, anti-patterns (700+ lines) |
| [MVVM Patterns](mvvm-patterns.md) | ReactiveUI and ViewModel patterns | MVVM architecture, ReactiveUI fundamentals, property monitoring, thread safety, best practices (850+ lines) |
| [Diagrams](diagrams.md) | Visual representations | System architecture diagrams, component relationships, data flows (Mermaid diagrams) |

### Specialized Topics

| Document | Purpose | Quick Description |
|----------|---------|-------------------|
| [Dependency Injection](dependency-injection.md) | DI patterns and service registration | Service registration, lifetime management, DI best practices |

### Architecture Decisions

| Document | Purpose | Quick Description |
|----------|---------|-------------------|
| [ADR Index](decisions/_index.md) | Architecture Decision Records | All ADRs documenting key architectural decisions |

## Document Status

| Document | Version | Last Updated | Status |
|----------|---------|--------------|--------|
| overview.md | 1.0.0 | 2025-11-10 | ✅ Current |
| clean-architecture.md | 1.0.0 | 2025-11-10 | ✅ Current |
| mvvm-patterns.md | 1.0.0 | 2025-11-10 | ✅ Current |
| diagrams.md | 1.1.0 | 2025-11-07 | ✅ Current |
| dependency-injection.md | - | - | 🚧 Planned |
| testing-architecture.md | - | - | 🚧 Planned |

## How to Use This Documentation

### For AI Coding Agents

**30-Second Navigation Strategy**:
1. Read [overview.md](overview.md) (10 seconds) - System understanding
2. Scan relevant section (10 seconds) - Find specific pattern
3. Jump to detailed guide (10 seconds) - Implementation details

**Context Gathering Priority**:
1. **Start**: [overview.md](overview.md) - System architecture
2. **Layers**: [clean-architecture.md](clean-architecture.md) - Dependency rules
3. **UI**: [mvvm-patterns.md](mvvm-patterns.md) - ViewModel patterns
4. **Visual**: [diagrams.md](diagrams.md) - Diagrams and visualizations

### For Human Developers

**By Task**:
- **New feature** → Overview → Clean Architecture → Relevant pattern guide
- **Bug fix** → Diagrams → Specific architecture document
- **Refactoring** → Clean Architecture → MVVM Patterns → ADRs
- **Architecture review** → Overview → ADRs → Diagrams

**By Experience Level**:
- **Junior**: Start with Overview, then Diagrams, then patterns
- **Mid**: Overview for context, jump to specific patterns
- **Senior**: ADRs for decisions, patterns for details

## Cross-References

### Related Documentation

- [Pattern Catalog](../patterns/_index.md) - Implementation patterns
- [System Patterns](../patterns/system-patterns.md) - Consolidated pattern guide
- [Master Index](../INDEX.md) - Complete documentation index

### Constitutional References

All architecture documentation aligns with the S7Tools Constitutional Framework:

- **Article II**: Clean Architecture Boundaries → [clean-architecture.md](clean-architecture.md)
- **Article III**: MVVM with ReactiveUI → [mvvm-patterns.md](mvvm-patterns.md)
- **Article IV**: Test-First Quality Gates → testing-architecture.md (planned)
- **Article V**: Thread Safety & Concurrency → [mvvm-patterns.md#thread-safety](mvvm-patterns.md#thread-safety-and-ui-updates)
- **Article VI**: Observability & Simplicity → [overview.md#logging-infrastructure](overview.md#logging-infrastructure)

## Search by Topic

### By Architecture Concern

| Concern | Primary Document | Related |
|---------|------------------|---------|
| Layer separation | [clean-architecture.md](clean-architecture.md) | overview.md, diagrams.md |
| Dependency rules | [clean-architecture.md](clean-architecture.md) | overview.md |
| MVVM implementation | [mvvm-patterns.md](mvvm-patterns.md) | overview.md, diagrams.md |
| ReactiveUI patterns | [mvvm-patterns.md](mvvm-patterns.md) | - |
| Service registration | dependency-injection.md (planned) | clean-architecture.md |
| Testing strategy | testing-architecture.md (planned) | - |

### By Technology

| Technology | Covered In |
|------------|------------|
| .NET 8 | overview.md |
| Avalonia UI | overview.md, mvvm-patterns.md |
| ReactiveUI | mvvm-patterns.md |
| Microsoft.Extensions.DI | clean-architecture.md, dependency-injection.md (planned) |
| xUnit | testing-architecture.md (planned) |

## Contributing to Architecture Docs

### Adding New Documents

1. Create document with proper frontmatter (see existing files)
2. Add entry to this index
3. Update cross-references in related documents
4. Run validation: `./scripts/validate-all.sh docs/architecture/`

### Updating Existing Documents

1. Increment version number (semantic versioning)
2. Update `last-updated` date
3. Update this index if title/purpose changes
4. Regenerate cross-references if needed

### Frontmatter Template

```yaml
---
title: "Document Title"
version: "1.0.0"
created: "YYYY-MM-DD"
last-updated: "YYYY-MM-DD"
status: "current"
tags: ["architecture", "relevant", "tags"]
related:
  - "docs/path/to/related1.md"
  - "docs/path/to/related2.md"
supersedes: []
---
```

## Maintenance

### Review Schedule

- **Monthly**: Check all documents for outdated information
- **Per Release**: Update version numbers and diagrams
- **Per Feature**: Add relevant architectural notes

### Quality Standards

All architecture documents must:
- [ ] Include complete frontmatter
- [ ] Have clear table of contents
- [ ] Include code examples where relevant
- [ ] Cross-reference related documents
- [ ] Follow markdown style guide
- [ ] Pass validation scripts

---

**Last Updated**: 2025-11-10
**Status**: Current architecture documentation index
**Document Count**: 4 current, 2 planned

## Related Documentation

- [Index](../INDEX.md)
- [Clean Architecture](clean-architecture.md)
- [_Index](decisions/_index.md)
- [Dependency Injection](dependency-injection.md)
- [Diagrams](diagrams.md)
- [Mvvm Patterns](mvvm-patterns.md)
- [Overview](overview.md)
- [_Index](../guides/_index.md)
- [_Index](../patterns/_index.md)
- [System Patterns](../patterns/system-patterns.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
