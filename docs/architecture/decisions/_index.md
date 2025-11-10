# Architectural Decision Records (ADR)

This index lists the key architectural decisions for S7Tools. New decisions should be added as separate files and linked here.

- [ADR-0001: UI Framework — Avalonia + ReactiveUI](ADR-0001-ui-framework-avalonia-reactiveui.md)
- [ADR-0002: Logging — In-memory DataStore Provider](ADR-0002-logging-inmemory-datastore-provider.md)

## How to add an ADR

1. Copy the template at `_template.md` to a new file named `ADR-XXXX-short-title.md` (increment number).
2. Fill in Status, Date, Context, Decision, Consequences, and References.
3. Add a link to the new ADR in the list above, keeping the list sorted by ID.
4. If an ADR supersedes another, mark the older one as “Superseded by ADR-XXXX”.
