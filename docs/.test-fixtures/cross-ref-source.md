---
title: "Cross-Reference Source Test"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - test-fixture
  - cross-reference
related:
  - docs/.test-fixtures/cross-ref-target.md
  - docs/patterns/profile-management.md
---

# Cross-Reference Source Test

This is a test fixture to validate bidirectional cross-reference generation.

## Purpose

This document explicitly references `cross-ref-target.md` in its frontmatter `related` field. The cross-reference generator should:

1. Detect this relationship
2. Add a reciprocal link in `cross-ref-target.md`'s "Related Documentation" section
3. Create bidirectional navigation

## Usage in Testing

**Test Case**: T111-T113 - Bidirectional Cross-Reference Generation

**Expected Outcome**:
- `cross-ref-target.md` gets a "Related Documentation" section
- That section contains a link back to this file
- Both files have complete bidirectional navigation

## Related Documentation

- [Cross Ref Target](cross-ref-target.md)
- [Profile Management](../patterns/profile-management.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
