"""
Core data model entities for S7Tools documentation validation.

This module defines the entity classes used throughout the validation system,
representing validation inputs, intermediate results, and final outputs.

Entity relationships:
    ValidationReport (1)
      ├── code_examples (many) → CodeExample
      │     └── compilation_result (1) → CompilationResult
      ├── file_references (many) → FilePathReference
      ├── namespace_validations (many) → NamespaceValidation
      ├── pattern_implementations (many) → PatternImplementation
      └── editorconfig_discrepancies (many) → EditorConfigRule
"""

from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Optional


@dataclass
class DocumentationFile:
    """Represents a single documentation file to be validated."""

    path: Path
    relative_path: str
    category: str
    format: str
    content: str
    last_modified: datetime
    size_bytes: int
    frontmatter: Optional[dict] = None

    def __post_init__(self):
        """Validate entity after initialization."""
        valid_categories = ["architecture", "patterns", "guides", "templates", "reviews", "archive"]
        valid_formats = ["markdown", "resx", "json", "xml"]

        if self.category not in valid_categories:
            raise ValueError(f"Category must be one of {valid_categories}, got '{self.category}'")
        if self.format not in valid_formats:
            raise ValueError(f"Format must be one of {valid_formats}, got '{self.format}'")
        if not self.path.exists():
            raise ValueError(f"Path does not exist: {self.path}")


@dataclass
class CompilationResult:
    """Represents the outcome of compiling a code example."""

    success: bool
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    execution_time_ms: int = 0
    exit_code: int = 0


@dataclass
class CodeExample:
    """Represents a C# code block extracted from documentation."""

    source_file: str
    line_number: int
    language: str
    code: str
    is_simplified: bool = False
    context: Optional[str] = None
    required_usings: list[str] = field(default_factory=list)
    compilation_result: Optional[CompilationResult] = None

    def __post_init__(self):
        """Validate entity after initialization."""
        valid_languages = ["csharp", "python", "bash", "xml", "json", "yaml"]

        if self.language not in valid_languages:
            raise ValueError(f"Language must be one of {valid_languages}, got '{self.language}'")
        if not self.code or not self.code.strip():
            raise ValueError("Code must be non-empty")
        if self.line_number <= 0:
            raise ValueError(f"Line number must be > 0, got {self.line_number}")


@dataclass
class FilePathReference:
    """Represents a file path mentioned in documentation."""

    source_file: str
    line_number: int
    referenced_path: str
    path_type: str
    exists: bool
    resolved_path: Optional[str] = None

    def __post_init__(self):
        """Validate entity after initialization."""
        valid_types = ["absolute", "relative", "project_relative"]

        if self.path_type not in valid_types:
            raise ValueError(f"Path type must be one of {valid_types}, got '{self.path_type}'")
        if self.line_number <= 0:
            raise ValueError(f"Line number must be > 0, got {self.line_number}")


@dataclass
class NamespaceValidation:
    """Represents validation of namespace conventions."""

    source_file: str
    declared_namespace: str
    expected_pattern: str
    is_compliant: bool
    category: Optional[str] = None
    violation_details: Optional[str] = None

    def __post_init__(self):
        """Validate entity after initialization."""
        valid_categories = [
            "Base", "Components", "Controls", "Dialogs", "Hex", "Jobs", "Layout",
            "Pages", "Profiles", "Settings", "Tasks"
        ]

        if self.category and self.category not in valid_categories:
            raise ValueError(f"Category must be one of {valid_categories}, got '{self.category}'")


@dataclass
class PatternImplementation:
    """Represents verification that a documented pattern exists in codebase."""

    pattern_name: str
    documented_location: str
    expected_files: list[str]
    found_files: list[str]
    is_verified: bool
    missing_files: list[str] = field(default_factory=list)
    extra_files: list[str] = field(default_factory=list)

    def __post_init__(self):
        """Validate entity after initialization."""
        core_patterns = [
            "Unified Profile Management",
            "Internal Method Pattern",
            "Resource Coordination",
            "Custom Exceptions",
            "Reusable Controls"
        ]

        if self.pattern_name not in core_patterns:
            # Allow custom patterns, but log warning
            pass


@dataclass
class EditorConfigRule:
    """Represents a single .editorconfig rule to compare with docs."""

    section: str
    rule_name: str
    rule_value: str
    is_consistent: bool
    documented_value: Optional[str] = None


@dataclass
class ValidationReport:
    """Aggregated results of all validation checks."""

    generated_at: datetime
    validation_version: str
    total_files_checked: int
    total_errors: int
    total_warnings: int
    code_examples: list[CodeExample]
    compilation_success_rate: float
    file_references: list[FilePathReference]
    file_reference_success_rate: float
    namespace_validations: list[NamespaceValidation]
    namespace_compliance_rate: float
    pattern_implementations: list[PatternImplementation]
    pattern_verification_rate: float
    editorconfig_discrepancies: list[EditorConfigRule]
    broken_links: list[dict]
    execution_time_seconds: float
    summary: str

    def passes_success_criteria(self) -> bool:
        """
        Check if validation meets all success criteria from spec.

        Success criteria:
        - SC-001: compilation_success_rate >= 100%
        - SC-002: file_reference_success_rate >= 100%
        - SC-003: namespace_compliance_rate >= 100%
        - SC-004: pattern_verification_rate >= 100% (all 5 patterns)
        - SC-005: len(broken_links) == 0
        - SC-007: len(editorconfig_discrepancies) == 0
        - SC-008: execution_time_seconds < 60
        """
        return (
            self.compilation_success_rate >= 100.0 and
            self.file_reference_success_rate >= 100.0 and
            self.namespace_compliance_rate >= 100.0 and
            self.pattern_verification_rate >= 100.0 and
            len(self.broken_links) == 0 and
            len(self.editorconfig_discrepancies) == 0 and
            self.execution_time_seconds < 60.0
        )

    def get_status(self) -> str:
        """Get validation status as string (PASS/FAIL)."""
        return "✅ PASS" if self.passes_success_criteria() else "❌ FAIL"
