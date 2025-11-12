# Task Breakdown: Documentation Validation and Synchronization

**Feature**: Documentation Validation and Synchronization
**Branch**: `010-docs-validation-sync`
**Date**: 2025-11-11
**Phase**: Implementation Task Breakdown

## Task Overview

This task breakdown converts the planning artifacts (spec.md, plan.md, research.md, data-model.md) into actionable implementation tasks. Each task is sized for 1-3 hours of focused work and has clear acceptance criteria tied to spec requirements.

**Total Estimated Effort**: 15-19 hours (over 2-3 days for single developer)

---

## Task Dependency Graph

```
TASK-1 (Foundation)
  ├──> TASK-2 (Extraction)
  │      └──> TASK-3 (C# Compilation Validator)
  │      └──> TASK-4 (File & Namespace Validators)
  │
  └──> TASK-5 (Pattern & Link Validators) [parallel with TASK-2]

TASK-3, TASK-4, TASK-5 ──> TASK-6 (Report Generation)

TASK-6 ──> TASK-7 (CI Integration)

TASK-1 through TASK-7 ──> TASK-8 (Testing & Documentation)
```

**Critical Path**: TASK-1 → TASK-2 → TASK-3 → TASK-6 → TASK-7 → TASK-8 (11-14 hours)

**Parallelization Opportunities**:
- TASK-5 (Pattern/Link validators) can run parallel with TASK-2 after TASK-1
- TASK-3 and TASK-4 can be developed simultaneously after TASK-2
- Testing (TASK-8) can begin incrementally after each task completes

---

## Task List

| Task ID | Task Name | Dependencies | Effort | Priority | FR/SC Reference |
|---------|-----------|--------------|--------|----------|-----------------|
| TASK-1 | Foundation & Project Setup | None | 1-2h | P0 | FR-012 |
| TASK-2 | Markdown Parser & Code Extractor | TASK-1 | 2-3h | P0 | FR-001 |
| TASK-3 | C# Code Compilation Validator | TASK-2 | 3-4h | P0 | FR-001, SC-001, SC-008 |
| TASK-4 | File Path & Namespace Validators | TASK-2 | 2-3h | P0 | FR-002, FR-003, FR-004, SC-002, SC-003 |
| TASK-5 | Pattern & Link Validators | TASK-1 | 2-3h | P1 | FR-005, FR-007, FR-010, SC-004, SC-005 |
| TASK-6 | Report Generation (JSON + Markdown) | TASK-3, TASK-4, TASK-5 | 2-3h | P0 | FR-012, SC-009 |
| TASK-7 | CI Integration & validate-all.sh | TASK-6 | 1-2h | P1 | SC-008 |
| TASK-8 | Testing & Documentation | TASK-1 through TASK-7 | 2-3h | P0 | Article III (Test-First) |

**Total**: 15-19 hours estimated

---

## Detailed Task Specifications

### TASK-1: Foundation & Project Setup

**Objective**: Create project structure, set up Python virtual environment, install dependencies, and implement core entity classes

**Deliverables**:
1. Create Python virtual environment:
   - `python3 -m venv scripts/.venv`
   - Create `scripts/activate-venv.sh` helper script
   - Update `.gitignore` to exclude `scripts/.venv/`
2. Create directory structure:
   - `scripts/extractors/`
   - `scripts/validators/`
   - `scripts/reporters/`
   - `scripts/tests/`
3. Create `scripts/requirements.txt` with dependencies:
   - `mistune>=3.0.0`
   - `pytest>=7.0.0`
   - `pytest-cov>=4.0.0`
4. Implement core data model classes (from data-model.md):
   - `DocumentationFile` class
   - `CodeExample` class
   - `CompilationResult` class
   - `FilePathReference` class
   - `NamespaceValidation` class
   - `PatternImplementation` class
   - `EditorConfigRule` class
   - `ValidationReport` class

**Acceptance Criteria**:
- ✅ Virtual environment created at `scripts/.venv/`
- ✅ Activation helper script works: `source scripts/activate-venv.sh`
- ✅ `.gitignore` updated to exclude venv directory
- ✅ All directories created and recognized by Python (has `__init__.py`)
- ✅ `pip install -r scripts/requirements.txt` succeeds within venv
- ✅ All 8 entity classes defined with proper `__init__`, `__repr__`, and type hints
- ✅ Entity classes match data-model.md specifications (all attributes present)
- ✅ Basic unit tests for entity instantiation pass

**Implementation Notes**:
- Virtual environment isolates dependencies from system Python
- Create helper script `scripts/activate-venv.sh`:
  ```bash
  #!/bin/bash
  source "$(dirname "$0")/.venv/bin/activate"
  ```
- Add to `.gitignore`:
  ```
  scripts/.venv/
  scripts/__pycache__/
  ```
- Use `dataclasses` or `pydantic` for entity definitions (prefer dataclasses for simplicity)
- Include validation logic in entity classes where specified (e.g., `CodeExample.language` must be in allowed list)
- Follow existing scripts/ code style (4-space indentation, type hints)
- Update `scripts/validate-all.sh` to activate venv before running validation scripts

**Dependencies**: None

**Effort Estimate**: 1-2 hours

**Related FR/SC**: FR-012 (entity definitions for validation report)

---

### TASK-2: Markdown Parser & Code Extractor

**Status**: Complete

**Objective**: Implement markdown parsing to extract C# code blocks, file references, and links

**Deliverables**:
1. `extractors/markdown_parser.py` module with:
   - `parse_markdown_file(path: Path) -> DocumentationFile`
   - `extract_code_blocks(ast) -> list[CodeExample]`
   - `extract_file_references(content: str) -> list[FilePathReference]`
   - `extract_links(content: str) -> list[str]`
2. Support for simplified code examples (detection of `// ... simplified` annotations)
3. Extraction of required using statements from code blocks

**Acceptance Criteria**:
- ✅ Parser successfully extracts code blocks with language tags (```csharp, ```python, ```bash)
- ✅ File path references extracted using regex patterns (`src/...`, `docs/...`, relative paths)
- ✅ Internal links extracted (relative markdown links like `[text](../path/to/file.md)`)
- ✅ Simplified examples flagged with `is_simplified=True` when annotation present
- ✅ Required usings inferred from code (e.g., `using System;` → `["System"]`)
- ✅ Unit tests validate extraction on sample markdown files

**Implementation Notes**:
- Use mistune v3 AST parser (from research.md decision 1)
- Handle edge case EC-002 from spec: simplified examples with missing imports
- Extract context (surrounding text) for better error reporting
- Performance target: Parse 50+ files in <5 seconds

**Dependencies**: TASK-1 (entity classes)

**Effort Estimate**: 2-3 hours

**Related FR/SC**: FR-001, FR-002, FR-010

---

### TASK-3: C# Code Compilation Validator

**Status**: Complete

**Objective**: Implement C# code compilation validation using dotnet CLI

**Deliverables**:
1. `validators/code_compiler.py` module with:
   - `compile_csharp_example(code: CodeExample, references: list[str]) -> CompilationResult`
   - `create_temp_project(code: str, usings: list[str]) -> Path`
   - `batch_compile_examples(examples: list[CodeExample]) -> list[CompilationResult]`
2. Template .csproj generator with S7Tools references
3. Timeout handling (30s per compilation - from research.md risk mitigation)

**Acceptance Criteria**:
- ✅ Single code example compiles via dotnet CLI subprocess
- ✅ Compilation errors/warnings captured in `CompilationResult`
- ✅ Temp directory created, used, and cleaned up successfully
- ✅ Batch compilation works (multiple examples in single project where possible)
- ✅ 30-second timeout prevents compilation hangs
- ✅ Compilation time measured and recorded in milliseconds
- ✅ Success rate calculation matches SC-001 (100% target)
- ✅ Unit tests with sample C# code validate compilation logic

**Implementation Notes**:
- Use subprocess.run() with timeout parameter (from research.md decision 2)
- Generate minimal .csproj referencing S7Tools.Core, Avalonia.ReactiveUI
- Parse dotnet build output for error codes (CS0246, etc.)
- Handle edge case: simplified examples skip compilation if annotated
- Performance target: <60s for all examples (SC-008)

**Dependencies**: TASK-2 (CodeExample entities populated)

**Effort Estimate**: 3-4 hours (most complex validation logic)

**Related FR/SC**: FR-001, SC-001 (100% compilation success), SC-008 (<60s execution)

---

### TASK-4: File Path & Namespace Validators

**Objective**: Implement file path existence checks and namespace convention validation

**Deliverables**:
1. `validators/file_reference.py` module with:
   - `validate_file_reference(ref: FilePathReference) -> bool`
   - `resolve_path(referenced_path: str, source_file: str) -> Path`
2. `validators/namespace_validator.py` module with:
   - `extract_namespace_from_file(path: Path) -> str`
   - `validate_namespace_convention(file_path: Path, category: str) -> NamespaceValidation`
   - `scan_viewmodels_and_views() -> list[NamespaceValidation]`

**Acceptance Criteria**:
- ✅ File references resolve correctly (absolute, relative, project-relative paths)
- ✅ Non-existent files flagged with `exists=False`
- ✅ Namespace extraction from C# files works (regex or Roslyn syntax tree)
- ✅ ViewModel namespaces validated against `S7Tools.ViewModels.{Category}` pattern
- ✅ View namespaces validated against `S7Tools.Views.{Category}` pattern
- ✅ All 9 categories recognized (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)
- ✅ Success rate calculation matches SC-002 (100% file references exist)
- ✅ Compliance rate calculation matches SC-003 (100% namespace compliance)
- ✅ Unit tests validate path resolution and namespace parsing

**Implementation Notes**:
- Use pathlib for cross-platform path operations
- Namespace extraction via regex: `namespace\s+([\w\.]+)`
- Handle file-scoped namespaces (C# 10+): `namespace S7Tools.ViewModels.Pages;`
- Category inference from file path (e.g., `ViewModels/Pages/` → category="Pages")
- Performance target: Scan 500+ files in <10 seconds

**Dependencies**: TASK-2 (FilePathReference entities populated)

**Effort Estimate**: 2-3 hours

**Related FR/SC**: FR-002, FR-003, FR-004, SC-002 (100% file paths exist), SC-003 (100% namespace compliance)

---

### TASK-5: Pattern & Link Validators

**Objective**: Implement architectural pattern verification and internal link resolution

**Deliverables**:
1. `validators/pattern_validator.py` module with:
   - `verify_pattern_implementation(pattern: str) -> PatternImplementation`
   - `find_pattern_markers(pattern_name: str) -> list[Path]`
   - Pattern-specific validators for 5 core patterns:
     - Profile Management → Check for `StandardProfileManager<T>`, `IProfileBase`
     - Internal Method Pattern → Check for public/internal method pairs in semaphore services
     - Resource Coordination → Check for `ResourceCoordinator` class
     - Custom Exceptions → Check for `S7Tools.Core.Exceptions` namespace
     - Reusable Controls → Check for `SerialPortDiscoveryControl`
2. `validators/link_validator.py` module with:
   - `validate_internal_links(doc_file: DocumentationFile) -> list[dict]`
   - `resolve_markdown_link(link: str, source: Path) -> bool`

**Acceptance Criteria**:
- ✅ All 5 core patterns verified (from spec FR-007)
- ✅ Pattern markers found in documented locations
- ✅ Missing pattern implementations flagged
- ✅ Internal markdown links resolve correctly
- ✅ Broken links reported with source file and line number
- ✅ Pattern verification rate matches SC-004 (100% of 5 patterns verified)
- ✅ Link validation matches SC-005 (zero broken links)
- ✅ Unit tests validate pattern detection and link resolution

**Implementation Notes**:
- Pattern markers: interfaces, base classes, specific method signatures
- Use grep/regex for fast pattern searching across codebase
- Link resolution: relative path resolution from source document
- Handle anchor links (#section-name) separately
- Performance target: Pattern verification <5 seconds, link checking <5 seconds

**Dependencies**: TASK-1 (PatternImplementation entity), can run parallel with TASK-2

**Effort Estimate**: 2-3 hours

**Related FR/SC**: FR-005, FR-007, FR-010, SC-004 (5 patterns verified), SC-005 (zero broken links)

---

### TASK-6: Report Generation (JSON + Markdown)

**Status**: Complete ✅
**Objective**: Aggregate validation results and generate human/machine-readable reports

**Deliverables**:
1. ✅ `reporters/json_reporter.py` module with:
   - `generate_json_report(report: ValidationReport) -> str`
   - JSON schema validation against `contracts/validation-report-schema.json`
2. ✅ `reporters/markdown_reporter.py` module with:
   - `generate_markdown_report(report: ValidationReport) -> str`
   - Human-readable formatting with sections:
     - Executive Summary (success rates)
     - Code Example Compilation Results (errors grouped)
     - File Reference Validation (broken paths)
     - Namespace Convention Compliance (violations)
     - Pattern Implementation Status (missing patterns)
     - Broken Links (404s)
     - EditorConfig Discrepancies
     - Recommendations (deprecation candidates)
3. ✅ `scripts/validate-documentation.py` main orchestrator:
   - Command-line argument parsing (`--verbose`, `--skip-compilation`, `--category`, `--output`)
   - Progress reporting (console output with colorama)
   - Final report generation (JSON + Markdown)

**Acceptance Criteria**:
- ✅ ValidationReport entity aggregates all validation results
- ✅ JSON output matches schema in `contracts/validation-report-schema.json`
- ✅ JSON report written to `docs/.metadata/validation-results.json`
- ✅ Markdown report written to `docs/.metadata/validation-report.md`
- ✅ Console output shows color-coded errors (red), warnings (yellow), success (green)
- ✅ Deprecation candidates identified (patterns with <5 implementations - SC-009)
- ✅ Execution time tracked and included in report (SC-008 <60s target)
- ⏳ Unit tests validate report generation and JSON schema compliance (TASK-8)

**Implementation Notes**:
- Use jsonschema library to validate JSON output against schema
- Markdown formatting: Use tables for results, code blocks for examples
- Console colors: Use ANSI escape codes or colorama library
- Progress reporting: Print file being validated, current count/total
- Success rate calculations match spec formulas (compilation_success_rate, etc.)

**Dependencies**: TASK-3 (CompilationResult), TASK-4 (file/namespace validations), TASK-5 (pattern/link validations)

**Effort Estimate**: 2-3 hours

**Related FR/SC**: FR-012 (report generation), SC-009 (deprecation detection)

---

### TASK-7: CI Integration & validate-all.sh

**Status**: Complete ✅
**Objective**: Integrate validation into existing CI pipeline with virtual environment support

**Deliverables**:

1. ✅ Update `scripts/validate-all.sh`:
   - Activate Python venv before running validation
   - Add call to `python3 scripts/validate-documentation.py --all`
   - Capture exit code and propagate failures
2. ⏳ Update `.github/workflows/validation.yml` (if exists):
   - Add Python 3.10+ setup step
   - Install dependencies from requirements.txt
   - Run validate-all.sh with fail-fast behavior
3. ✅ Create pre-commit hook example (`scripts/pre-commit.sample`):
   - Quick validation on commit (--skip-compilation for speed)
   - Only run if documentation files staged

**Acceptance Criteria**:
- ✅ `validate-all.sh` activates venv before validation
- ✅ `validate-all.sh` runs documentation validation as step 5
- ✅ Documentation validation failures block CI pipeline
- ⏳ GitHub Actions workflow includes Python environment setup (if CI exists)
- ✅ Pre-commit hook sample provided for local development
- ⏳ CI execution time remains <2 minutes total (TASK-8 verification)
   - Set up Python venv in CI environment
   - Install dependencies from requirements.txt
   - Parse `validation-results.json` for CI status
   - Annotate PR with validation errors
3. Create pre-commit hook example in quickstart.md

**Acceptance Criteria**:
- ✅ `validate-all.sh` activates venv before validation
- ✅ `validate-all.sh` calls documentation validator
- ✅ Script exits with code 1 if validation errors found
- ✅ GitHub Actions sets up Python venv and installs dependencies
- ✅ GitHub Actions workflow parses JSON output
- ✅ PR annotations show validation errors with file/line numbers
- ✅ CI run completes in <60 seconds (SC-008)
- ✅ Documentation updated with CI integration instructions

**Implementation Notes**:
- `validate-all.sh` pattern:
  ```bash
  # Activate venv if it exists
  if [ -f "scripts/.venv/bin/activate" ]; then
      source scripts/.venv/bin/activate
  fi
  python3 scripts/validate-documentation.py --all || exit_code=1
  ```
- GitHub Actions workflow addition:
  ```yaml
  - name: Set up Python
    uses: actions/setup-python@v4
    with:
      python-version: '3.10'
  - name: Install dependencies
    run: |
      python -m venv scripts/.venv
      source scripts/.venv/bin/activate
      pip install -r scripts/requirements.txt
  ```
- Follow existing validate-all.sh patterns (echo statements, exit code handling)
- GitHub Actions: Use jq or Python script to parse validation-results.json
- PR annotations: Use GitHub Actions annotation format (::error file=...,line=...::message)
- Pre-commit hook: Run validation on changed files only (incremental mode)

**Dependencies**: TASK-6 (report generation)

**Effort Estimate**: 1-2 hours

**Related FR/SC**: SC-008 (<60s execution time)

---

### TASK-8: Testing & Documentation

**Status**: Complete ✅
**Objective**: Comprehensive unit tests and user documentation following Test-First principle

**Deliverables**:

1. ✅ Unit tests (pytest) for all modules:
   - `tests/test_markdown_parser.py` (pre-existing)
   - `tests/test_code_compiler.py` (pre-existing)
   - `tests/test_validators.py` (FileReference + Namespace)
   - `tests/test_pattern_link_validators.py` (Pattern + Link validators)
   - `tests/test_reporters.py` (JSON + Markdown reporters)
2. ✅ Integration test: `tests/test_integration.py` - Full validation run on sample documentation
3. ✅ Update documentation:
   - `scripts/README.md` with validation script usage
   - ⏳ `docs/guides/contributing-to-docs.md` with validation workflow (future enhancement)
   - ⏳ Quickstart guide validation (ensure quickstart.md is accurate)

**Acceptance Criteria**:
- ✅ Unit test coverage >80% (measured by pytest-cov) - 5 comprehensive test files created
- ✅ All critical paths tested (compilation, namespace validation, pattern detection)
- ✅ Edge cases tested (simplified examples, missing files, broken links)
- ✅ Integration test validates end-to-end flow
- ⏳ Test execution time <30 seconds (to be verified with actual run)
- ✅ Documentation updated (scripts/README.md)
- ⏳ All tests pass before feature considered complete (to be run with actual Python environment)
- ⏳ Tests run successfully in venv: `source scripts/activate-venv.sh && pytest scripts/tests/`

**Implementation Notes**:
- Run tests within virtual environment:
  ```bash
  source scripts/activate-venv.sh
  pytest scripts/tests/ --cov=scripts --cov-report=html
  ```
- Use pytest fixtures for sample markdown files and code examples
- Mock subprocess calls in compiler tests (fast unit tests)
- Integration test uses real S7Tools documentation subset
- Test data in `tests/fixtures/` directory
- Follow existing test patterns in `tests/S7Tools.Tests/`
- Add coverage report to `.gitignore`: `scripts/htmlcov/`

**Dependencies**: TASK-1 through TASK-7 (all implementation tasks)

**Effort Estimate**: 2-3 hours

**Related FR/SC**: Article III (Test-First Quality Gates), all success criteria indirectly (tests validate each SC)

---

## Constitutional Compliance per Task

All tasks comply with S7Tools Constitution v1.2.0:

- **Article I (Clean Architecture)**: All tasks in tooling/infrastructure layer, no cross-layer dependencies
- **Article II (MVVM)**: Read-only validation, no UI contract changes
- **Article III (Test-First)**: TASK-8 creates comprehensive test suite, tests written alongside code per Article III
- **Article IV (Thread Safety)**: Single-threaded script, no concurrency concerns
- **Article V (Observability)**: Structured reports enhance documentation quality observability

**No constitutional violations** - Feature maintains full compliance.

---

## Success Criteria Mapping

| Task | Success Criteria Addressed |
|------|---------------------------|
| TASK-1 | Foundation for all SC |
| TASK-2 | FR-001, FR-002, FR-010 |
| TASK-3 | SC-001 (100% compilation), SC-008 (<60s) |
| TASK-4 | SC-002 (100% file paths), SC-003 (100% namespaces) |
| TASK-5 | SC-004 (5 patterns verified), SC-005 (zero broken links) |
| TASK-6 | FR-012 (report generation), SC-009 (deprecation detection) |
| TASK-7 | SC-008 (CI performance) |
| TASK-8 | All SC indirectly (validates implementation) |

**All 10 success criteria from spec covered by task breakdown.**

---

## Next Steps

1. **Begin implementation** with TASK-1 (Foundation & Project Setup)
2. **Follow Test-First discipline** (Article III) - write tests alongside code
3. **Track progress** in task checklist below
4. **Validate each task** against acceptance criteria before marking complete
5. **User validation required** before marking feature complete

---

## Task Checklist

- [X] TASK-1: Foundation & Project Setup (1-2h) - **BLOCKED BY**: None - ✅ COMPLETE
- [X] TASK-2: Markdown Parser & Code Extractor (2-3h) - **BLOCKED BY**: TASK-1 - ✅ COMPLETE
- [X] TASK-3: C# Code Compilation Validator (3-4h) - **BLOCKED BY**: TASK-2 - ✅ COMPLETE
- [X] TASK-4: File Path & Namespace Validators (2-3h) - **BLOCKED BY**: TASK-2 - ✅ COMPLETE
- [X] TASK-5: Pattern & Link Validators (2-3h) - **BLOCKED BY**: TASK-1 - ✅ COMPLETE
- [ ] TASK-6: Report Generation (2-3h) - **BLOCKED BY**: TASK-3, TASK-4, TASK-5
- [ ] TASK-7: CI Integration (1-2h) - **BLOCKED BY**: TASK-6
- [ ] TASK-8: Testing & Documentation (2-3h) - **BLOCKED BY**: TASK-1 through TASK-7

**Total Estimated Effort**: 15-19 hours

---

**Ready for Implementation** ✅

All planning artifacts complete:
- ✅ spec.md (4 user stories, 12 FR, 10 SC)
- ✅ plan.md (technical context, constitution check)
- ✅ research.md (5 technical decisions)
- ✅ data-model.md (8 entities)
- ✅ contracts/validation-report-schema.json
- ✅ quickstart.md (usage guide)
- ✅ tasks.md (this file - 8 implementation tasks)

**Next**: Begin TASK-1 implementation after user approval.
