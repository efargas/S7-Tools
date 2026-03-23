# Documentation Validation Scripts

This directory contains validation scripts for the S7Tools documentation consolidation system.

## Setup

### Python Environment

Create and activate a virtual environment:

```bash
# Create virtual environment
python3 -m venv .venv

# Activate (Linux/macOS)
source .venv/bin/activate

# Activate (Windows)
.venv\Scripts\activate

# Install dependencies
pip install -r scripts/requirements.txt
```

### Node.js Tools (Optional)

For link validation with `markdown-link-check`:

```bash
# Install globally (requires Node.js 20+)
npm install -g markdown-link-check markdownlint-cli

# Or use npx without global install
npx markdown-link-check docs/INDEX.md
```

## Available Scripts

### 1. Frontmatter Validator

Validates YAML frontmatter in all Markdown files.

```bash
python scripts/validate-frontmatter.py docs/ [--output=json] [--strict]
```

**Options:**
- `--output=json|text`: Output format (default: text)
- `--strict`: Fail on warnings in addition to errors

**Exit codes:**
- `0`: All validations passed
- `1`: Errors found (blocking)
- `2`: Warnings found (non-blocking, only if `--strict` mode)

### 2. Duplicate Detector

Identifies duplicate or highly similar documentation content.

```bash
python scripts/detect-duplicates.py docs/ [--threshold=80] [--output=duplicates.csv]
```

**Options:**
- `--threshold=<percent>`: Similarity threshold (default: 80)
- `--output=<file>`: Write report to CSV file

### 3. Orphan Detector

Finds documentation files with no incoming links.

```bash
python scripts/detect-orphans.py docs/ [--exclude=INDEX.md,README.md] [--report]
```

**Options:**
- `--exclude=<pattern>`: Comma-separated patterns for intentional orphans
- `--report`: Generate detailed report file

### 4. Cross-Reference Generator

Generates bidirectional cross-references and "Related Documentation" sections.

```bash
python scripts/generate-cross-references.py docs/ [--dry-run] [--output=graph.json]
```

**Options:**
- `--dry-run`: Show what would be generated without modifying files
- `--output=<file>`: Export cross-reference graph to JSON

### 5. Migration Tracker

Tracks file migrations from old structure to new consolidated structure.

```bash
# Record migration
python scripts/track-migration.py add --old=<path> --new=<path> [--stub]

# Generate migration report
python scripts/track-migration.py report [--output=migration-report.md]

# Check migration status
python scripts/track-migration.py status
```

### 6. Documentation Validation (Code Sync)

Validates documentation against source code: compiles C# examples, checks file references, validates namespace conventions, verifies pattern implementations, and checks internal links.

```bash
python scripts/validate-documentation.py [options]
```

**Options:**
- `--verbose`: Enable verbose output with detailed progress
- `--skip-compilation`: Skip code compilation (faster, for quick checks)
- `--category <cat>`: Validate specific category only (architecture, patterns, guides, etc.)
- `--output <path>`: Output directory for reports (default: docs/.metadata/)

**Output:**
- JSON report: `docs/.metadata/validation-results.json` (machine-readable)
- Markdown report: `docs/.metadata/validation-report.md` (human-readable)
- Console output with color-coded status (green ✅/red ❌/yellow ⚠️)

**Exit codes:**
- `0`: All validations passed
- `1`: Validation errors found
- `2`: Exception during validation

**Example usage:**
```bash
# Full validation (all checks including compilation)
python scripts/validate-documentation.py

# Quick validation (skip compilation for speed)
python scripts/validate-documentation.py --skip-compilation

# Validate specific category with verbose output
python scripts/validate-documentation.py --category patterns --verbose

# Custom output directory
python scripts/validate-documentation.py --output /tmp/validation-reports
```

### 7. Validation Suite (All-in-One)

Runs all validation checks in sequence.

```bash
./scripts/validate-all.sh [docs_root]
```

This wrapper script runs:
1. Frontmatter validation (BLOCKING)
2. Link validation (BLOCKING, if markdown-link-check available)
3. Orphan detection (WARNING only)
4. Duplicate detection (WARNING only)

## Validation Rules

### Frontmatter Requirements

All documentation files must include:

```yaml
---
title: "Document Title"
version: "1.0.0"           # Semantic versioning (X.Y.Z)
created: "2025-11-10"      # ISO8601 format (YYYY-MM-DD)
last-updated: "2025-11-10" # ISO8601 format (YYYY-MM-DD)
status: "current"          # current | deprecated | draft
tags: ["tag1", "tag2"]     # Non-empty array
related:                   # Optional: paths to related docs
  - docs/path/to/related.md
supersedes: docs/path/to/old.md  # Optional: for deprecated content
---
```

### Validation Rule IDs

| Rule ID | Description | Severity |
|---------|-------------|----------|
| META-001 | Required fields present | error |
| META-002 | Valid semver format | error |
| META-003 | Valid date format (ISO8601) | error |
| META-004 | Status enum validation | error |
| META-005 | Tags non-empty array | error |
| META-006 | Related paths exist | error |
| LINK-001 | Broken internal links | error |
| LINK-002 | Orphaned files | warning |
| CONTENT-001 | Duplicate content | warning |

## CI/CD Integration

These scripts are designed to run in CI/CD pipelines. Example GitHub Actions workflow:

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

      - name: Install dependencies
        run: pip install -r scripts/requirements.txt

      - name: Validate documentation
        run: ./scripts/validate-all.sh docs/
```

## Troubleshooting

### Python Import Errors

If you see `ModuleNotFoundError: No module named 'yaml'`:

```bash
# Ensure virtual environment is activated
source .venv/bin/activate

# Reinstall dependencies
pip install -r scripts/requirements.txt
```

### Permission Denied

If you see `Permission denied` when running scripts:

```bash
# Make scripts executable
chmod +x scripts/*.py scripts/*.sh
```

### Node.js Version Issues

`markdown-link-check` requires Node.js 20+. If you have an older version:

```bash
# Skip link validation or upgrade Node.js
# The wrapper script will skip if markdown-link-check is not available
```

## Development

### Adding New Validation Rules

1. Edit the appropriate script (e.g., `validate-frontmatter.py`)
2. Add rule ID constant and validation logic
3. Update rule table in this README
4. Add test fixtures in `docs/.test-fixtures/`
5. Update CI/CD workflow if needed

### Testing Scripts Locally

```bash
# Activate virtual environment
source .venv/bin/activate

# Test on specific file
python scripts/validate-frontmatter.py docs/

# Test with dry-run
python scripts/generate-cross-references.py docs/ --dry-run

# Check exit codes
echo $?  # 0 = success, 1 = errors, 2 = warnings (if --strict)
```

## Related Documentation

- [Index](../docs/INDEX.md)
- [Readme](../docs/README.md)
- [Contributing To Docs](../docs/guides/contributing-to-docs.md)
- [Documentation Templates](../docs/guides/documentation-templates.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
