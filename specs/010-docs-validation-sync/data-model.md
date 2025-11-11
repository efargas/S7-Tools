# Data Model: Documentation Validation

**Feature**: Documentation Validation and Synchronization
**Branch**: `010-docs-validation-sync`
**Date**: 2025-11-11

## Overview

This document defines the core entities used in the documentation validation system. These entities represent validation inputs, intermediate results, and final outputs.

---

## Core Entities

### 1. DocumentationFile

**Purpose**: Represents a single documentation file to be validated

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `path` | `str` (pathlib.Path) | ✅ | Absolute path to documentation file |
| `relative_path` | `str` | ✅ | Path relative to docs/ root |
| `category` | `str` | ✅ | Category (architecture, patterns, guides, templates, reviews) |
| `format` | `str` | ✅ | File format (markdown, resx, json) |
| `content` | `str` | ✅ | Raw file content |
| `frontmatter` | `dict` | ❌ | YAML frontmatter metadata (if present) |
| `last_modified` | `datetime` | ✅ | File modification timestamp |
| `size_bytes` | `int` | ✅ | File size in bytes |

**Validation Rules**:
- `path` must exist in filesystem
- `category` must be one of: `architecture`, `patterns`, `guides`, `templates`, `reviews`, `archive`
- `format` must be one of: `markdown`, `resx`, `json`, `xml`

**Example**:
```python
DocumentationFile(
    path=Path("/home/user/S7-Tools/docs/patterns/profile-management.md"),
    relative_path="patterns/profile-management.md",
    category="patterns",
    format="markdown",
    content="---\ntitle: Profile Management\n...",
    frontmatter={"title": "Profile Management", "version": "1.0.0"},
    last_modified=datetime(2025, 11, 10, 14, 30),
    size_bytes=12845
)
```

---

### 2. CodeExample

**Purpose**: Represents a C# code block extracted from documentation

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `source_file` | `str` | ✅ | Documentation file containing the example |
| `line_number` | `int` | ✅ | Line number where code block starts |
| `language` | `str` | ✅ | Programming language (csharp, python, bash) |
| `code` | `str` | ✅ | Extracted code content |
| `context` | `str` | ❌ | Surrounding text explaining the example |
| `is_simplified` | `bool` | ✅ | Flag indicating simplified/incomplete example |
| `required_usings` | `list[str]` | ❌ | Required using statements for compilation |
| `compilation_result` | `CompilationResult` | ❌ | Result after compilation attempt |

**Validation Rules**:
- `language` must be one of: `csharp`, `python`, `bash`, `xml`
- `code` must be non-empty
- `line_number` must be > 0

**Example**:
```python
CodeExample(
    source_file="docs/patterns/profile-management.md",
    line_number=145,
    language="csharp",
    code="public class MyService : IProfileManager<T> { ... }",
    context="Example implementation of profile manager",
    is_simplified=False,
    required_usings=["System", "S7Tools.Core.Services.Interfaces"],
    compilation_result=CompilationResult(success=True, errors=[])
)
```

---

### 3. CompilationResult

**Purpose**: Represents the outcome of compiling a code example

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `success` | `bool` | ✅ | True if compilation succeeded |
| `errors` | `list[str]` | ✅ | Compilation error messages (empty if success) |
| `warnings` | `list[str]` | ✅ | Compilation warnings |
| `execution_time_ms` | `int` | ✅ | Time taken to compile (milliseconds) |
| `exit_code` | `int` | ✅ | dotnet build exit code (0 = success) |

**Example**:
```python
CompilationResult(
    success=False,
    errors=["CS0246: The type or namespace name 'IProfileManager' could not be found"],
    warnings=[],
    execution_time_ms=523,
    exit_code=1
)
```

---

### 4. FilePathReference

**Purpose**: Represents a file path mentioned in documentation

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `source_file` | `str` | ✅ | Documentation file containing the reference |
| `line_number` | `int` | ✅ | Line number of reference |
| `referenced_path` | `str` | ✅ | The path mentioned in documentation |
| `path_type` | `str` | ✅ | Type (absolute, relative, project-relative) |
| `exists` | `bool` | ✅ | True if path exists in repository |
| `resolved_path` | `str` | ❌ | Absolute resolved path (if exists) |

**Validation Rules**:
- `path_type` must be one of: `absolute`, `relative`, `project_relative`
- `resolved_path` only set if `exists = True`

**Example**:
```python
FilePathReference(
    source_file="docs/architecture/overview.md",
    line_number=89,
    referenced_path="src/S7Tools/Services/StandardProfileManager.cs",
    path_type="project_relative",
    exists=True,
    resolved_path="/home/user/S7-Tools/src/S7Tools/Services/StandardProfileManager.cs"
)
```

---

### 5. NamespaceValidation

**Purpose**: Represents validation of namespace conventions

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `source_file` | `str` | ✅ | Source code file being validated |
| `declared_namespace` | `str` | ✅ | Namespace declared in source file |
| `expected_pattern` | `str` | ✅ | Expected pattern from documentation |
| `category` | `str` | ❌ | Category (Base, Controls, Dialogs, etc.) |
| `is_compliant` | `bool` | ✅ | True if namespace matches expected pattern |
| `violation_details` | `str` | ❌ | Description of violation (if not compliant) |

**Validation Rules**:
- ViewModels must match: `S7Tools.ViewModels.{Category}`
- Views must match: `S7Tools.Views.{Category}`
- Category must be one of: `Base`, `Controls`, `Dialogs`, `Jobs`, `Layout`, `Pages`, `Profiles`, `Settings`, `Tasks`

**Example**:
```python
NamespaceValidation(
    source_file="src/S7Tools/ViewModels/Pages/HomeViewModel.cs",
    declared_namespace="S7Tools.ViewModels.Pages",
    expected_pattern="S7Tools.ViewModels.{Category}",
    category="Pages",
    is_compliant=True,
    violation_details=None
)
```

---

### 6. PatternImplementation

**Purpose**: Represents verification that a documented pattern exists in codebase

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `pattern_name` | `str` | ✅ | Name of architectural pattern |
| `documented_location` | `str` | ✅ | Where pattern is documented |
| `expected_files` | `list[str]` | ✅ | Files that should implement pattern |
| `found_files` | `list[str]` | ✅ | Files actually found implementing pattern |
| `is_verified` | `bool` | ✅ | True if pattern found as documented |
| `missing_files` | `list[str]` | ✅ | Expected files not found |
| `extra_files` | `list[str]` | ✅ | Found files not documented |

**Validation Rules**:
- `pattern_name` must be one of 5 core patterns from spec (Profile Management, Internal Method, Resource Coordination, Custom Exceptions, Reusable Controls)
- `is_verified = True` only if `missing_files` is empty

**Example**:
```python
PatternImplementation(
    pattern_name="Unified Profile Management",
    documented_location="docs/patterns/profile-management.md",
    expected_files=[
        "src/S7Tools/Services/StandardProfileManager.cs",
        "src/S7Tools.Core/Services/Interfaces/IProfileManager.cs"
    ],
    found_files=[
        "src/S7Tools/Services/StandardProfileManager.cs",
        "src/S7Tools.Core/Services/Interfaces/IProfileManager.cs"
    ],
    is_verified=True,
    missing_files=[],
    extra_files=[]
)
```

---

### 7. ValidationReport

**Purpose**: Aggregated results of all validation checks

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `generated_at` | `datetime` | ✅ | Report generation timestamp |
| `validation_version` | `str` | ✅ | Version of validation script |
| `total_files_checked` | `int` | ✅ | Number of documentation files validated |
| `total_errors` | `int` | ✅ | Total error count |
| `total_warnings` | `int` | ✅ | Total warning count |
| `code_examples` | `list[CodeExample]` | ✅ | All extracted code examples |
| `compilation_success_rate` | `float` | ✅ | Percentage of examples that compiled |
| `file_references` | `list[FilePathReference]` | ✅ | All file path references |
| `file_reference_success_rate` | `float` | ✅ | Percentage of valid file paths |
| `namespace_validations` | `list[NamespaceValidation]` | ✅ | All namespace checks |
| `namespace_compliance_rate` | `float` | ✅ | Percentage of compliant namespaces |
| `pattern_implementations` | `list[PatternImplementation]` | ✅ | All pattern verifications |
| `pattern_verification_rate` | `float` | ✅ | Percentage of verified patterns |
| `editorconfig_discrepancies` | `list[dict]` | ✅ | Differences between .editorconfig and docs |
| `broken_links` | `list[dict]` | ✅ | Internal documentation links that 404 |
| `execution_time_seconds` | `float` | ✅ | Total validation execution time |
| `summary` | `str` | ✅ | Human-readable summary |

**Success Criteria** (from spec):
- `compilation_success_rate >= 100%` (SC-001)
- `file_reference_success_rate >= 100%` (SC-002)
- `namespace_compliance_rate >= 100%` (SC-003)
- `pattern_verification_rate >= 100%` (all 5 patterns verified - SC-004)
- `len(broken_links) == 0` (SC-005)
- `len(editorconfig_discrepancies) == 0` (SC-007)
- `execution_time_seconds < 60` (SC-008)

**Example**:
```python
ValidationReport(
    generated_at=datetime(2025, 11, 11, 10, 30),
    validation_version="1.0.0",
    total_files_checked=52,
    total_errors=3,
    total_warnings=7,
    code_examples=[...],  # List of CodeExample objects
    compilation_success_rate=97.5,
    file_references=[...],
    file_reference_success_rate=100.0,
    namespace_validations=[...],
    namespace_compliance_rate=100.0,
    pattern_implementations=[...],
    pattern_verification_rate=100.0,
    editorconfig_discrepancies=[],
    broken_links=[],
    execution_time_seconds=45.3,
    summary="Validation completed with 3 errors and 7 warnings. See report for details."
)
```

---

### 8. EditorConfigRule

**Purpose**: Represents a single .editorconfig rule to compare with docs

**Attributes**:

| Attribute | Type | Required | Description |
|-----------|------|----------|-------------|
| `section` | `str` | ✅ | File pattern (e.g., "*.cs", "*.axaml") |
| `rule_name` | `str` | ✅ | Rule identifier (e.g., "indent_size") |
| `rule_value` | `str` | ✅ | Configured value (e.g., "4", "true") |
| `documented_value` | `str` | ❌ | Value mentioned in docs/guides/code-style.md |
| `is_consistent` | `bool` | ✅ | True if .editorconfig matches documentation |

**Example**:
```python
EditorConfigRule(
    section="*.cs",
    rule_name="indent_size",
    rule_value="4",
    documented_value="4",
    is_consistent=True
)
```

---

## Entity Relationships

```
ValidationReport (1)
  ├── code_examples (many) → CodeExample
  │     └── compilation_result (1) → CompilationResult
  ├── file_references (many) → FilePathReference
  ├── namespace_validations (many) → NamespaceValidation
  ├── pattern_implementations (many) → PatternImplementation
  └── editorconfig_discrepancies (many) → EditorConfigRule
```

---

## Data Flow

1. **Input**: Scan `docs/` directory → Create `DocumentationFile` entities
2. **Extraction**: Parse markdown → Create `CodeExample`, `FilePathReference` entities
3. **Validation**:
   - Compile code examples → Create `CompilationResult` entities
   - Check file paths → Update `FilePathReference.exists`
   - Validate namespaces → Create `NamespaceValidation` entities
   - Verify patterns → Create `PatternImplementation` entities
   - Compare .editorconfig → Create `EditorConfigRule` entities
4. **Aggregation**: Collect all entities → Create `ValidationReport`
5. **Output**: Serialize `ValidationReport` → JSON + Markdown files

---

## Next Steps

1. Generate JSON schema for `ValidationReport` entity (contracts/)
2. Create quickstart guide for running validation
3. Update agent context with validation data model
