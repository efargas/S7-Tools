#!/bin/bash
# Wrapper script to run all validation checks in sequence
# Usage: ./scripts/validate-all.sh [docs_root]
#
# By default, validates both docs/ and docs/website/docs/
# Pass a specific path to validate only that directory.

set -e  # Exit on first error

DOCS_ROOT="${1:-docs}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WEBSITE_DOCS="$REPO_ROOT/docs/website/docs"

# Use virtual environment if available, otherwise use system python3
if [ -f "$REPO_ROOT/.venv/bin/python" ]; then
    PYTHON="$REPO_ROOT/.venv/bin/python"
    echo "Using virtual environment: $REPO_ROOT/.venv"
else
    PYTHON="python3"
    echo "Using system Python"
fi

echo "========================================="
echo "S7Tools Documentation Validation Suite"
echo "========================================="
echo ""

# Check if docs directory exists
if [ ! -d "$DOCS_ROOT" ]; then
    echo "Error: Documentation directory '$DOCS_ROOT' not found"
    exit 1
fi

echo "📁 Documentation root: $DOCS_ROOT"
echo ""

# Frontmatter validation (BLOCKING)
echo "1️⃣  Validating frontmatter..."
echo "-----------------------------------"
if $PYTHON "$SCRIPT_DIR/validate-frontmatter.py" "$DOCS_ROOT"; then
    echo "✓ Frontmatter validation passed"
else
    echo "✗ Frontmatter validation FAILED"
    exit 1
fi
echo ""

# Link validation (would run markdown-link-check if available)
echo "2️⃣  Checking links..."
echo "-----------------------------------"
if command -v markdown-link-check &> /dev/null; then
    if find "$DOCS_ROOT" -name "*.md" -exec markdown-link-check --config .markdown-link-check.json {} \; 2>&1 | grep -q "ERROR"; then
        echo "✗ Link validation FAILED"
        exit 1
    else
        echo "✓ Link validation passed"
    fi
else
    echo "⚠ markdown-link-check not installed (skipping)"
    echo "  Install with: npm install -g markdown-link-check"
fi
echo ""

# Orphan detection (WARNING only)
echo "3️⃣  Detecting orphaned files..."
echo "-----------------------------------"
$PYTHON "$SCRIPT_DIR/detect-orphans.py" "$DOCS_ROOT" || true
echo ""

# Duplicate detection (WARNING only)
echo "4️⃣  Detecting duplicate content..."
echo "-----------------------------------"
$PYTHON "$SCRIPT_DIR/detect-duplicates.py" "$DOCS_ROOT" || true
echo ""

# Documentation validation (code examples, file refs, namespaces, patterns, links)
echo "5️⃣  Validating documentation against source code..."
echo "-----------------------------------"
if $PYTHON "$SCRIPT_DIR/validate-documentation.py" --output "$REPO_ROOT/docs/.metadata"; then
    echo "✓ Documentation validation passed"
else
    echo "✗ Documentation validation FAILED"
    exit 1
fi
echo ""

# Website docs validation (docs/website/docs) — only when running the default suite
RESOLVED_DOCS_ROOT="$(cd "$DOCS_ROOT" 2>/dev/null && pwd)"
RESOLVED_DEFAULT_DOCS="$(cd "$REPO_ROOT/docs" 2>/dev/null && pwd)"
if [ "$RESOLVED_DOCS_ROOT" = "$RESOLVED_DEFAULT_DOCS" ] && [ -d "$WEBSITE_DOCS" ]; then
    echo "========================================="
    echo "Website Documentation Validation"
    echo "========================================="
    echo "📁 Website docs root: $WEBSITE_DOCS"
    echo ""

    echo "1️⃣  Validating website docs frontmatter..."
    echo "-----------------------------------"
    if $PYTHON "$SCRIPT_DIR/validate-frontmatter.py" "$WEBSITE_DOCS"; then
        echo "✓ Website frontmatter validation passed"
    else
        echo "✗ Website frontmatter validation FAILED"
        exit 1
    fi
    echo ""

    echo "2️⃣  Detecting orphaned website docs files..."
    echo "-----------------------------------"
    $PYTHON "$SCRIPT_DIR/detect-orphans.py" "$WEBSITE_DOCS" || true
    echo ""

    echo "3️⃣  Validating website docs against source code..."
    echo "-----------------------------------"
    if $PYTHON "$SCRIPT_DIR/validate-documentation.py" \
        --docs-path "$WEBSITE_DOCS" \
        --output "$REPO_ROOT/docs/.metadata/website"; then
        echo "✓ Website documentation validation passed"
    else
        echo "✗ Website documentation validation FAILED"
        exit 1
    fi
    echo ""
fi

echo "========================================="
echo "✓ Validation suite complete"
echo "========================================="
