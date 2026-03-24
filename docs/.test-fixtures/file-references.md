---
title: "File References Test"
version: "1.0.0"
created: "2026-01-01"
last-updated: "2026-03-24"
status: "current"
tags: ["test", "file-references"]
---

# File References Test

This document exercises file-reference extraction and resolution.

## Existing References (should all resolve)

Inline backtick paths that exist in the repository:

- `src/S7Tools/Services/Profiles/StandardProfileManager.cs`
- `src/S7Tools.Core/Interfaces/Services/IProfileManager.cs`
- `src/S7Tools.Core/Interfaces/Services/IProfileBase.cs`
- `src/S7Tools/Services/Socat/SocatService.cs`
- `src/S7Tools/Services/PowerSupply/PowerSupplyService.cs`
- `src/S7Tools/Services/Tasking/ResourceCoordinator.cs`

## Paths Inside Code Blocks (must NOT be extracted as file references)

The paths inside the fenced block below are hypothetical examples and should
not be counted as file references by the validator:

```text
src/Does/Not/Exist.cs
docs/imaginary-file.md
tests/Fake/FakeTest.cs
src/S7Tools/Hypothetical/MyService.cs
```

## Relative Markdown Links

Link to sibling: [valid document](valid-document.md)

## Path in Markdown Link Syntax

[StandardProfileManager](../../src/S7Tools/Services/Profiles/StandardProfileManager.cs)
