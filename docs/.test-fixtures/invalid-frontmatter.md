---
title: "Invalid Frontmatter Test"
version: "not-semver"
created: "01/01/2026"
status: "unknown-status"
tags: []
---

# Invalid Frontmatter Test

This document deliberately contains frontmatter violations so the validator
can be tested against known-bad input.

## Expected Violations

| Rule | Field | Problem |
|------|-------|---------|
| META-002 | version | `not-semver` is not valid semver (expects `X.Y.Z`) |
| META-003 | created | `01/01/2026` is not `YYYY-MM-DD` format |
| META-001 | last-updated | Field is missing entirely |
| META-004 | status | `unknown-status` is not a recognised status value |
| META-005 | tags | Empty array is not allowed |
