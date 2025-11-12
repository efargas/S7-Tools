---
title: "Guide Documentation Template"
type: "template"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-11"
status: "current"
tags: ["template", "guide"]
related:
  - docs/guides/contributing-to-docs.md
  - docs/templates/pattern-template.md
---

# Guide Title

Brief description of what this guide covers and who it's for.

## Overview

Provide context: what will the reader learn? What are the prerequisites?

## Prerequisites

- Prerequisite 1
- Prerequisite 2
- Prerequisite 3

## Quick Start (5-10 Minutes)

Fastest path to get started:

```bash
# Essential commands
command1
command2
```

## Concepts

### Concept 1

Explain the first key concept.

### Concept 2

Explain the second key concept.

## Step-by-Step Instructions

### Task 1: First Task

1. **Step 1**: Description
   ```bash
   # Command example
   ```

2. **Step 2**: Description
   ```bash
   # Command example
   ```

3. **Verify**: How to verify this step worked
   ```bash
   # Verification command
   ```

### Task 2: Second Task

[Same structure as Task 1]

## Common Workflows

### Workflow 1: Daily Operations

```bash
# Typical daily commands
step1
step2
step3
```

### Workflow 2: Troubleshooting

1. Check X
2. Verify Y
3. Fix Z

## Best Practices

✅ **DO**:
- Best practice 1
- Best practice 2

❌ **DON'T**:
- Anti-pattern 1
- Anti-pattern 2

## Examples

### Example 1: Basic Usage

```bash
# Complete working example
```

**Expected Output**:
```
output here
```

### Example 2: Advanced Usage

```bash
# More complex example
```

## Troubleshooting

### Problem 1: Error Message

**Symptom**: Description of the problem

**Cause**: Why this happens

**Solution**:
```bash
# Fix command
```

### Problem 2: Another Issue

[Same structure as Problem 1]

## FAQ

**Q: Common question?**
A: Answer with examples if needed.

**Q: Another question?**
A: Answer.

## Version History

### v1.0.0 (YYYY-MM-DD)

**MAJOR**: Initial release

**Sections**:
- Overview and prerequisites
- Step-by-step instructions
- Common workflows
- Troubleshooting guide

**Migration Notes**: N/A (initial release)

## Related Documentation

- [Guides Index](../guides/_index.md)
- [Related Guide 1](./related-guide.md)
- [Related Pattern](../patterns/related-pattern.md)

---

**Maintenance Notes**:
- Update `last-updated` field when making changes
- Increment `version` according to semantic versioning rules:
  - **PATCH** (x.y.Z): Typos, clarifications, updated commands
  - **MINOR** (x.Y.0): New sections, new workflows, backward compatible
  - **MAJOR** (X.0.0): Breaking changes, workflow redesign
- Add entry to Version History section for each version bump
- Test all command examples before publishing
- Keep screenshots up-to-date if included
