---
title: "Archive Index"
version: "1.0.0"
created: "2025-11-12"
last-updated: "2025-11-12"
status: "current"
tags: ["archive", "index", "deprecated"]
related:
  - docs/INDEX.md
  - docs/guides/archive-management.md
---

This directory contains deprecated documentation with a 2-year retention policy before permanent deletion.

## Archived Documentation

| File | Original Location | Archived Date | Reason | Delete After |
|------|-------------------|---------------|--------|--------------|
| [Project_Architecture_Blueprint.md](Project_Architecture_Blueprint.md) | docs/architecture/ | 2025-11-10 | Superseded by architecture/overview.md | 2027-11-10 |
| [Project_Folders_Structure_Blueprint.md](Project_Folders_Structure_Blueprint.md) | docs/ | 2025-11-10 | Consolidated into architecture docs | 2027-11-10 |
| [ATTRIBUTE_BASED_DISPLAY.md](ATTRIBUTE_BASED_DISPLAY.md) | docs/ | 2025-11-10 | Superseded by patterns/reusable-controls.md | 2027-11-10 |

## Archive Policy

### When to Archive

Documentation should be archived when:

- Superseded by newer, better-organized documentation
- Contains outdated patterns no longer used in codebase
- Maintained for historical reference only
- Migration guides exist for deprecated patterns

### Retention Period

- **2 years** from archive date
- After 2 years, files are reviewed for permanent deletion
- Critical historical context may be retained longer

### Archive Process

1. Add frontmatter `status: "deprecated"` to original file
2. Move file to `docs/archive/`
3. Add entry to this index with:
   - Original location
   - Archive date
   - Reason for archiving
   - Scheduled deletion date
4. Update cross-references in active documentation

### Accessing Archived Content

Archived documentation remains available for:

- Understanding historical design decisions
- Migration context for deprecated patterns
- Reference during code archaeology
- Compliance and audit requirements

## Related Documentation

- [Index](../INDEX.md)
- [Attribute_Based_Display](ATTRIBUTE_BASED_DISPLAY.md)
- [Project_Architecture_Blueprint](Project_Architecture_Blueprint.md)
- [Project_Folders_Structure_Blueprint](Project_Folders_Structure_Blueprint.md)
- [Archive Management](../guides/archive-management.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
