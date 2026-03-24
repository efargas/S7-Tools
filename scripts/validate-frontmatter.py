#!/usr/bin/env python3
"""
Frontmatter Validator for S7Tools Documentation

Validates YAML frontmatter in Markdown files to ensure completeness and correct format.

Usage:
    python scripts/validate-frontmatter.py <docs_root> [--output=<format>] [--strict]

Requirements:
    pip install pyyaml
"""

import sys
import os
import re
import json
from pathlib import Path
from dataclasses import dataclass, field
from typing import List, Optional
from datetime import datetime

try:
    import yaml
except ImportError:
    print("Error: PyYAML not installed. Run: pip install pyyaml", file=sys.stderr)
    sys.exit(2)


@dataclass
class ValidationResult:
    """Represents a validation issue"""
    file_path: str
    rule_id: str
    severity: str  # "error" | "warning"
    message: str
    line_number: Optional[int] = None


@dataclass
class ValidationSummary:
    """Overall validation summary"""
    files_checked: int = 0
    errors: int = 0
    warnings: int = 0
    violations: List[ValidationResult] = field(default_factory=list)

    @property
    def passed(self) -> bool:
        return self.errors == 0


class FrontmatterValidator:
    """Validates frontmatter in Markdown documentation files"""

    REQUIRED_FIELDS = ['title', 'version', 'created', 'last-updated', 'status', 'tags']
    VALID_STATUSES = ['current', 'deprecated', 'draft', 'redirect']
    # Full semantic versioning pattern (supports pre-release and build metadata)
    # Examples: 1.0.0, 2.1.3, 1.0.0-beta.1, 3.2.1+build.123
    SEMVER_PATTERN = r'^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$'
    DATE_PATTERN = r'^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$'

    def __init__(self, docs_root: str, strict: bool = False):
        self.docs_root = Path(docs_root)
        self.strict = strict
        self.summary = ValidationSummary()

    def validate_all(self):
        """Validate all markdown files in docs_root"""
        for md_file in self.docs_root.rglob('*.md'):
            # Skip metadata, test fixtures, and website/blog directories
            if (
                '.metadata' in md_file.parts or
                '.test-fixtures' in md_file.parts or
                'website' in md_file.parts
            ):
                continue

            self.summary.files_checked += 1
            self.validate_file(md_file)

    def validate_file(self, file_path: Path):
        """Validate a single file's frontmatter"""
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception as e:
            self.add_violation(str(file_path), "FILE-001", "error",
                             f"Cannot read file: {e}")
            return

        frontmatter = self.extract_frontmatter(content)
        if frontmatter is None:
            self.add_violation(str(file_path), "META-001", "error",
                             "No frontmatter found or invalid YAML")
            return

        # Check required fields
        for field in self.REQUIRED_FIELDS:
            if field not in frontmatter:
                self.add_violation(str(file_path), "META-001", "error",
                                 f"Missing required field: {field}")

        # Validate version format
        if 'version' in frontmatter:
            if not re.match(self.SEMVER_PATTERN, str(frontmatter['version'])):
                self.add_violation(str(file_path), "META-002", "error",
                                 f"Invalid version format: {frontmatter['version']} (expected X.Y.Z)")

        # Validate date formats
        for date_field in ['created', 'last-updated']:
            if date_field in frontmatter:
                if not re.match(self.DATE_PATTERN, str(frontmatter[date_field])):
                    self.add_violation(str(file_path), "META-003", "error",
                                     f"Invalid date format: {date_field} (expected YYYY-MM-DD, got {frontmatter[date_field]})")

        # Validate status enum
        if 'status' in frontmatter:
            if frontmatter['status'] not in self.VALID_STATUSES:
                self.add_violation(str(file_path), "META-004", "error",
                                 f"Invalid status: {frontmatter['status']} (must be one of: {', '.join(self.VALID_STATUSES)})")

        # Validate tags is non-empty array
        if 'tags' in frontmatter:
            if not isinstance(frontmatter['tags'], list) or len(frontmatter['tags']) == 0:
                self.add_violation(str(file_path), "META-005", "error",
                                 "Tags must be a non-empty array")

        # Validate deprecated documents have required fields
        if frontmatter.get('status') == 'deprecated':
            if 'deprecated-date' not in frontmatter:
                self.add_violation(str(file_path), "META-007", "error",
                                 "Deprecated documents must have 'deprecated-date' field")
            if 'superseded-by' not in frontmatter:
                self.add_violation(str(file_path), "META-008", "warning",
                                 "Deprecated documents should have 'superseded-by' field")

            # Validate deprecated-date format
            if 'deprecated-date' in frontmatter:
                if not re.match(self.DATE_PATTERN, str(frontmatter['deprecated-date'])):
                    self.add_violation(str(file_path), "META-003", "error",
                                     f"Invalid deprecated-date format (expected YYYY-MM-DD, got {frontmatter['deprecated-date']})")

            # Validate removal-date if present
            if 'removal-date' in frontmatter:
                if not re.match(self.DATE_PATTERN, str(frontmatter['removal-date'])):
                    self.add_violation(str(file_path), "META-003", "error",
                                     f"Invalid removal-date format (expected YYYY-MM-DD, got {frontmatter['removal-date']})")

        # Check related paths exist (if specified)
        if 'related' in frontmatter and isinstance(frontmatter['related'], list):
            for related_path in frontmatter['related']:
                full_path = self.docs_root.parent / related_path
                if not full_path.exists():
                    self.add_violation(str(file_path), "META-006", "error",
                                     f"Related file does not exist: {related_path}")

    def extract_frontmatter(self, content: str) -> Optional[dict]:
        """Extract and parse YAML frontmatter from markdown content"""
        # Match frontmatter between --- delimiters at start of file
        match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
        if not match:
            return None

        try:
            return yaml.safe_load(match.group(1))
        except yaml.YAMLError:
            return None

    def add_violation(self, file_path: str, rule_id: str, severity: str, message: str):
        """Add a validation violation"""
        violation = ValidationResult(
            file_path=file_path,
            rule_id=rule_id,
            severity=severity,
            message=message
        )
        self.summary.violations.append(violation)

        if severity == "error":
            self.summary.errors += 1
        else:
            self.summary.warnings += 1

    def print_results(self, output_format: str = "text"):
        """Print validation results"""
        if output_format == "json":
            self.print_json()
        else:
            self.print_text()

    def print_text(self):
        """Print results in text format"""
        print(f"Validating frontmatter in {self.docs_root}...\n")

        # Group violations by file
        files_with_violations = {}
        for violation in self.summary.violations:
            if violation.file_path not in files_with_violations:
                files_with_violations[violation.file_path] = []
            files_with_violations[violation.file_path].append(violation)

        # Print violations by file
        for file_path, violations in sorted(files_with_violations.items()):
            severity_label = "ERROR" if any(v.severity == "error" for v in violations) else "WARNING"
            print(f"[{severity_label}] {file_path}")
            for violation in violations:
                print(f"  - {violation.message}")
            print()

        # Print summary
        print("Summary:")
        print(f"  Files checked: {self.summary.files_checked}")
        print(f"  Errors: {self.summary.errors}")
        print(f"  Warnings: {self.summary.warnings}")
        print()

        if self.summary.passed:
            print("✓ All validations passed")
        else:
            print(f"✗ {self.summary.errors} error(s) found")

    def print_json(self):
        """Print results in JSON format"""
        result = {
            "summary": {
                "files_checked": self.summary.files_checked,
                "errors": self.summary.errors,
                "warnings": self.summary.warnings,
                "passed": self.summary.passed
            },
            "violations": [
                {
                    "file": v.file_path,
                    "severity": v.severity,
                    "rule": v.rule_id,
                    "message": v.message
                }
                for v in self.summary.violations
            ]
        }
        print(json.dumps(result, indent=2))

    def get_exit_code(self) -> int:
        """Get exit code based on validation results"""
        if self.summary.errors > 0:
            return 1
        if self.strict and self.summary.warnings > 0:
            return 2
        return 0


def main():
    if len(sys.argv) < 2:
        print("Usage: python scripts/validate-frontmatter.py <docs_root> [--output=text|json] [--strict]")
        sys.exit(1)

    docs_root = sys.argv[1]
    output_format = "text"
    strict = False

    for arg in sys.argv[2:]:
        if arg.startswith("--output="):
            output_format = arg.split("=")[1]
        elif arg == "--strict":
            strict = True

    if not os.path.isdir(docs_root):
        print(f"Error: {docs_root} is not a directory", file=sys.stderr)
        sys.exit(1)

    validator = FrontmatterValidator(docs_root, strict=strict)
    validator.validate_all()
    validator.print_results(output_format)
    sys.exit(validator.get_exit_code())


if __name__ == "__main__":
    main()
