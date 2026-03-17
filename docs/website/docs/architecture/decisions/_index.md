---
title: "Architectural Decision Records (ADR) Index"
version: "1.0.0"
created: "2025-01-15"
last-updated: "2025-11-10"
status: "current"
tags: ["architecture", "adr", "decisions", "index"]
related:
  - docs/architecture/_index.md
  - docs/architecture/overview.md
---

# Architectural Decision Records (ADR)

This index lists the key architectural decisions for S7Tools. New decisions should be added as separate files and linked here.

- [ADR-0001: UI Framework — Avalonia + ReactiveUI](0001-ui-framework.md)
- [ADR-0002: Logging — In-memory DataStore Provider](0002-logging-provider.md)

## How to add an ADR

1. Copy the template at `_template.md` to a new file named `ADR-XXXX-short-title.md` (increment number).
2. Fill in Status, Date, Context, Decision, Consequences, and References.
3. Add a link to the new ADR in the list above, keeping the list sorted by ID.
4. If an ADR supersedes another, mark the older one as “Superseded by ADR-XXXX”.

## Related Documentation

- [Index](../../INDEX.md)
- [_Index](../_index.md)
- [0001 Ui Framework](0001-ui-framework.md)
- [0002 Logging Provider](0002-logging-provider.md)
- [_Template](_template.md)
- [Overview](../overview.md)
- [Contributing To Docs](../../guides/contributing-to-docs.md)
- [_Index](../../templates/_index.md)
- [Adr Template](../../templates/adr-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
