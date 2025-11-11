# Quickstart: Documentation Validation

**Feature**: Documentation Validation and Synchronization
**Branch**: `010-docs-validation-sync`
**Date**: 2025-11-11

## Overview

This guide shows how to run the S7Tools documentation validation system, interpret results, and integrate with CI pipelines.

---

## Prerequisites

**System Requirements**:
- Python 3.10+ installed
- .NET 8 SDK installed (for code compilation)
- Git (for repository operations)

**Installation**:

```bash
# Install Python dependencies
pip install mistune pytest pytest-cov

# Verify .NET SDK
dotnet --version  # Should be 8.0 or higher
```

---

## Running Validation

### Option 1: Standalone Script (Recommended for Development)

```bash
# From repository root
cd /path/to/S7-Tools

# Run documentation validation
python3 scripts/validate-documentation.py

# Run with verbose output
python3 scripts/validate-documentation.py --verbose

# Validate specific documentation category
python3 scripts/validate-documentation.py --category patterns

# Skip slow code compilation checks (faster, less thorough)
python3 scripts/validate-documentation.py --skip-compilation
```

### Option 2: Full Validation Suite (Recommended for CI)

```bash
# Run all validation scripts (includes documentation validation)
./scripts/validate-all.sh

# This runs:
# 1. Frontmatter validation (validate-frontmatter.py)
# 2. Link validation (markdown-link-check)
# 3. Archive validation (validate-archive.py)
# 4. Documentation validation (validate-documentation.py) ← NEW
```

---

## Understanding Output

### Console Output

**Success Example**:
```
✅ Documentation Validation Report
=====================================
Generated: 2025-11-11 10:30:45
Validation Version: 1.0.0

📊 Summary Statistics:
  • Files Checked: 52
  • Total Errors: 0
  • Total Warnings: 2
  • Execution Time: 45.3s

✅ Code Examples: 100% compiled (40/40)
✅ File References: 100% valid (128/128)
✅ Namespace Conventions: 100% compliant (85/85)
✅ Pattern Implementations: 100% verified (5/5)
✅ Internal Links: 100% valid (234/234)
✅ EditorConfig Consistency: 100% (0 discrepancies)

⚠️  Warnings:
  1. Simplified code example in docs/patterns/profile-management.md:145
     → Missing using statements (expected for simplified examples)
  2. Deprecated pattern reference in docs/archive/old-pattern.md:89
     → Archived documentation contains outdated pattern

✅ All validation checks passed!
```

**Failure Example**:
```
❌ Documentation Validation Report
=====================================
Generated: 2025-11-11 10:35:12
Validation Version: 1.0.0

📊 Summary Statistics:
  • Files Checked: 52
  • Total Errors: 3
  • Total Warnings: 1
  • Execution Time: 47.8s

❌ Code Examples: 95.0% compiled (38/40)
  → 2 examples failed compilation

❌ File References: 98.4% valid (126/128)
  → 2 file paths do not exist

✅ Namespace Conventions: 100% compliant (85/85)
✅ Pattern Implementations: 100% verified (5/5)
⚠️  Internal Links: 99.6% valid (233/234)
  → 1 broken link found
✅ EditorConfig Consistency: 100% (0 discrepancies)

🔴 Errors:
  1. Code compilation failed: docs/patterns/internal-method.md:112
     → CS0246: The type or namespace name 'SemaphoreSlim' could not be found
     → Add missing using: System.Threading

  2. File path not found: docs/architecture/overview.md:234
     → Referenced: src/S7Tools/Models/NonExistent.cs
     → File does not exist in repository

  3. File path not found: docs/guides/testing-guide.md:67
     → Referenced: tests/S7Tools.Old.Tests/OldTest.cs
     → File does not exist in repository

⚠️  Warnings:
  1. Broken internal link: docs/patterns/_index.md:89
     → Target: docs/patterns/deleted-pattern.md (404)

❌ Validation failed with 3 errors!
```

### Generated Reports

**Markdown Report** (`docs/.metadata/validation-report.md`):
```markdown
# Documentation Validation Report

**Generated**: 2025-11-11 10:30:45
**Validation Version**: 1.0.0
**Status**: ✅ PASS

## Summary

- **Files Checked**: 52
- **Total Errors**: 0
- **Total Warnings**: 2
- **Execution Time**: 45.3s

## Detailed Results

### Code Examples (40 total)
✅ Compilation Success Rate: 100%

| Source File | Line | Language | Status |
|-------------|------|----------|--------|
| docs/patterns/profile-management.md | 145 | csharp | ✅ Compiled |
| docs/architecture/mvvm-patterns.md | 89 | csharp | ✅ Compiled |
| ... | ... | ... | ... |

### File Path References (128 total)
✅ Reference Success Rate: 100%

[... detailed table ...]

### Namespace Validations (85 total)
✅ Compliance Rate: 100%

[... detailed table ...]

### Pattern Implementations (5 total)
✅ Verification Rate: 100%

[... detailed table ...]
```

**JSON Report** (`docs/.metadata/validation-results.json`):
```json
{
  "generated_at": "2025-11-11T10:30:45Z",
  "validation_version": "1.0.0",
  "total_files_checked": 52,
  "total_errors": 0,
  "total_warnings": 2,
  "compilation_success_rate": 100.0,
  "file_reference_success_rate": 100.0,
  "namespace_compliance_rate": 100.0,
  "pattern_verification_rate": 100.0,
  "execution_time_seconds": 45.3,
  "summary": "All validation checks passed with 2 warnings"
}
```

---

## CI Integration

### GitHub Actions Workflow

Add to `.github/workflows/documentation-validation.yml`:

```yaml
name: Documentation Validation

on:
  push:
    branches: [ main, develop ]
    paths:
      - 'docs/**'
      - 'src/**/*.cs'
      - '.editorconfig'
  pull_request:
    paths:
      - 'docs/**'
      - 'src/**/*.cs'
      - '.editorconfig'

jobs:
  validate-docs:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Setup Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.10'

    - name: Install Python dependencies
      run: pip install mistune

    - name: Run documentation validation
      run: python3 scripts/validate-documentation.py

    - name: Upload validation report
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: validation-report
        path: docs/.metadata/validation-report.md

    - name: Parse validation results
      if: always()
      run: |
        if jq -e '.total_errors > 0' docs/.metadata/validation-results.json; then
          echo "❌ Documentation validation failed!"
          exit 1
        fi
```

### Pre-Commit Hook (Optional)

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash
# Run documentation validation before commit (if docs changed)

if git diff --cached --name-only | grep -qE '^docs/'; then
    echo "📚 Running documentation validation..."
    python3 scripts/validate-documentation.py --skip-compilation

    if [ $? -ne 0 ]; then
        echo "❌ Documentation validation failed. Commit aborted."
        echo "Fix errors or use 'git commit --no-verify' to bypass."
        exit 1
    fi
fi
```

---

## Interpreting Results

### Success Criteria (from spec)

The validation **PASSES** if:
- ✅ `compilation_success_rate >= 100%` (all code examples compile)
- ✅ `file_reference_success_rate >= 100%` (all file paths exist)
- ✅ `namespace_compliance_rate >= 100%` (all namespaces follow conventions)
- ✅ `pattern_verification_rate >= 100%` (all 5 core patterns verified)
- ✅ `len(broken_links) == 0` (no broken internal links)
- ✅ `len(editorconfig_discrepancies) == 0` (no .editorconfig vs docs conflicts)
- ✅ `execution_time_seconds < 60` (performance target met)

### Common Issues and Fixes

**Issue 1: Code compilation failed - missing using statements**
```
❌ CS0246: The type or namespace name 'ReactiveObject' could not be found
```
**Fix**: Add required using statements to code example:
```csharp
// Add to example:
using ReactiveUI;
```

**Issue 2: File path reference not found**
```
❌ Referenced: src/S7Tools/Models/OldModel.cs (does not exist)
```
**Fix Options**:
- Update documentation to reference correct path
- Restore deleted file if it was accidentally removed
- Mark path as deprecated in documentation

**Issue 3: Namespace convention violation**
```
❌ Expected: S7Tools.ViewModels.Pages.HomeViewModel
    Found: S7Tools.ViewModels.HomeViewModel (missing category)
```
**Fix**: Move file to correct category folder and update namespace:
```bash
# Move file
mv src/S7Tools/ViewModels/HomeViewModel.cs src/S7Tools/ViewModels/Pages/

# Update namespace in file
namespace S7Tools.ViewModels.Pages;  # Add .Pages
```

**Issue 4: Pattern implementation not found**
```
❌ Pattern 'Internal Method' expected in:
    - src/S7Tools/Services/SocatService.cs (NOT FOUND)
```
**Fix**: Implement missing pattern or update documentation to reflect current implementation location.

---

## Performance Optimization

**Speed Up Validation** (for rapid iteration):

```bash
# Skip slow code compilation (fastest, least thorough)
python3 scripts/validate-documentation.py --skip-compilation

# Validate only changed files (Git-aware)
python3 scripts/validate-documentation.py --changed-only

# Validate specific category (faster scope)
python3 scripts/validate-documentation.py --category guides
```

**Parallel Execution** (for full validation):
```bash
# Compile examples in parallel (uses all CPU cores)
python3 scripts/validate-documentation.py --parallel
```

---

## Troubleshooting

### Validation Script Crashes

**Error**: `ModuleNotFoundError: No module named 'mistune'`
**Fix**: `pip install mistune`

**Error**: `subprocess.TimeoutExpired: Command 'dotnet build' timed out`
**Fix**: Check network connection (NuGet restore) or increase timeout:
```bash
python3 scripts/validate-documentation.py --compilation-timeout 60
```

### Unexpected Failures

**Enable debug logging**:
```bash
python3 scripts/validate-documentation.py --verbose --debug
```

**Check validation version**:
```bash
python3 scripts/validate-documentation.py --version
```

**Verify prerequisites**:
```bash
# Check Python version
python3 --version  # Should be 3.10+

# Check .NET SDK
dotnet --version   # Should be 8.0+

# Check mistune installation
python3 -c "import mistune; print(mistune.__version__)"
```

---

## Next Steps

After running validation:

1. **Review Reports**: Check `docs/.metadata/validation-report.md` for detailed findings
2. **Fix Errors**: Address all errors flagged in output (required for PR merge)
3. **Address Warnings**: Optional but recommended for documentation quality
4. **Commit Fixes**: Update documentation and source code as needed
5. **Re-validate**: Run validation again to confirm fixes

---

## Additional Resources

- **Full Specification**: `specs/010-docs-validation-sync/spec.md`
- **Data Model**: `specs/010-docs-validation-sync/data-model.md`
- **JSON Schema**: `specs/010-docs-validation-sync/contracts/validation-report-schema.json`
- **Existing Validation Scripts**: `scripts/validate-*.py` (for reference)
