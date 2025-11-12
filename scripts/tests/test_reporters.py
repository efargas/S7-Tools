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
    PatternImplementation
)


@pytest.fixture
def sample_report():
    """Create a sample ValidationReport for testing."""
    return ValidationReport(
        generated_at=datetime(2025, 1, 1, 12, 0, 0),
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
                compilation_result=CompilationResult(
                    success=True,
                    errors=[],
                    warnings=[],
                    execution_time_ms=150,
                    exit_code=0
                )
            ),
            CodeExample(
                code="public class Broken {}",
                language="csharp",
                source_file="docs/broken.md",
                line_number=20,
                is_simplified=False,
                compilation_result=CompilationResult(
                    success=False,
                    errors=["CS0246: The type or namespace name 'Missing' could not be found"],
                    warnings=[],
                    execution_time_ms=200,
                    exit_code=1
                )
            ),
        ],
        compilation_success_rate=50.0,
        file_references=[
            FilePathReference(
                source_file="docs/test.md",
                line_number=5,
                referenced_path="src/Exists.cs",
                path_type="project_relative",
                exists=True,
                resolved_path=str(Path("/workspace/src/Exists.cs"))
            ),
            FilePathReference(
                source_file="docs/test.md",
                line_number=6,
                referenced_path="src/Missing.cs",
                path_type="project_relative",
                exists=False
            ),
        ],
        file_reference_success_rate=50.0,
        namespace_validations=[
            NamespaceValidation(
                source_file="src/ViewModels/Pages/HomeViewModel.cs",
                declared_namespace="S7Tools.ViewModels.Pages",
                expected_pattern="S7Tools.ViewModels.Pages",
                is_compliant=True,
                category="Pages"
            ),
        ],
        namespace_compliance_rate=100.0,
        pattern_implementations=[
            PatternImplementation(
                pattern_name="Unified Profile Management",
                documented_location="docs/patterns/profile-management.md",
                expected_files=["src/Services/StandardProfileManager.cs"],
                found_files=["src/Services/StandardProfileManager.cs"],
                is_verified=True
            ),
        ],
        pattern_verification_rate=100.0,
        editorconfig_discrepancies=[],
        broken_links=[
            {
                "target_path": "missing.md",
                "source_file": "docs/test.md",
                "line_number": 30,
                "reason": "File not found"
            },
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
            required_usings=["System"],
            compilation_result=CompilationResult(
                success=True,
                errors=[],
                warnings=[],
                execution_time_ms=100,
                exit_code=0
            )
        )

        serialized = reporter._serialize_code_example(example)

        assert serialized["code"] == "public class Test {}"
        assert serialized["language"] == "csharp"
        assert serialized["source_file"] == "test.md"
        assert serialized["line_number"] == 10
        assert serialized["is_simplified"] is False
        assert "System" in serialized["required_usings"]
        assert serialized["compilation_result"]["success"] is True

    def test_serialize_compilation_result(self, reporter):
        """Test serializing CompilationResult entity."""
        result = CompilationResult(
            success=False,
            errors=["CS0246: Type not found"],
            warnings=["CS0168: Variable declared but never used"],
            execution_time_ms=150,
            exit_code=1
        )

        serialized = reporter._serialize_compilation_result(result)

        assert serialized["success"] is False
        assert "CS0246" in serialized["errors"][0]
        assert len(serialized["warnings"]) == 1
        assert serialized["execution_time_ms"] == 150

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

        # Should contain key sections (match actual implementation)
        assert "# Documentation Validation Report" in md_str
        assert "## Summary" in md_str
        assert "## Code Examples" in md_str
        assert "## File Path References" in md_str
        assert "## Namespace Convention Compliance" in md_str
        assert "## Pattern Implementation Status" in md_str
        assert "## Broken Internal Links" in md_str

    def test_generate_header(self, reporter, sample_report):
        """Test generating report header."""
        header = reporter._generate_header(sample_report)

        assert "# Documentation Validation Report" in header
        assert "2025-01-01" in header  # Date from sample_report
        assert "1.0.0" in header
        assert "FAIL" in header or "PASS" in header

    def test_generate_executive_summary(self, reporter, sample_report):
        """Test generating executive summary."""
        summary = reporter._generate_executive_summary(sample_report)

        assert "## Summary" in summary
        assert "Files Checked" in summary
        assert "Total Errors" in summary

    def test_generate_code_examples_section(self, reporter, sample_report):
        """Test generating code examples section."""
        section = reporter._generate_code_examples_section(sample_report)

        assert "## Code Examples" in section
        assert "Success Rate" in section or "total" in section
        assert "50.0%" in section or "1/2" in section
        # Should show failed compilation
        assert "docs/broken.md" in section or "CS0246" in section

    def test_generate_file_references_section(self, reporter, sample_report):
        """Test generating file references section."""
        section = reporter._generate_file_references_section(sample_report)

        assert "## File Path References" in section
        assert "Success Rate" in section or "total" in section
        assert "50.0%" in section or "1/2" in section
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
        assert "# Documentation Validation Report" in content

    def test_generate_recommendations(self, reporter, sample_report):
        """Test generating recommendations section."""
        section = reporter._generate_recommendations(sample_report)

        assert "## Recommendations" in section
        # Actual implementation generates recommendations for low implementation counts
        assert "Pattern" in section or "implementations" in section
