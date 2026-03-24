"""
Unit tests for core data model entities.

Tests basic entity instantiation, validation rules, and business logic.
"""

import pytest
from datetime import datetime
from pathlib import Path
import tempfile
import os

# Add parent directory to path for imports
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import (
    DocumentationFile,
    CodeExample,
    CompilationResult,
    FilePathReference,
    NamespaceValidation,
    PatternImplementation,
    EditorConfigRule,
    ValidationReport
)


class TestDocumentationFile:
    """Tests for DocumentationFile entity."""

    def test_valid_documentation_file(self, tmp_path):
        """Test creating valid DocumentationFile."""
        # Create temporary file
        test_file = tmp_path / "test.md"
        test_file.write_text("# Test Content")

        doc_file = DocumentationFile(
            path=test_file,
            relative_path="test.md",
            category="patterns",
            format="markdown",
            content="# Test Content",
            last_modified=datetime.now(),
            size_bytes=100,
            frontmatter={"title": "Test"}
        )

        assert doc_file.path == test_file
        assert doc_file.category == "patterns"
        assert doc_file.format == "markdown"

    def test_invalid_category(self, tmp_path):
        """Test that invalid category raises ValueError."""
        test_file = tmp_path / "test.md"
        test_file.write_text("Test")

        with pytest.raises(ValueError, match="Category must be one of"):
            DocumentationFile(
                path=test_file,
                relative_path="test.md",
                category="invalid_category",
                format="markdown",
                content="Test",
                last_modified=datetime.now(),
                size_bytes=4
            )

    def test_invalid_format(self, tmp_path):
        """Test that invalid format raises ValueError."""
        test_file = tmp_path / "test.md"
        test_file.write_text("Test")

        with pytest.raises(ValueError, match="Format must be one of"):
            DocumentationFile(
                path=test_file,
                relative_path="test.md",
                category="patterns",
                format="invalid_format",
                content="Test",
                last_modified=datetime.now(),
                size_bytes=4
            )


class TestCodeExample:
    """Tests for CodeExample entity."""

    def test_valid_code_example(self):
        """Test creating valid CodeExample."""
        example = CodeExample(
            source_file="docs/patterns/test.md",
            line_number=42,
            language="csharp",
            code="public class Test { }",
            is_simplified=False,
            required_usings=["System"]
        )

        assert example.source_file == "docs/patterns/test.md"
        assert example.line_number == 42
        assert example.language == "csharp"

    def test_invalid_language(self):
        """Test that invalid language raises ValueError."""
        with pytest.raises(ValueError, match="Language must be one of"):
            CodeExample(
                source_file="test.md",
                line_number=1,
                language="invalid_language",
                code="test code"
            )

    def test_empty_code(self):
        """Test that empty code raises ValueError."""
        with pytest.raises(ValueError, match="Code must be non-empty"):
            CodeExample(
                source_file="test.md",
                line_number=1,
                language="csharp",
                code=""
            )

    def test_invalid_line_number(self):
        """Test that invalid line number raises ValueError."""
        with pytest.raises(ValueError, match="Line number must be > 0"):
            CodeExample(
                source_file="test.md",
                line_number=0,
                language="csharp",
                code="test code"
            )


class TestCompilationResult:
    """Tests for CompilationResult entity."""

    def test_successful_compilation(self):
        """Test creating successful CompilationResult."""
        result = CompilationResult(
            success=True,
            errors=[],
            warnings=[],
            execution_time_ms=523,
            exit_code=0
        )

        assert result.success is True
        assert len(result.errors) == 0
        assert result.exit_code == 0

    def test_failed_compilation(self):
        """Test creating failed CompilationResult."""
        result = CompilationResult(
            success=False,
            errors=["CS0246: Type not found"],
            warnings=["CS0168: Variable declared but not used"],
            execution_time_ms=450,
            exit_code=1
        )

        assert result.success is False
        assert len(result.errors) == 1
        assert result.exit_code == 1


class TestFilePathReference:
    """Tests for FilePathReference entity."""

    def test_valid_file_reference(self):
        """Test creating valid FilePathReference."""
        ref = FilePathReference(
            source_file="docs/test.md",
            line_number=10,
            referenced_path="src/S7Tools/Models/Test.cs",
            path_type="project_relative",
            exists=True,
            resolved_path="/full/path/to/Test.cs"
        )

        assert ref.path_type == "project_relative"
        assert ref.exists is True

    def test_invalid_path_type(self):
        """Test that invalid path type raises ValueError."""
        with pytest.raises(ValueError, match="Path type must be one of"):
            FilePathReference(
                source_file="test.md",
                line_number=1,
                referenced_path="test.cs",
                path_type="invalid_type",
                exists=False
            )


class TestNamespaceValidation:
    """Tests for NamespaceValidation entity."""

    def test_compliant_namespace(self):
        """Test creating compliant NamespaceValidation."""
        validation = NamespaceValidation(
            source_file="src/S7Tools/ViewModels/Pages/Home.cs",
            declared_namespace="S7Tools.ViewModels.Pages",
            expected_pattern="S7Tools.ViewModels.{Category}",
            is_compliant=True,
            category="Pages"
        )

        assert validation.is_compliant is True
        assert validation.category == "Pages"

    def test_invalid_category(self):
        """Test that invalid category raises ValueError."""
        with pytest.raises(ValueError, match="Category must be one of"):
            NamespaceValidation(
                source_file="test.cs",
                declared_namespace="Test",
                expected_pattern="Test.{Category}",
                is_compliant=False,
                category="InvalidCategory"
            )


class TestPatternImplementation:
    """Tests for PatternImplementation entity."""

    def test_verified_pattern(self):
        """Test creating verified PatternImplementation."""
        pattern = PatternImplementation(
            pattern_name="Unified Profile Management",
            documented_location="docs/patterns/profile-management.md",
            expected_files=["src/S7Tools/Services/StandardProfileManager.cs"],
            found_files=["src/S7Tools/Services/StandardProfileManager.cs"],
            is_verified=True,
            missing_files=[],
            extra_files=[]
        )

        assert pattern.is_verified is True
        assert len(pattern.missing_files) == 0


class TestEditorConfigRule:
    """Tests for EditorConfigRule entity."""

    def test_consistent_rule(self):
        """Test creating consistent EditorConfigRule."""
        rule = EditorConfigRule(
            section="*.cs",
            rule_name="indent_size",
            rule_value="4",
            is_consistent=True,
            documented_value="4"
        )

        assert rule.is_consistent is True
        assert rule.rule_value == rule.documented_value


class TestValidationReport:
    """Tests for ValidationReport entity."""

    def test_validation_report_creation(self):
        """Test creating ValidationReport."""
        report = ValidationReport(
            generated_at=datetime.now(),
            validation_version="1.0.0",
            total_files_checked=10,
            total_errors=0,
            total_warnings=2,
            code_examples=[],
            compilation_success_rate=100.0,
            file_references=[],
            file_reference_success_rate=100.0,
            namespace_validations=[],
            namespace_compliance_rate=100.0,
            pattern_implementations=[],
            pattern_verification_rate=100.0,
            editorconfig_discrepancies=[],
            broken_links=[],
            execution_time_seconds=45.3,
            summary="All validation checks passed"
        )

        assert report.total_files_checked == 10
        assert report.compilation_success_rate == 100.0

    def test_passes_success_criteria_all_pass(self):
        """Test success criteria check when all criteria pass."""
        report = ValidationReport(
            generated_at=datetime.now(),
            validation_version="1.0.0",
            total_files_checked=10,
            total_errors=0,
            total_warnings=0,
            code_examples=[],
            compilation_success_rate=100.0,
            file_references=[],
            file_reference_success_rate=100.0,
            namespace_validations=[],
            namespace_compliance_rate=100.0,
            pattern_implementations=[],
            pattern_verification_rate=100.0,
            editorconfig_discrepancies=[],
            broken_links=[],
            execution_time_seconds=45.3,
            summary="All validation checks passed"
        )

        assert report.passes_success_criteria() is True
        assert report.get_status() == "✅ PASS"

    def test_passes_success_criteria_compilation_fail(self):
        """Test success criteria check when compilation fails."""
        report = ValidationReport(
            generated_at=datetime.now(),
            validation_version="1.0.0",
            total_files_checked=10,
            total_errors=1,
            total_warnings=0,
            code_examples=[],
            compilation_success_rate=95.0,  # Failed
            file_references=[],
            file_reference_success_rate=100.0,
            namespace_validations=[],
            namespace_compliance_rate=100.0,
            pattern_implementations=[],
            pattern_verification_rate=100.0,
            editorconfig_discrepancies=[],
            broken_links=[],
            execution_time_seconds=45.3,
            summary="Compilation failed"
        )

        assert report.passes_success_criteria() is False
        assert report.get_status() == "❌ FAIL"

    def test_passes_success_criteria_timeout(self):
        """Test success criteria check when execution times out."""
        report = ValidationReport(
            generated_at=datetime.now(),
            validation_version="1.0.0",
            total_files_checked=10,
            total_errors=0,
            total_warnings=0,
            code_examples=[],
            compilation_success_rate=100.0,
            file_references=[],
            file_reference_success_rate=100.0,
            namespace_validations=[],
            namespace_compliance_rate=100.0,
            pattern_implementations=[],
            pattern_verification_rate=100.0,
            editorconfig_discrepancies=[],
            broken_links=[],
            execution_time_seconds=65.0,  # Exceeded timeout
            summary="Execution timeout"
        )

        assert report.passes_success_criteria() is False
        assert report.get_status() == "❌ FAIL"


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
