"""Markdown Reporter

Generates human-readable Markdown validation reports with formatted tables,
sections, and color-coded status indicators.
"""

from datetime import datetime
from pathlib import Path
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import ValidationReport


class MarkdownReporter:
    """Generates Markdown validation reports."""

    def generate_markdown_report(self, report: ValidationReport) -> str:
        """Generate Markdown report from ValidationReport entity.

        Args:
            report: ValidationReport entity with validation results

        Returns:
            Formatted Markdown string
        """
        sections = []

        # Header
        sections.append(self._generate_header(report))

        # Executive Summary
        sections.append(self._generate_executive_summary(report))

        # Code Example Compilation Results
        if report.code_examples:
            sections.append(self._generate_code_examples_section(report))

        # File Reference Validation
        if report.file_references:
            sections.append(self._generate_file_references_section(report))

        # Namespace Convention Compliance
        if report.namespace_validations:
            sections.append(self._generate_namespace_section(report))

        # Pattern Implementation Status
        if report.pattern_implementations:
            sections.append(self._generate_patterns_section(report))

        # Broken Links
        if report.broken_links:
            sections.append(self._generate_broken_links_section(report))

        # EditorConfig Discrepancies
        if report.editorconfig_discrepancies:
            sections.append(self._generate_editorconfig_section(report))

        # Recommendations
        sections.append(self._generate_recommendations(report))

        return "\n\n".join(sections)

    def write_markdown_report(self, report: ValidationReport, output_path: Path) -> None:
        """Write Markdown report to file.

        Args:
            report: ValidationReport entity
            output_path: Path to write Markdown file
        """
        markdown_content = self.generate_markdown_report(report)

        # Ensure output directory exists
        output_path.parent.mkdir(parents=True, exist_ok=True)

        # Write Markdown file
        output_path.write_text(markdown_content, encoding='utf-8')

    def _generate_header(self, report: ValidationReport) -> str:
        """Generate report header."""
        status = report.get_status()
        return f"""# Documentation Validation Report

**Generated**: {report.generated_at.strftime("%Y-%m-%d %H:%M:%S")}
**Validation Version**: {report.validation_version}
**Status**: {status}"""

    def _generate_executive_summary(self, report: ValidationReport) -> str:
        """Generate executive summary section."""
        return f"""## Summary

- **Files Checked**: {report.total_files_checked}
- **Total Errors**: {report.total_errors}
- **Total Warnings**: {report.total_warnings}
- **Execution Time**: {report.execution_time_seconds:.2f}s

### Success Criteria Status

| Criterion | Target | Actual | Status |
|-----------|--------|--------|--------|
| Code Compilation | 100% | {report.compilation_success_rate:.1f}% | {self._status_icon(report.compilation_success_rate >= 100.0)} |
| File References | 100% | {report.file_reference_success_rate:.1f}% | {self._status_icon(report.file_reference_success_rate >= 100.0)} |
| Namespace Compliance | 100% | {report.namespace_compliance_rate:.1f}% | {self._status_icon(report.namespace_compliance_rate >= 100.0)} |
| Pattern Verification | 100% | {report.pattern_verification_rate:.1f}% | {self._status_icon(report.pattern_verification_rate >= 100.0)} |
| Broken Links | 0 | {len(report.broken_links)} | {self._status_icon(len(report.broken_links) == 0)} |
| EditorConfig Consistency | 100% | {100.0 if not report.editorconfig_discrepancies else 0.0}% | {self._status_icon(len(report.editorconfig_discrepancies) == 0)} |
| Execution Time | <60s | {report.execution_time_seconds:.2f}s | {self._status_icon(report.execution_time_seconds < 60.0)} |"""

    def _generate_code_examples_section(self, report: ValidationReport) -> str:
        """Generate code examples section."""
        total = len(report.code_examples)
        success_count = sum(1 for ex in report.code_examples if ex.compilation_result and ex.compilation_result.success)
        failed = [ex for ex in report.code_examples if ex.compilation_result and not ex.compilation_result.success]

        sections = [f"""## Code Examples ({total} total)

**Success Rate**: {report.compilation_success_rate:.1f}% ({success_count}/{total})"""]

        if failed:
            sections.append("\n### Failed Compilations\n")
            sections.append("| Source File | Line | Error |")
            sections.append("|-------------|------|-------|")

            for ex in failed[:10]:  # Limit to first 10 errors
                errors = ", ".join(ex.compilation_result.errors[:2]) if ex.compilation_result else "Unknown"
                sections.append(f"| {ex.source_file} | {ex.line_number} | {errors} |")

            if len(failed) > 10:
                sections.append(f"\n*... and {len(failed) - 10} more compilation failures*\n")

        return "\n".join(sections)

    def _generate_file_references_section(self, report: ValidationReport) -> str:
        """Generate file references section."""
        total = len(report.file_references)
        valid_count = sum(1 for ref in report.file_references if ref.exists)
        broken = [ref for ref in report.file_references if not ref.exists]

        sections = [f"""## File Path References ({total} total)

**Success Rate**: {report.file_reference_success_rate:.1f}% ({valid_count}/{total})"""]

        if broken:
            sections.append("\n### Broken File References\n")
            sections.append("| Source File | Line | Referenced Path |")
            sections.append("|-------------|------|----------------|")

            for ref in broken[:10]:  # Limit to first 10
                sections.append(f"| {ref.source_file} | {ref.line_number} | {ref.referenced_path} |")

            if len(broken) > 10:
                sections.append(f"\n*... and {len(broken) - 10} more broken references*\n")

        return "\n".join(sections)

    def _generate_namespace_section(self, report: ValidationReport) -> str:
        """Generate namespace compliance section."""
        total = len(report.namespace_validations)
        compliant_count = sum(1 for val in report.namespace_validations if val.is_compliant)
        violations = [val for val in report.namespace_validations if not val.is_compliant]

        sections = [f"""## Namespace Convention Compliance ({total} total)

**Compliance Rate**: {report.namespace_compliance_rate:.1f}% ({compliant_count}/{total})"""]

        if violations:
            sections.append("\n### Violations\n")
            sections.append("| Source File | Expected Pattern | Declared Namespace |")
            sections.append("|-------------|------------------|-------------------|")

            for val in violations[:10]:  # Limit to first 10
                sections.append(f"| {val.source_file} | {val.expected_pattern} | {val.declared_namespace} |")

            if len(violations) > 10:
                sections.append(f"\n*... and {len(violations) - 10} more violations*\n")

        return "\n".join(sections)

    def _generate_patterns_section(self, report: ValidationReport) -> str:
        """Generate pattern verification section."""
        total = len(report.pattern_implementations)
        verified_count = sum(1 for pat in report.pattern_implementations if pat.is_verified)

        sections = [f"""## Pattern Implementation Status ({total} patterns)

**Verification Rate**: {report.pattern_verification_rate:.1f}% ({verified_count}/{total})\n"""]

        sections.append("| Pattern | Status | Missing Files |")
        sections.append("|---------|--------|--------------|")

        for pattern in report.pattern_implementations:
            status = "✅ Verified" if pattern.is_verified else "❌ Incomplete"
            missing = ", ".join(pattern.missing_files) if pattern.missing_files else "None"
            sections.append(f"| {pattern.pattern_name} | {status} | {missing} |")

        return "\n".join(sections)

    def _generate_broken_links_section(self, report: ValidationReport) -> str:
        """Generate broken links section."""
        total = len(report.broken_links)

        sections = [f"""## Broken Internal Links ({total} total)\n"""]

        if total > 0:
            sections.append("| Source File | Target Path | Line |")
            sections.append("|-------------|-------------|------|")

            for link in report.broken_links[:20]:  # Limit to first 20
                sections.append(f"| {link['source_file']} | {link['target_path']} | {link['line_number']} |")

            if total > 20:
                sections.append(f"\n*... and {total - 20} more broken links*\n")
        else:
            sections.append("✅ No broken links found!\n")

        return "\n".join(sections)

    def _generate_editorconfig_section(self, report: ValidationReport) -> str:
        """Generate EditorConfig discrepancies section."""
        total = len(report.editorconfig_discrepancies)

        sections = [f"""## EditorConfig Consistency ({total} discrepancies)\n"""]

        if total > 0:
            sections.append("| Section | Rule | .editorconfig | Documented |")
            sections.append("|---------|------|--------------|------------|")

            for rule in report.editorconfig_discrepancies:
                sections.append(f"| {rule.section} | {rule.rule_name} | {rule.rule_value} | {rule.documented_value or 'N/A'} |")
        else:
            sections.append("✅ All EditorConfig rules match documentation!\n")

        return "\n".join(sections)

    def _generate_recommendations(self, report: ValidationReport) -> str:
        """Generate recommendations section."""
        recommendations = []

        # Deprecation candidates (patterns with <5 implementations)
        for pattern in report.pattern_implementations:
            impl_count = len(pattern.found_files)
            if impl_count > 0 and impl_count < 5:
                recommendations.append(
                    f"- **{pattern.pattern_name}**: Only {impl_count} implementations found. "
                    f"Consider documenting as deprecated if pattern is being phased out."
                )

        if recommendations:
            return f"""## Recommendations

{chr(10).join(recommendations)}"""
        else:
            return """## Recommendations

No specific recommendations at this time. All validation checks passed!"""

    def _status_icon(self, passed: bool) -> str:
        """Get status icon for boolean result."""
        return "✅" if passed else "❌"


def generate_markdown_report(report: ValidationReport) -> str:
    """Convenience function to generate Markdown report.

    Args:
        report: ValidationReport entity

    Returns:
        Formatted Markdown string
    """
    reporter = MarkdownReporter()
    return reporter.generate_markdown_report(report)


def write_markdown_report(report: ValidationReport, output_path: Path) -> None:
    """Convenience function to write Markdown report to file.

    Args:
        report: ValidationReport entity
        output_path: Path to write Markdown file
    """
    reporter = MarkdownReporter()
    reporter.write_markdown_report(report, output_path)
