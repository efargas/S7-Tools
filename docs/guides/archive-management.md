# Archive Management Workflow

**Version**: 1.0.0
**Last Updated**: 2025-11-10
**Status**: current

## Overview

This document describes the complete workflow for managing deprecated documentation in S7Tools, including archiving, retention tracking, and automated cleanup.

## Deprecation Policy

### 2-Year Retention Rule

All deprecated documents follow a **2-year retention policy**:

1. **Deprecation Date**: Document marked as deprecated and moved to `docs/archive/`
2. **Retention Period**: 730 days (2 years) from deprecation date
3. **Removal Date**: Document and redirect stub permanently removed after retention period
4. **Grace Period**: None - removal is automatic on removal-date

### Required Frontmatter

**Archived Documents** (`status: deprecated`):
```yaml
---
title: "Document Title (Deprecated)"
status: "deprecated"
deprecated-date: "2025-11-10"
superseded-by: "docs/new-document.md"
removal-date: "2027-11-10"  # Auto-calculated: deprecated-date + 730 days
tags: ["deprecated", "category"]
---
```

**Redirect Stubs** (`status: redirect`):
```yaml
---
title: "REDIRECT: Document Title"
status: "redirect"
deprecated-date: "2025-11-10"
superseded-by: "docs/new-document.md"
removal-date: "2027-11-10"
tags: ["redirect", "deprecated"]
---
```

## Archiving Workflow

### Manual Archive Process

Use the automated archiver script:

```bash
# Dry run (preview changes)
python scripts/auto-archive.py docs/old-document.md \
  --superseded-by docs/new-document.md \
  --reason "Content consolidated into updated architecture docs" \
  --dry-run

# Execute archiving
python scripts/auto-archive.py docs/old-document.md \
  --superseded-by docs/new-document.md \
  --reason "Content consolidated into updated architecture docs"

# Update cross-references
python scripts/generate-cross-references.py docs/

# Update inventory
python scripts/archive-inventory.py docs/

# Commit changes
git add docs/
git commit -m "Archive old-document.md (superseded by new-document.md)"
```

### What the Archiver Does

1. **Reads original document**: Extracts frontmatter and content
2. **Updates frontmatter**: Sets `status: deprecated`, adds dates, tags
3. **Moves to archive/**: Preserves original path structure under `docs/archive/`
4. **Adds deprecation notice**: Inserts warning banner at top of archived content
5. **Creates redirect stub**: Replaces original with redirect pointing to:
   - Archived location (`archive/...`)
   - Superseding document (`docs/new-document.md`)
6. **Sets removal date**: Auto-calculates `deprecated-date + 730 days`

### After Archiving

1. Run cross-reference generator to update bidirectional links
2. Run archive inventory to update removal calendar
3. Commit changes with descriptive message
4. Update Memory Bank if architectural patterns changed

## Validation & Monitoring

### Daily/Weekly Checks

```bash
# Validate archive structure
python scripts/validate-archive.py docs/

# Check inventory and removal dates
python scripts/archive-inventory.py docs/

# Detect orphaned documents
python scripts/detect-orphans.py docs/
```

### Validation Tools

| Tool | Purpose | Frequency |
|------|---------|-----------|
| `validate-archive.py` | Verify frontmatter, retention policy compliance | Before commit |
| `archive-inventory.py` | Generate removal calendar, track days remaining | Weekly |
| `validate-frontmatter.py` | Check all documentation frontmatter schema | Before commit |
| `detect-orphans.py` | Find documents with no incoming links | Monthly |

## Retention Tracking

### Removal Calendar

The archive inventory (`docs/.metadata/archive-inventory.json`) tracks:

- **Total archived documents**: Current count
- **Pending removal**: Documents with future removal dates
- **Days until next removal**: Countdown to earliest removal-date
- **Overdue removals**: Documents past their removal-date

Example output:

```
Statistics:
  Total Archived Documents: 3
  Pending Removal: 3
  Next Removal In: 729 days

Archived Documents:
📄 Document Title
   Deprecated: 2025-11-10
   Removal: 2027-11-10 (729 days / 1 years remaining)
   Superseded By: docs/new-doc.md
```

### Expiration Notifications

**GitHub Actions** (future enhancement):
- Weekly check for documents with `removal-date` within 30 days
- Create GitHub issue with removal checklist
- Auto-assign to documentation maintainer

**Manual Check**:
```bash
python scripts/archive-inventory.py docs/ | grep "days remaining" | sort -n
```

## Removal Process

### When Removal Date Arrives

1. **Verify superseding document exists**:
   ```bash
   # Check that new doc is still valid
   python scripts/validate-frontmatter.py docs/
   ```

2. **Remove archived document**:
   ```bash
   rm docs/archive/old-document.md
   ```

3. **Remove redirect stub**:
   ```bash
   rm docs/old-document.md
   ```

4. **Update cross-references**:
   ```bash
   python scripts/generate-cross-references.py docs/
   ```

5. **Update inventory**:
   ```bash
   python scripts/archive-inventory.py docs/
   ```

6. **Commit removal**:
   ```bash
   git add docs/
   git commit -m "Remove old-document.md (2-year retention expired)"
   ```

### Automated Removal (Future)

Create scheduled GitHub Action:
```yaml
name: Archive Cleanup
on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday
jobs:
  cleanup:
    runs-on: ubuntu-latest
    steps:
      - name: Check for expired documents
        run: python scripts/check-expired.py --auto-remove
```

## Statistics & Reporting

### Archive Metrics

Track these metrics monthly:

- **Total archived documents**: Growth over time
- **Average retention period used**: Actual days before removal
- **Redirect stub usage**: How often old URLs are accessed (if tracking added)
- **Archive growth rate**: New deprecations per month

### Inventory Report

Generate comprehensive report:

```bash
# JSON format (for automation)
cat docs/.metadata/archive-inventory.json

# Human-readable format
python scripts/archive-inventory.py docs/
```

## Migration Guidance

All redirect stubs include migration path:

```markdown
## Migration Path

If you were using this document:

1. Read the new documentation - See [new-doc.md](new-doc.md)
2. Check migration guide - See [deprecated-patterns.md](guides/migration/deprecated-patterns.md)
3. Update references - Update any bookmarks or links

For questions, file an issue in the repository.
```

## Best Practices

### When to Archive

Archive a document when:
- ✅ Content is **fully superseded** by new documentation
- ✅ Superseding document is **complete and validated**
- ✅ New document **covers all use cases** from old document
- ✅ Cross-references are **updated** to point to new location

**Do NOT archive** when:
- ❌ New documentation is incomplete
- ❌ No clear superseding document exists
- ❌ Content is still actively referenced
- ❌ Migration path is unclear

### Superseding References

Always provide **specific** superseding references:

- ✅ `docs/architecture/overview.md#clean-architecture`
- ✅ `docs/patterns/profile-management.md`
- ❌ `docs/` (too vague)
- ❌ `README.md` (not specific enough)

### Reason for Deprecation

Provide clear, actionable reasons:

- ✅ "Content consolidated into docs/architecture/overview.md with updated examples"
- ✅ "Pattern superseded by StandardProfileManager<T> in profile-management.md"
- ❌ "Outdated" (not helpful)
- ❌ "See new docs" (not specific)

## Troubleshooting

### Validation Errors

**Error**: "Removal date does not match 2-year retention policy"
- **Fix**: Use auto-archive.py script (calculates automatically)
- **Manual**: `removal-date = deprecated-date + 730 days`

**Error**: "Superseding document does not exist"
- **Fix**: Create superseding document first, then archive
- **Or**: Update `superseded-by` field to correct path

**Error**: "Missing required field in frontmatter"
- **Fix**: Run `python scripts/validate-frontmatter.py` to identify missing fields
- **Reference**: See SETTINGS_SCHEMA.md for required fields

### Inventory Issues

**Issue**: "Days until removal shows negative number"
- **Meaning**: Document is overdue for removal
- **Action**: Follow removal process immediately

**Issue**: "Next removal in X days" but expected sooner
- **Check**: Run `python scripts/archive-inventory.py docs/ | grep "days remaining"`
- **Sort**: Find earliest removal date manually

## Related Documentation

- [Settings Schema](SETTINGS_SCHEMA.md) - Frontmatter field definitions
- [Version Control Integration](../adr/ADR-0003-version-control-integration.md) - Git workflow
- [Cross-Reference Network](../patterns/cross-reference-network.md) - Link management

## Appendix: Script Reference

### validate-archive.py

**Purpose**: Validate archive structure compliance
**Usage**: `python scripts/validate-archive.py docs/`
**Checks**:
- Frontmatter required fields
- 2-year retention policy
- Superseding document exists
- Redirect stub validity

### archive-inventory.py

**Purpose**: Generate removal calendar and statistics
**Usage**: `python scripts/archive-inventory.py docs/`
**Outputs**:
- Human-readable report (stdout)
- JSON inventory (`docs/.metadata/archive-inventory.json`)

### auto-archive.py

**Purpose**: Automatically archive deprecated documents
**Usage**: `python scripts/auto-archive.py docs/file.md --superseded-by docs/new.md [--reason "..."] [--dry-run]`
**Actions**:
- Moves document to archive/
- Creates redirect stub
- Calculates removal date
- Updates frontmatter

---

**Revision History**:
- 2025-11-10: Initial version (User Story 5)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
