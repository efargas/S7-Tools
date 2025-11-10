#!/bin/bash
# Wrapper script to run all validation checks in sequence
# Usage: ./scripts/validate-all.sh [docs_root]

set -e  # Exit on first error

DOCS_ROOT="${1:-docs}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

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
if python3 "$SCRIPT_DIR/validate-frontmatter.py" "$DOCS_ROOT"; then
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
python3 "$SCRIPT_DIR/detect-orphans.py" "$DOCS_ROOT" || true
echo ""

# Duplicate detection (WARNING only)
echo "4️⃣  Detecting duplicate content..."
echo "-----------------------------------"
python3 "$SCRIPT_DIR/detect-duplicates.py" "$DOCS_ROOT" || true
echo ""

echo "========================================="
echo "✓ Validation suite complete"
echo "========================================="
