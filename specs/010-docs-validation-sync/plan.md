# Implementation Plan: Documentation Validation and Synchronization

**Branch**: `010-docs-validation-sync` | **Date**: 2025-11-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-docs-validation-sync/spec.md`

**Note**: This plan implements automated validation of S7Tools documentation against source code to ensure patterns, examples, naming conventions, and templates accurately reflect the current codebase.

## Summary

**Primary Requirement**: Validate that all S7Tools documentation (architectural patterns, code examples, namespace conventions, templates, and style guidelines) accurately reflects the current codebase, ensuring developers and AI agents can confidently follow documented patterns without discovering mismatches.

**Technical Approach**: Python-based validation suite that:
1. Extracts and compiles C# code examples from markdown documentation
2. Verifies file path references exist in repository
3. Validates namespace conventions match documented patterns
4. Confirms architectural pattern implementations exist in documented locations
5. Validates internal documentation links
6. Compares .editorconfig rules to code-style.md
7. Generates comprehensive validation report with errors, warnings, and recommendations

## Technical Context

**Language/Version**: Python 3.10+ (aligns with existing scripts/validate-*.py)
**Primary Dependencies**:
- Markdown parser: `mistune` or `markdown` (Python stdlib)
- Code compilation: `subprocess` calling `dotnet build`
- File operations: `pathlib`, `os` (Python stdlib)
- Regex patterns: `re` (Python stdlib)
- Report generation: `jinja2` for HTML templates (optional)

**Storage**: N/A (reads documentation and source files, writes validation report to `docs/.metadata/validation-report.md`)
**Testing**: pytest (aligns with existing validation script testing patterns)
**Target Platform**: Linux/macOS/Windows (cross-platform Python script)
**Project Type**: Tooling/Validation script (single Python module)
**Performance Goals**:
- Full documentation scan in <60 seconds
- Incremental validation of changed files in <10 seconds
- Memory usage <200MB for full scan

**Constraints**:
- Must integrate with existing `scripts/validate-all.sh`
- Must produce machine-readable output for CI integration (JSON + Markdown)
- Must handle edge cases (simplified code examples, external dependencies)
- Must not modify source code or documentation (read-only validation)

**Scale/Scope**:
- ~50+ documentation files to validate
- ~500+ source files to scan for pattern compliance
- ~100+ code examples to extract and compile
- 5 core architectural patterns to verify

## Constitution Check

**Gate**: Must pass before Phase 0 research. Re-check after Phase 1 design.

**Constitution Version**: v1.2.0 (last amended 2025-11-07)

### Constitutional Compliance Assessment

**Article I (Clean Architecture & Layered Boundaries)**
✅ **PASS** - Validation script is tooling infrastructure that verifies documented dependency flow matches actual project references. Does not modify architecture or introduce cross-layer dependencies.

**Article II (MVVM, ReactiveUI & UI Contracts)**
✅ **PASS** - Validation checks that ViewModels inherit from ReactiveObject and follow documented patterns. Read-only verification with no UI contract modifications.

**Article III (Test-First Quality Gates - NON-NEGOTIABLE)**
✅ **PASS** - Validation verifies test templates compile and enforces quality through documentation accuracy checks. Enhances test-first discipline by ensuring documented patterns are correct. pytest tests required for validation logic.

**Article IV (Thread Safety & Concurrency Contracts)**
✅ **PASS** - Validation script verifies Internal Method Pattern usage in semaphore services. Single-threaded validation process with no concurrency concerns.

**Article V (Observability, Versioning & Simplicity)**
✅ **PASS** - Generates structured validation reports enhancing observability of documentation quality. Simple Python script following existing validation patterns in scripts/ directory.

### Compliance Checklist

- ✅ **Public Contracts**: No new public APIs or interfaces in application code (tooling only)
- ✅ **DI Registration**: No dependency injection changes (standalone script)
- ✅ **Cross-Cutting Changes**: No logging, threading, or error handling changes to application
- ✅ **Breaking Changes**: None - read-only validation, no code modifications
- ✅ **Test Coverage**: pytest tests for validation logic required (FR-012 in spec)
- ✅ **Documentation**: Updates docs/.metadata/ with validation reports

**Overall Assessment**: ✅ FULLY COMPLIANT - No constitutional violations. Feature enhances architectural governance through automated documentation validation.

### Post-Design Validation (Phase 1 Complete)

**Re-evaluated**: 2025-11-11 after Phase 1 (research, data-model, contracts, quickstart)

**Design Artifacts Review**:
- ✅ `research.md`: Technical decisions (mistune, dotnet CLI, validate-all.sh integration) - all align with simplicity principle
- ✅ `data-model.md`: 8 core entities (ValidationReport, CodeExample, CompilationResult, etc.) - pure data structures, no architectural violations
- ✅ `contracts/validation-report-schema.json`: JSON Schema v7 for ValidationReport - standard contract pattern
- ✅ `quickstart.md`: User guide for running validation - documentation only

**Constitutional Re-Check**:
- **Article I (Clean Architecture)**: ✅ PASS - Validation script stays in infrastructure/tooling layer, no cross-layer dependencies introduced
- **Article II (MVVM)**: ✅ PASS - No UI contracts modified, validation verifies ReactiveObject compliance
- **Article III (Test-First)**: ✅ PASS - Design includes pytest test structure, no feature code without tests
- **Article IV (Thread Safety)**: ✅ PASS - Single-threaded design, parallel compilation optional, no concurrency violations
- **Article V (Observability)**: ✅ PASS - JSON + Markdown reporting enhances observability, simple Python script follows existing patterns

**Complexity Assessment**:
- Single Python module with 5 validator submodules (extractors/, validators/, reporters/)
- Extends existing scripts/ infrastructure (no new projects)
- Reuses existing validation patterns (frontmatter, archive scripts)
- Output to existing docs/.metadata/ directory

**Final Assessment**: ✅ NO NEW VIOLATIONS - Design maintains full constitutional compliance. Ready for Phase 2 (Task Breakdown).


## Project Structure

### Documentation (this feature)

```
specs/010-docs-validation-sync/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0: Technical decisions (markdown parser, compilation approach)
├── data-model.md        # Phase 1: ValidationReport, CodeExample, PatternImplementation entities
├── quickstart.md        # Phase 1: How to run validation script and interpret results
├── contracts/           # Phase 1: JSON schema for validation report
│   └── validation-report-schema.json
└── tasks.md             # Phase 2: Task breakdown (/speckit.tasks command)
```

### Source Code (repository root)

**Structure Decision**: Single project (tooling script) - extends existing scripts/ directory

```
scripts/
├── validate-documentation.py    # NEW: Main validation orchestrator
├── extractors/                   # NEW: Code extraction modules
│   ├── __init__.py
│   ├── markdown_parser.py       # Extract C# code blocks from markdown
│   └── namespace_extractor.py   # Extract namespace declarations from source
├── validators/                   # NEW: Validation modules
│   ├── __init__.py
│   ├── code_compiler.py         # Compile extracted C# examples
│   ├── file_reference.py        # Verify file path references
│   ├── namespace_validator.py   # Check namespace conventions
│   ├── pattern_validator.py     # Verify pattern implementations
│   └── link_validator.py        # Check internal documentation links
├── reporters/                    # NEW: Report generation
│   ├── __init__.py
│   ├── markdown_reporter.py     # Generate validation-report.md
│   └── json_reporter.py         # Generate machine-readable JSON
└── tests/                        # NEW: pytest tests for validation
    ├── test_markdown_parser.py
    ├── test_code_compiler.py
    └── test_validators.py

docs/.metadata/                   # NEW: Validation outputs
└── validation-report.md          # Generated validation report
```

**Integration Points**:
- `scripts/validate-all.sh` - Add call to `validate-documentation.py`
- GitHub Actions CI - Parse JSON output for pass/fail status
- Existing validation infrastructure (frontmatter, links, archive scripts)

## Complexity Tracking

**Status**: N/A - No constitutional violations requiring justification.

All complexity decisions align with constitutional principles:
- Single Python script follows simplicity principle (Article V)
- No additional projects/layers needed
- Extends existing scripts/ infrastructure
- Read-only validation approach avoids architectural complexity

