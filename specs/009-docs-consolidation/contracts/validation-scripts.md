# Validation Scripts API Contracts

**Feature**: Documentation Consolidation and Reorganization
**Date**: 2025-11-10
**Version**: 1.0.0

## Overview

This document defines the input/output contracts for all documentation validation scripts. These scripts enforce quality standards and automate documentation maintenance tasks.

## 1. Frontmatter Validator

### Purpose

Validates that all Markdown files have complete and correctly formatted YAML frontmatter.

### Script Name

`scripts/validate-frontmatter.py`

### Command Line Interface

```bash
python scripts/validate-frontmatter.py <docs_root> [--output=<format>] [--strict]
```

**Arguments**:

- `docs_root` (required): Path to documentation root directory (e.g., `docs/`)
- `--output` (optional): Output format (`text` [default], `json`, `junit`)
- `--strict` (optional): Fail on warnings in addition to errors

### Input

- **Directory tree**: Recursively scans all `.md` files in `docs_root`
- **Files to validate**: All `*.md` files except those in `.metadata/` directory

### Output

**Text Format** (default):

```
Validating frontmatter in docs/...

[ERROR] docs/patterns/profile-management.md
  - Missing required field: version
  - Invalid date format: last-updated (expected YYYY-MM-DD, got 2025/11/10)

[WARNING] docs/guides/onboarding.md
  - No incoming links detected (orphan file)

Summary:
  Files checked: 67
  Errors: 2
  Warnings: 1

Exit code: 1 (errors found)
```

**JSON Format** (`--output=json`):

```json
{
  "summary": {
    "files_checked": 67,
    "errors": 2,
    "warnings": 1,
    "passed": false
  },
  "violations": [
    {
      "file": "docs/patterns/profile-management.md",
      "severity": "error",
      "rule": "META-001",
      "message": "Missing required field: version"
    },
    {
      "file": "docs/patterns/profile-management.md",
      "severity": "error",
      "rule": "META-003",
      "message": "Invalid date format: last-updated"
    }
  ]
}
```

### Exit Codes

- `0`: All validations passed
- `1`: Errors found (blocking)
- `2`: Warnings found (non-blocking, only if `--strict` mode)

### Validation Rules

| Rule ID | Check | Severity |
|---------|-------|----------|
| META-001 | All required fields present (title, version, created, last-updated, status, tags) | error |
| META-002 | Version follows semver format (X.Y.Z) | error |
| META-003 | Dates in ISO8601 format (YYYY-MM-DD) | error |
| META-004 | Status is one of: current, deprecated, draft | error |
| META-005 | Tags is non-empty array | error |
| META-006 | Related paths exist (if specified) | error |

---

## 2. Link Validator

### Purpose

Validates that all internal Markdown links resolve to existing files or anchors.

### Script Name

Uses `markdown-link-check` (npm package)

### Command Line Interface

```bash
find docs -name "*.md" -exec markdown-link-check --config .markdown-link-check.json {} \;
```

### Configuration File

`.markdown-link-check.json`:

```json
{
  "ignorePatterns": [
    {
      "pattern": "^http"
    },
    {
      "pattern": "^https"
    }
  ],
  "replacementPatterns": [],
  "httpHeaders": [],
  "timeout": "5s",
  "retryOn429": false,
  "aliveStatusCodes": [200, 206]
}
```

### Input

- **Files**: All `.md` files in docs tree
- **Link types checked**:
    - Relative file links (e.g., `../patterns/profile-management.md`)
    - Anchor links (e.g., `#section-heading`)
    - Combined (e.g., `../patterns/profile-management.md#example`)

### Output

**Per-File Output**:

```
FILE: docs/architecture/overview.md
[✓] ../patterns/profile-management.md
[✓] #architecture-layers
[✗] ../guides/missing-file.md → Status: 404

3 links checked.
1 link broken.
```

**Summary** (via wrapper script):

```
Total files: 67
Total links: 342
Broken links: 1

Exit code: 1 (broken links found)
```

### Exit Codes

- `0`: All links valid
- `1`: Broken links found

---

## 3. Cross-Reference Generator

### Purpose

Generates bidirectional cross-references and "Related Documentation" sections.

### Script Name

`scripts/generate-cross-references.py`

### Command Line Interface

```bash
python scripts/generate-cross-references.py <docs_root> [--dry-run] [--output=<file>]
```

**Arguments**:

- `docs_root` (required): Path to documentation root
- `--dry-run` (optional): Show what would be generated without modifying files
- `--output` (optional): Write cross-reference graph to file (JSON format)

### Input

- **Frontmatter**: Parses `related` field from all files
- **Content**: Scans for inline Markdown links to other docs
- **Link detection**: Matches pattern `[text](path/to/file.md)`

### Output

**Modified Files**: Appends or updates "Related Documentation" section at end of each file:

```markdown
## Related Documentation

- [Profile Management Pattern](../patterns/profile-management.md) - Core pattern implementation
- [Clean Architecture](./clean-architecture.md) - Architectural context
- [Service Registration](../guides/service-registration.md) - DI registration guide

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
```

**JSON Graph** (`--output=graph.json`):

```json
{
  "nodes": [
    {
      "file": "docs/patterns/profile-management.md",
      "title": "Profile Management Pattern",
      "outgoing_links": 3,
      "incoming_links": 5
    }
  ],
  "edges": [
    {
      "source": "docs/patterns/profile-management.md",
      "target": "docs/architecture/clean-architecture.md",
      "type": "relates-to"
    }
  ],
  "orphans": [
    "docs/archive/old-doc.md"
  ]
}
```

### Exit Codes

- `0`: Success
- `1`: Error (e.g., invalid frontmatter, file system error)

---

## 4. Duplicate Detector

### Purpose

Identifies duplicate or highly similar documentation content.

### Script Name

`scripts/detect-duplicates.py`

### Command Line Interface

```bash
python scripts/detect-duplicates.py <docs_root> [--threshold=<percent>] [--output=<file>]
```

**Arguments**:

- `docs_root` (required): Path to documentation root
- `--threshold` (optional): Similarity threshold percentage (default: 80)
- `--output` (optional): Write report to file (CSV format)

### Input

- **Files**: All `.md` files in docs tree
- **Content**: Markdown content (excluding frontmatter)
- **Algorithm**:
    1. MD5 hash for exact duplicates
    2. Difflib sequence matcher for similarity ratio

### Output

**Console Output**:

```
Analyzing 67 files for duplicates...

EXACT DUPLICATES (100% match):
  - docs/archive/old-pattern.md
  - docs/patterns/pattern-copy.md
  Action: Merge into single file

HIGH SIMILARITY (85% match):
  - docs/guides/testing-guide.md
  - docs/guides/test-practices.md
  Common sections: 15/18 paragraphs
  Action: Review for consolidation

Summary:
  Exact duplicates: 2 pairs
  High similarity: 1 pair
  Potential space saved: ~15 KB
```

**CSV Output** (`--output=duplicates.csv`):

```csv
File1,File2,Similarity,Type,Recommendation
docs/archive/old-pattern.md,docs/patterns/pattern-copy.md,100,exact,Merge
docs/guides/testing-guide.md,docs/guides/test-practices.md,85,similar,Review
```

### Exit Codes

- `0`: Analysis complete (always succeeds; duplicates are warnings)

---

## 5. Orphan Detector

### Purpose

Identifies documentation files with no incoming links (potential orphans).

### Script Name

`scripts/detect-orphans.py`

### Command Line Interface

```bash
python scripts/detect-orphans.py <docs_root> [--exclude=<pattern>] [--report]
```

**Arguments**:

- `docs_root` (required): Path to documentation root
- `--exclude` (optional): Pattern for intentional orphans (e.g., `INDEX.md,README.md`)
- `--report` (optional): Generate detailed report file

### Input

- **Files**: All `.md` files
- **Links**: Cross-reference graph from frontmatter and content

### Output

**Console Output**:

```
Checking for orphaned files...

ORPHANED FILES (no incoming links):
  - docs/guides/deprecated-workflow.md
    Last updated: 2023-05-10 (outdated)
    Recommendation: Archive or create links

  - docs/patterns/experimental-pattern.md
    Last updated: 2025-11-01 (recent)
    Recommendation: Add to relevant guides

Excluded (intentional orphans):
  - docs/INDEX.md (entry point)
  - docs/README.md (entry point)

Summary:
  Orphans found: 2
  Excluded: 2
  Review required: 2
```

### Exit Codes

- `0`: Analysis complete (warnings only, non-blocking)

---

## 6. Migration Tracker

### Purpose

Tracks file migrations from old structure to new consolidated structure.

### Script Name

`scripts/track-migration.py`

### Command Line Interface

```bash
# Record migration
python scripts/track-migration.py add \
  --old=.copilot-tracking/memory-bank/systemPatterns.md \
  --new=docs/patterns/system-patterns.md \
  --stub

# Generate migration report
python scripts/track-migration.py report --output=migration-report.md

# Check if migration complete
python scripts/track-migration.py status
```

### Input

- **Migration log**: JSON file at `docs/.metadata/migration-log.json`
- **File system**: Verifies old/new paths exist

### Output

**Migration Log** (`migration-log.json`):

```json
{
  "version": "1.0.0",
  "migrations": [
    {
      "oldPath": ".copilot-tracking/memory-bank/systemPatterns.md",
      "newPath": "docs/patterns/system-patterns.md",
      "migrationDate": "2025-11-15",
      "redirectStubCreated": true,
      "stubRemovalDate": "2026-02-15",
      "gitHistoryPreserved": true
    }
  ]
}
```

**Migration Report** (Markdown):

```markdown
# Documentation Migration Report

**Generated**: 2025-11-15

## Summary
- Files migrated: 45
- Stubs created: 45
- Stubs pending removal: 45
- Duplicates consolidated: 12

## Upcoming Stub Removals
- 2026-02-15: 45 stubs ready for removal

## Migration by Category
- Patterns: 15 files
- Architecture: 8 files
- Guides: 12 files
- Reviews: 10 files
```

### Exit Codes

- `0`: Success
- `1`: Error (e.g., invalid paths, missing log file)

---

## Integration with CI/CD

### GitHub Actions Workflow

```yaml
name: Documentation Validation

on: [push, pull_request]

jobs:
  validate-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'

      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install dependencies
        run: |
          pip install pyyaml
          npm install -g markdown-link-check

      - name: Validate frontmatter
        run: python scripts/validate-frontmatter.py docs/

      - name: Check links
        run: find docs -name "*.md" -exec markdown-link-check {} \;

      - name: Detect orphans
        run: python scripts/detect-orphans.py docs/ --report
        continue-on-error: true

      - name: Upload reports
        uses: actions/upload-artifact@v3
        with:
          name: validation-reports
          path: docs/.metadata/reports/
```

---

## Script Dependencies

### Python Requirements

```
# requirements.txt
pyyaml>=6.0
```

### Node Requirements

```
# package.json dependencies
markdown-link-check>=3.11.0
markdownlint-cli>=0.37.0
```

---

## Common Data Structures

### ValidationResult

```python
@dataclass
class ValidationResult:
    file_path: str
    rule_id: str
    severity: str  # "error" | "warning"
    message: str
    line_number: Optional[int] = None
```

### CrossReferenceEdge

```python
@dataclass
class CrossReferenceEdge:
    source: str  # file path
    target: str  # file path
    link_type: str  # "relates-to", "implements", etc.
    context: Optional[str] = None
```

---

## Error Handling

All scripts follow consistent error handling:

1. **Validation errors**: Report and continue (collect all violations)
2. **File system errors**: Fail fast with clear error message
3. **Parse errors**: Report file and line number, skip file
4. **Exit codes**: Follow Unix conventions (0=success, non-zero=failure)
