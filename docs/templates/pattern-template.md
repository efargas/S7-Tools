---
title: "Pattern Documentation Template"
version: "1.0.0"
created: "YYYY-MM-DD"
last-updated: "YYYY-MM-DD"
status: "current"
tags: ["template", "pattern"]
related:
  - docs/patterns/_index.md
---

# Pattern Name

Brief description of what this pattern solves and when to use it.

## Problem

What problem does this pattern address? What challenges does it solve?

## Solution

How does this pattern solve the problem? What is the core approach?

## Implementation

### Core Components

List the main classes, interfaces, or components involved.

### Code Example

```csharp
// Minimal working example of the pattern
public class ExampleImplementation
{
    // Implementation details
}
```

### Step-by-Step Guide

1. **Step 1**: First action
2. **Step 2**: Second action
3. **Step 3**: Third action

## Benefits

- ✅ Benefit 1
- ✅ Benefit 2
- ✅ Benefit 3

## Trade-offs

- ⚠️ Trade-off 1
- ⚠️ Trade-off 2

## When to Use

Use this pattern when:
- Condition 1
- Condition 2

## When NOT to Use

Avoid this pattern when:
- Condition 1
- Condition 2

## Examples

### Real-World Usage

Link to actual usage in codebase:
- `src/Path/To/RealExample.cs`

### Complete Example

```csharp
// Full working example with context
```

## Anti-Patterns

❌ **Anti-Pattern Name**: Description of what to avoid

```csharp
// Bad approach
```

## Testing

How to test implementations of this pattern:

```csharp
[Fact]
public async Task TestExample()
{
    // Arrange
    // Act
    // Assert
}
```

## Version History

### v1.0.0 (YYYY-MM-DD)

**MAJOR**: Initial release

**Features**:
- Initial pattern documentation
- Core examples
- Usage guidelines

**Migration Notes**: N/A (initial release)

## Related Documentation

- [Pattern Index](../patterns/_index.md)
- [Architecture Overview](../architecture/overview.md)

---

**Maintenance Notes**:
- Update `last-updated` field when making changes
- Increment `version` according to semantic versioning rules:
  - **PATCH** (x.y.Z): Typos, clarifications, no semantic change
  - **MINOR** (x.Y.0): New sections, expanded examples, backward compatible
  - **MAJOR** (X.0.0): Breaking changes, pattern redesign
- Add entry to Version History section for each version bump
- Keep examples synchronized with actual codebase
