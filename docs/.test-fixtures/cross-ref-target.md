---
title: "Cross-Reference Target Test"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - test-fixture
  - cross-reference
---

# Cross-Reference Target Test

This is a test fixture that should receive an automatic cross-reference from `cross-ref-source.md`.

## Purpose

This document does NOT explicitly reference `cross-ref-source.md` in its frontmatter. However, since `cross-ref-source.md` lists this file in its `related` field, the cross-reference generator should:

1. Detect the incoming reference from `cross-ref-source.md`
2. Auto-generate a "Related Documentation" section in this file
3. Add a reciprocal link back to `cross-ref-source.md`
4. Create complete bidirectional navigation

## Expected Behavior

After running the cross-reference generator, this file should have a "Related Documentation" section added below, containing a link to `cross-ref-source.md`.

## Test Validation

**Validation Steps**:

1. Verify "Related Documentation" section exists below this section
2. Verify link to `cross-ref-source.md` is present and valid
3. Verify bidirectional navigation works (can navigate from source → target → source)
