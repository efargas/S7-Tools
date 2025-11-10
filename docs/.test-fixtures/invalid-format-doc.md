---
title: "Invalid Format Test Document"
version: "1.0"
created: "2025/11/10"
last-updated: "2025-11-10"
status: "published"
tags: []
---

# Invalid Format Test Document

This document has INVALID frontmatter:
- version: "1.0" (should be X.Y.Z format like "1.0.0")
- created: "2025/11/10" (should be YYYY-MM-DD format)
- status: "published" (should be one of: current, deprecated, draft)
- tags: [] (should be non-empty array)

It should FAIL multiple validation rules.
