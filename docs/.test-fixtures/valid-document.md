---
title: "Valid Test Document"
version: "1.0.0"
created: "2026-01-01"
last-updated: "2026-03-24"
status: "current"
tags: ["test", "fixture", "validation"]
---

# Valid Test Document

This document has correct frontmatter and is used to verify that the
validation scripts correctly accept well-formed documentation.

## File References

The following file references exist in the repository and should all resolve:

- `src/S7Tools/Services/Profiles/StandardProfileManager.cs`
- `src/S7Tools.Core/Interfaces/Services/IProfileManager.cs`
- `src/S7Tools.Core/Interfaces/Services/IProfileBase.cs`
- `src/S7Tools/Services/Socat/SocatService.cs`
- `src/S7Tools/Services/PowerSupply/PowerSupplyService.cs`
- `src/S7Tools/Services/Tasking/ResourceCoordinator.cs`

## Code Snippets

C# code inside fenced blocks must **not** generate false-positive file references
or link targets.

```csharp
// Correct: StandardProfileManager implements the unified pattern
public class MyProfileService : StandardProfileManager<MyProfile>
{
    protected override MyProfile CreateDefaultProfile() =>
        new() { Name = "Default" };
}
```

Inline code like `src/S7Tools/Services/Profiles/StandardProfileManager.cs` is fine
because the validator intentionally resolves inline-backtick paths.

```csharp
// This is a simplified illustration
// ... simplified
public class SimplifiedExample
{
    // work omitted for brevity
}
```

## Internal Links

Link to a sibling fixture: [invalid-frontmatter](invalid-frontmatter.md)

## Escaped Characters

Markdown allows escape sequences: \*not bold\*, \[not a link\], \`not code\`.
These must not be mistaken for real syntax by the validators.
