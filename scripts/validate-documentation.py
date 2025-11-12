#!/usr/bin/env python3
"""S7Tools Documentation Validation Script

Main orchestrator for validating S7Tools documentation against source code.
Performs code compilation checks, file reference validation, namespace convention
verification, pattern implementation checks, and link validation.

Usage:
    python3 scripts/validate-documentation.py [options]

Options:
    --verbose           Enable verbose output
    --skip-compilation  Skip slow code compilation checks
    --category <cat>    Validate specific category only (architecture, patterns, guides, etc.)
    --output <path>     Output directory for reports (default: docs/.metadata/)
"""

import argparse
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Optional

# Add script directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from entities import ValidationReport, CodeExample
from extractors.markdown_parser import MarkdownParser
from validators.code_compiler import CodeCompiler
from validators.file_reference import FileReferenceValidator
from validators.namespace_validator import NamespaceValidator
from validators.pattern_validator import PatternValidator
from validators.link_validator import LinkValidator
from reporters.json_reporter import JSONReporter
from reporters.markdown_reporter import MarkdownReporter

# Colorama for cross-platform colored output
try:
    from colorama import init as colorama_init, Fore, Style
    colorama_init()
    HAS_COLOR = True
except ImportError:
    HAS_COLOR = False
    # Fallback no-op classes
    class Fore:
        RED = GREEN = YELLOW = RESET = ""
    class Style:
        BRIGHT = RESET_ALL = ""


class DocumentationValidator:
    """Main orchestrator for documentation validation."""

    VERSION = "1.0.0"

    def __init__(
        self,
        workspace_root: Path,
        skip_compilation: bool = False,
        verbose: bool = False
    ):
        """Initialize validator.

        Args:
            workspace_root: Path to S7Tools workspace root
            skip_compilation: Skip code compilation checks (faster)
            verbose: Enable verbose logging
        """
        self.workspace_root = workspace_root
        self.skip_compilation = skip_compilation
        self.verbose = verbose

        # Initialize components
        self.parser = MarkdownParser()
        self.compiler = CodeCompiler(workspace_root)
        self.file_validator = FileReferenceValidator(workspace_root)
        self.namespace_validator = NamespaceValidator(workspace_root)
        self.pattern_validator = PatternValidator(workspace_root)
        self.link_validator = LinkValidator(workspace_root)

    def validate_all_documentation(
        self,
        category: Optional[str] = None
    ) -> ValidationReport:
        """Validate all documentation files.

        Args:
            category: Optional category filter (architecture, patterns, etc.)

        Returns:
            ValidationReport with aggregated results
        """
        start_time = time.time()

        self._log(f"{Fore.CYAN}Starting S7Tools documentation validation...{Style.RESET_ALL}")
        self._log(f"Workspace: {self.workspace_root}")
        self._log(f"Skip compilation: {self.skip_compilation}")

        # 1. Find and parse documentation files
        self._log("\n📄 Scanning documentation files...")
        doc_files = self._find_documentation_files(category)
        self._log(f"Found {len(doc_files)} documentation files")

        # 2. Extract code examples and file references
        self._log("\n📝 Extracting code examples and file references...")
        all_code_examples = []
        all_file_references = []
        for doc_file in doc_files:
            examples = self.parser.extract_code_blocks(doc_file.content, str(doc_file.path))
            all_code_examples.extend(examples)

            references = self.parser.extract_file_references(doc_file.content, str(doc_file.path))
            all_file_references.extend(references)

        self._log(f"Extracted {len(all_code_examples)} code examples")
        self._log(f"Extracted {len(all_file_references)} file references")

        # 3. Compile code examples
        if not self.skip_compilation and all_code_examples:
            self._log("\n⚙️  Compiling code examples...")
            for i, example in enumerate(all_code_examples, 1):
                if self.verbose:
                    self._log(f"  Compiling {i}/{len(all_code_examples)}: {example.source_file}:{example.line_number}")
                example.compilation_result = self.compiler.compile_csharp_example(example)
        elif self.skip_compilation:
            self._log("\n⏭️  Skipping code compilation (--skip-compilation)")

        # Calculate compilation success rate
        compilable_examples = [ex for ex in all_code_examples if not ex.is_simplified]
        if compilable_examples:
            successful_compilations = sum(
                1 for ex in compilable_examples
                if ex.compilation_result and ex.compilation_result.success
            )
            compilation_success_rate = (successful_compilations / len(compilable_examples)) * 100.0
        else:
            compilation_success_rate = 100.0

        # 4. Validate file references
        self._log("\n🔗 Validating file references...")
        all_file_references = self.file_validator.validate_all_references(all_file_references)
        file_reference_success_rate = self.file_validator.calculate_success_rate(all_file_references)

        # 5. Validate namespace conventions
        self._log("\n📦 Validating namespace conventions...")
        namespace_validations = self.namespace_validator.scan_viewmodels_and_views()
        namespace_compliance_rate = self.namespace_validator.calculate_compliance_rate(namespace_validations)

        # 6. Verify pattern implementations
        self._log("\n🎨 Verifying architectural patterns...")
        pattern_implementations = self.pattern_validator.verify_all_core_patterns()
        pattern_verification_rate = self.pattern_validator.calculate_verification_rate(pattern_implementations)

        # 7. Validate internal links
        self._log("\n🔗 Validating internal links...")
        broken_links = self.link_validator.validate_all_documentation_links(doc_files)

        # 8. Check EditorConfig consistency (placeholder - TODO in future enhancement)
        editorconfig_discrepancies = []

        # Calculate totals
        total_errors = 0
        total_warnings = 0

        # Count errors
        total_errors += len([ex for ex in all_code_examples if ex.compilation_result and not ex.compilation_result.success])
        total_errors += len([ref for ref in all_file_references if not ref.exists])
        total_errors += len([val for val in namespace_validations if not val.is_compliant])
        total_errors += len([pat for pat in pattern_implementations if not pat.is_verified])
        total_errors += len(broken_links)

        # Count warnings
        total_warnings += len([ex for ex in all_code_examples if ex.is_simplified])

        # Generate summary
        execution_time = time.time() - start_time
        summary = self._generate_summary(
            len(doc_files),
            total_errors,
            total_warnings,
            execution_time
        )

        # Create report
        report = ValidationReport(
            generated_at=datetime.now(),
            validation_version=self.VERSION,
            total_files_checked=len(doc_files),
            total_errors=total_errors,
            total_warnings=total_warnings,
            code_examples=all_code_examples,
            compilation_success_rate=compilation_success_rate,
            file_references=all_file_references,
            file_reference_success_rate=file_reference_success_rate,
            namespace_validations=namespace_validations,
            namespace_compliance_rate=namespace_compliance_rate,
            pattern_implementations=pattern_implementations,
            pattern_verification_rate=pattern_verification_rate,
            editorconfig_discrepancies=editorconfig_discrepancies,
            broken_links=broken_links,
            execution_time_seconds=execution_time,
            summary=summary
        )

        return report

    def _find_documentation_files(self, category: Optional[str] = None):
        """Find all documentation files to validate.

        Args:
            category: Optional category filter

        Returns:
            List of DocumentationFile entities
        """
        docs_dir = self.workspace_root / "docs"
        if not docs_dir.exists():
            return []

        doc_files = []
        for md_file in docs_dir.rglob("*.md"):
            # Skip certain files
            if md_file.name in ["README.md", "CHANGELOG.md"]:
                continue

            # Parse file
            try:
                doc_file = self.parser.parse_markdown_file(md_file)

                # Filter by category if specified
                if category and doc_file.category != category:
                    continue

                doc_files.append(doc_file)
            except Exception as e:
                self._log(f"Warning: Failed to parse {md_file}: {e}")

        return doc_files

    def _generate_summary(
        self,
        files_checked: int,
        total_errors: int,
        total_warnings: int,
        execution_time: float
    ) -> str:
        """Generate human-readable summary.

        Args:
            files_checked: Number of files validated
            total_errors: Total error count
            total_warnings: Total warning count
            execution_time: Execution time in seconds

        Returns:
            Summary string
        """
        if total_errors == 0:
            return (
                f"All validation checks passed! "
                f"Checked {files_checked} files with {total_warnings} warnings "
                f"in {execution_time:.2f}s."
            )
        else:
            return (
                f"Validation completed with {total_errors} errors and {total_warnings} warnings. "
                f"Checked {files_checked} files in {execution_time:.2f}s."
            )

    def _log(self, message: str):
        """Log message to console.

        Args:
            message: Message to log
        """
        print(message)


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Validate S7Tools documentation against source code"
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Enable verbose output"
    )
    parser.add_argument(
        "--skip-compilation",
        action="store_true",
        help="Skip code compilation checks (faster)"
    )
    parser.add_argument(
        "--category",
        type=str,
        choices=["architecture", "patterns", "guides", "templates", "reviews", "archive"],
        help="Validate specific category only"
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output directory for reports (default: docs/.metadata/)"
    )

    args = parser.parse_args()

    # Determine workspace root (assume script is in scripts/ directory)
    workspace_root = Path(__file__).parent.parent
    output_dir = args.output or (workspace_root / "docs" / ".metadata")

    # Create validator
    validator = DocumentationValidator(
        workspace_root=workspace_root,
        skip_compilation=args.skip_compilation,
        verbose=args.verbose
    )

    # Run validation
    try:
        report = validator.validate_all_documentation(category=args.category)

        # Generate reports
        print(f"\n{Fore.CYAN}Generating validation reports...{Style.RESET_ALL}")

        # JSON report
        json_reporter = JSONReporter()
        json_path = output_dir / "validation-results.json"
        json_reporter.write_json_report(report, json_path)
        print(f"✅ JSON report: {json_path}")

        # Markdown report
        md_reporter = MarkdownReporter()
        md_path = output_dir / "validation-report.md"
        md_reporter.write_markdown_report(report, md_path)
        print(f"✅ Markdown report: {md_path}")

        # Print summary to console
        print(f"\n{Fore.CYAN}{'=' * 60}{Style.RESET_ALL}")
        print(f"{Fore.CYAN}VALIDATION SUMMARY{Style.RESET_ALL}")
        print(f"{Fore.CYAN}{'=' * 60}{Style.RESET_ALL}")
        print(f"Files Checked: {report.total_files_checked}")
        print(f"Total Errors: {report.total_errors}")
        print(f"Total Warnings: {report.total_warnings}")
        print(f"Execution Time: {report.execution_time_seconds:.2f}s")
        print(f"\nStatus: {report.get_status()}")
        print(f"{Fore.CYAN}{'=' * 60}{Style.RESET_ALL}")

        # Exit with appropriate code
        if report.total_errors > 0:
            print(f"\n{Fore.RED}❌ Validation failed with {report.total_errors} errors!{Style.RESET_ALL}")
            sys.exit(1)
        else:
            print(f"\n{Fore.GREEN}✅ All validation checks passed!{Style.RESET_ALL}")
            sys.exit(0)

    except Exception as e:
        print(f"\n{Fore.RED}❌ Validation failed with exception: {e}{Style.RESET_ALL}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(2)


if __name__ == "__main__":
    main()
