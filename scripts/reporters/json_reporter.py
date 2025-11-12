"""JSON Reporter

Generates machine-readable JSON validation reports that comply with
the validation-report-schema.json contract.
"""

import json
from datetime import datetime
from pathlib import Path
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import ValidationReport, CodeExample, CompilationResult, FilePathReference
from entities import NamespaceValidation, PatternImplementation, EditorConfigRule


class JSONReporter:
    """Generates JSON validation reports."""

    def generate_json_report(self, report: ValidationReport) -> str:
        """Generate JSON report from ValidationReport entity.

        Args:
            report: ValidationReport entity with validation results

        Returns:
            JSON string formatted according to schema
        """
        report_dict = {
            "generated_at": report.generated_at.isoformat(),
            "validation_version": report.validation_version,
            "total_files_checked": report.total_files_checked,
            "total_errors": report.total_errors,
            "total_warnings": report.total_warnings,
            "code_examples": [self._serialize_code_example(ex) for ex in report.code_examples],
            "compilation_success_rate": report.compilation_success_rate,
            "file_references": [self._serialize_file_reference(ref) for ref in report.file_references],
            "file_reference_success_rate": report.file_reference_success_rate,
            "namespace_validations": [self._serialize_namespace_validation(val) for val in report.namespace_validations],
            "namespace_compliance_rate": report.namespace_compliance_rate,
            "pattern_implementations": [self._serialize_pattern_implementation(pat) for pat in report.pattern_implementations],
            "pattern_verification_rate": report.pattern_verification_rate,
            "editorconfig_discrepancies": [self._serialize_editorconfig_rule(rule) for rule in report.editorconfig_discrepancies],
            "broken_links": report.broken_links,
            "execution_time_seconds": report.execution_time_seconds,
            "summary": report.summary
        }

        return json.dumps(report_dict, indent=2)

    def write_json_report(self, report: ValidationReport, output_path: Path) -> None:
        """Write JSON report to file.

        Args:
            report: ValidationReport entity
            output_path: Path to write JSON file
        """
        json_content = self.generate_json_report(report)

        # Ensure output directory exists
        output_path.parent.mkdir(parents=True, exist_ok=True)

        # Write JSON file
        output_path.write_text(json_content, encoding='utf-8')

    def _serialize_code_example(self, example: CodeExample) -> dict:
        """Serialize CodeExample entity to dict."""
        result = {
            "source_file": example.source_file,
            "line_number": example.line_number,
            "language": example.language,
            "code": example.code,
            "is_simplified": example.is_simplified
        }

        if example.context:
            result["context"] = example.context

        if example.required_usings:
            result["required_usings"] = example.required_usings

        if example.compilation_result:
            result["compilation_result"] = self._serialize_compilation_result(example.compilation_result)

        return result

    def _serialize_compilation_result(self, result: CompilationResult) -> dict:
        """Serialize CompilationResult entity to dict."""
        return {
            "success": result.success,
            "errors": result.errors,
            "warnings": result.warnings,
            "execution_time_ms": result.execution_time_ms,
            "exit_code": result.exit_code
        }

    def _serialize_file_reference(self, ref: FilePathReference) -> dict:
        """Serialize FilePathReference entity to dict."""
        result = {
            "source_file": ref.source_file,
            "line_number": ref.line_number,
            "referenced_path": ref.referenced_path,
            "path_type": ref.path_type,
            "exists": ref.exists
        }

        if ref.resolved_path:
            result["resolved_path"] = ref.resolved_path

        return result

    def _serialize_namespace_validation(self, validation: NamespaceValidation) -> dict:
        """Serialize NamespaceValidation entity to dict."""
        result = {
            "source_file": validation.source_file,
            "declared_namespace": validation.declared_namespace,
            "expected_pattern": validation.expected_pattern,
            "is_compliant": validation.is_compliant
        }

        if validation.category:
            result["category"] = validation.category

        if validation.violation_details:
            result["violation_details"] = validation.violation_details

        return result

    def _serialize_pattern_implementation(self, pattern: PatternImplementation) -> dict:
        """Serialize PatternImplementation entity to dict."""
        return {
            "pattern_name": pattern.pattern_name,
            "documented_location": pattern.documented_location,
            "expected_files": pattern.expected_files,
            "found_files": pattern.found_files,
            "is_verified": pattern.is_verified,
            "missing_files": pattern.missing_files,
            "extra_files": pattern.extra_files
        }

    def _serialize_editorconfig_rule(self, rule: EditorConfigRule) -> dict:
        """Serialize EditorConfigRule entity to dict."""
        result = {
            "section": rule.section,
            "rule_name": rule.rule_name,
            "rule_value": rule.rule_value,
            "is_consistent": rule.is_consistent
        }

        if rule.documented_value:
            result["documented_value"] = rule.documented_value

        return result


def generate_json_report(report: ValidationReport) -> str:
    """Convenience function to generate JSON report.

    Args:
        report: ValidationReport entity

    Returns:
        JSON string
    """
    reporter = JSONReporter()
    return reporter.generate_json_report(report)


def write_json_report(report: ValidationReport, output_path: Path) -> None:
    """Convenience function to write JSON report to file.

    Args:
        report: ValidationReport entity
        output_path: Path to write JSON file
    """
    reporter = JSONReporter()
    reporter.write_json_report(report, output_path)
