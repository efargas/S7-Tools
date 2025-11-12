"""Unit tests for JSONReporter and MarkdownReporter

Tests report generation for both machine-readable and human-readable formats.
"""

import pytest
import json
from pathlib import Path
from datetime import datetime
from reporters.json_reporter import JSONReporter
from reporters.markdown_reporter import MarkdownReporter
from entities import (
    ValidationReport,
    CodeExample,
    CompilationResult,
    FilePathReference,
    NamespaceValidation,
    PatternImplementation,
    InternalLink
)


@pytest.fixture
def sample_report():
    """Create sample ValidationReport for testing."""
    return ValidationReport(
        generated_at=datetime(2025, 1, 15, 10, 30, 0),
        validation_version="1.0.0",
        total_files_checked=10,
        total_errors=2,
        total_warnings=1,
        code_examples=[
            CodeExample(
                code="public class Test {}",
                language="csharp",
                source_file="docs/test.md",
                line_number=10,
                is_simplified=False,
                usings=[],
                compilation_result=CompilationResult(
                    success=True,
                    error_code="",
                    error_message="",
                    compilation_time_ms=150
                )
            ),
            CodeExample(
                code="public class Broken {}",
                language="csharp",
                source_file="docs/broken.md",
                line_number=20,
                is_simplified=False,
                usings=[],
                compilation_result=CompilationResult(
                    success=False,
                    error_code="CS0246",
                    error_message="The type or namespace name 'Missing' could not be found",
                    compilation_time_ms=200
                )
            ),
        ],
        compilation_success_rate=50.0,
        file_references=[
            FilePathReference(
                referenced_path="src/Exists.cs",
                source_file="docs/test.md",
                line_number=5,
                exists=True,
                resolved_path=Path("/workspace/src/Exists.cs")
            ),
            FilePathReference(
                referenced_path="src/Missing.cs",
                source_file="docs/test.md",
                line_number=6,
                exists=False
            ),
        ],
        file_reference_success_rate=50.0,
        namespace_validations=[
            NamespaceValidation(
                file_path="src/ViewModels/Pages/HomeViewModel.cs",
                actual_namespace="S7Tools.ViewModels.Pages",
                expected_namespace="S7Tools.ViewModels.Pages",
                category="Pages",
                is_compliant=True
            ),
        ],
        namespace_compliance_rate=100.0,
        pattern_implementations=[
            PatternImplementation(
                pattern_name="Profile Management",
                documentation_path="docs/patterns/profile-management.md",
                is_verified=True,
                implementation_files=["src/Services/StandardProfileManager.cs"]
            ),
        ],
        pattern_verification_rate=100.0,
        editorconfig_discrepancies=[],
        broken_links=[
            InternalLink(
                target="missing.md",
                source_file="docs/test.md",
                line_number=30
            ),
        ],
        execution_time_seconds=45.5,
        summary="Validation completed with 2 errors and 1 warnings."
    )


class TestJSONReporter:
    """Test suite for JSONReporter."""

    @pytest.fixture
    def reporter(self):
        """Create JSONReporter instance."""
        return JSONReporter()

    def test_generate_json_report(self, reporter, sample_report):
        """Test generating JSON report string."""
        json_str = reporter.generate_json_report(sample_report)

        # Should be valid JSON
        data = json.loads(json_str)

        # Check top-level fields
        assert data["validation_version"] == "1.0.0"
        assert data["total_files_checked"] == 10
        assert data["total_errors"] == 2
        assert data["total_warnings"] == 1

    def test_serialize_code_example(self, reporter):
        """Test serializing CodeExample entity."""
        example = CodeExample(
            code="public class Test {}",
            language="csharp",
            source_file="test.md",
            line_number=10,
            is_simplified=False,
            usings=["System"],
            compilation_result=CompilationResult(
                success=True,
                error_code="",
                error_message="",
                compilation_time_ms=100
            )
        )

        serialized = reporter._serialize_code_example(example)

        assert serialized["code"] == "public class Test {}"
        assert serialized["language"] == "csharp"
        assert serialized["source_file"] == "test.md"
        assert serialized["line_number"] == 10
        assert serialized["is_simplified"] is False
        assert "System" in serialized["usings"]
        assert serialized["compilation_result"]["success"] is True

    def test_serialize_compilation_result(self, reporter):
        """Test serializing CompilationResult entity."""
        result = CompilationResult(
            success=False,
            error_code="CS0246",
            error_message="Type not found",
            compilation_time_ms=150
        )

        serialized = reporter._serialize_compilation_result(result)

        assert serialized["success"] is False
        assert serialized["error_code"] == "CS0246"
        assert serialized["error_message"] == "Type not found"
        assert serialized["compilation_time_ms"] == 150

    def test_write_json_report(self, reporter, sample_report, tmp_path):
        """Test writing JSON report to file."""
        output_path = tmp_path / "validation-results.json"
        reporter.write_json_report(sample_report, output_path)

        # File should exist
        assert output_path.exists()

        # Should be valid JSON
        with open(output_path) as f:
            data = json.load(f)
            assert data["total_errors"] == 2

    def test_json_schema_compliance(self, reporter, sample_report):
        """Test that generated JSON matches expected schema structure."""
        json_str = reporter.generate_json_report(sample_report)
        data = json.loads(json_str)

        # Check required fields exist
        required_fields = [
            "generated_at", "validation_version", "total_files_checked",
            "total_errors", "total_warnings", "code_examples",
            "compilation_success_rate", "file_references",
            "file_reference_success_rate", "namespace_validations",
            "namespace_compliance_rate", "pattern_implementations",
            "pattern_verification_rate", "broken_links",
            "execution_time_seconds", "summary"
        ]
        for field in required_fields:
            assert field in data


class TestMarkdownReporter:
    """Test suite for MarkdownReporter."""

    @pytest.fixture
    def reporter(self):
        """Create MarkdownReporter instance."""
        return MarkdownReporter()

    def test_generate_markdown_report(self, reporter, sample_report):
        """Test generating Markdown report string."""
        md_str = reporter.generate_markdown_report(sample_report)

        # Should contain key sections
        assert "# S7Tools Documentation Validation Report" in md_str
        assert "## Executive Summary" in md_str
        assert "## Code Example Compilation Results" in md_str
        assert "## File Reference Validation" in md_str
        assert "## Namespace Convention Compliance" in md_str
        assert "## Pattern Implementation Verification" in md_str
        assert "## Broken Internal Links" in md_str

    def test_generate_header(self, reporter, sample_report):
        """Test generating report header."""
        header = reporter._generate_header(sample_report)

        assert "# S7Tools Documentation Validation Report" in header
        assert "**Generated**: 2025-01-15" in header
        assert "**Version**: 1.0.0" in header
        assert "**Status**: ❌ FAILED" in header  # Has errors

    def test_generate_executive_summary(self, reporter, sample_report):
        """Test generating executive summary."""
        summary = reporter._generate_executive_summary(sample_report)

        assert "## Executive Summary" in summary
        assert "| Success Criterion |" in summary
        assert "| Code Compilation Success |" in summary
        assert "| 100% | 50.0% | ❌ |" in summary  # Expected vs Actual

    def test_generate_code_examples_section(self, reporter, sample_report):
        """Test generating code examples section."""
        section = reporter._generate_code_examples_section(sample_report)

        assert "## Code Example Compilation Results" in section
        assert "Total examples: 2" in section
        assert "Compilable: 2" in section
        assert "Successful: 1" in section
        assert "Failed: 1" in section
        assert "CS0246" in section  # Error code

    def test_generate_file_references_section(self, reporter, sample_report):
        """Test generating file references section."""
        section = reporter._generate_file_references_section(sample_report)

        assert "## File Reference Validation" in section
        assert "Total references: 2" in section
        assert "Valid: 1" in section
        assert "Broken: 1" in section
        assert "src/Missing.cs" in section

    def test_status_icon(self, reporter):
        """Test status icon generation."""
        assert reporter._status_icon(True) == "✅"
        assert reporter._status_icon(False) == "❌"

    def test_write_markdown_report(self, reporter, sample_report, tmp_path):
        """Test writing Markdown report to file."""
        output_path = tmp_path / "validation-report.md"
        reporter.write_markdown_report(sample_report, output_path)

        # File should exist
        assert output_path.exists()

        # Should contain expected content
        content = output_path.read_text()
        assert "# S7Tools Documentation Validation Report" in content

    def test_generate_recommendations(self, reporter, sample_report):
        """Test generating recommendations section."""
        section = reporter._generate_recommendations(sample_report)

        assert "## Recommendations" in section
        # Note: Sample report has all patterns verified, so no recommendations
        assert "No major issues detected" in section or "Pattern" in section
