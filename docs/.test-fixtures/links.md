---
title: "Links Test"
version: "1.0.0"
created: "2026-01-01"
last-updated: "2026-03-24"
status: "current"
tags: ["test", "links"]
---

# Links Test

This document exercises link-extraction edge cases for the link validator.

## Valid Internal Links

- Sibling: [valid-document](valid-document.md)
- Another sibling: [code-blocks](code-blocks.md)

## Anchor-Only Links (must not trigger broken-link errors)

- [Jump to section](#valid-internal-links)
- [Another anchor](#code-snippets-in-links)

## External Links (must be ignored by the internal-link validator)

- [GitHub](https://github.com/efargas/S7-Tools)
- [Avalonia UI](https://avaloniaui.net)
- [Microsoft Docs](https://docs.microsoft.com)

## Links with Anchors (file must exist, anchor is informational)

- [valid doc section](valid-document.md#file-references)

## Code Snippets in Links

Links written **inside** backticks must not be treated as real links:

The text `` [not a link](no-such-file.md) `` is inline code, not a link.

And inside a fenced block they must also be ignored:

```markdown
[fake link inside fence](totally-imaginary.md)
```

## Link with URL-Encoded Characters

- [url encoded](valid-document.md)

## mailto and Protocol Links (must be ignored)

- [Send email](mailto:user@example.com)
- [FTP resource](ftp://example.com/resource)
