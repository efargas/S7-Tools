#!/usr/bin/env python3
"""
Archive Structure Validator

Validates that archived documents follow the deprecation policy:
1. Proper frontmatter with required fields
2. 2-year retention period (removal-date)
3. Superseding references exist
4. Redirect stubs preserved in original location (if applicable)

Usage:
    python validate-archive.py docs/
"""

import sys
import re
from pathlib import Path
from datetime import datetime, timedelta
from typing import Dict, List, Set
import yaml


class ArchiveValidator:
    """Validates archive structure and deprecation policy compliance"""

    REQUIRED_DEPRECATED_FIELDS = [
        'title', 'status', 'deprecated-date', 'superseded-by', 'removal-date', 'tags'
    ]

    REQUIRED_REDIRECT_FIELDS = [
        'title', 'status', 'superseded-by', 'removal-date'
    ]

    def __init__(self, docs_root: Path):
        self.docs_root = Path(docs_root).resolve()
        self.archive_root = self.docs_root / 'archive'
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.stats: Dict = {
            'archived_files': 0,
            'redirect_stubs': 0,
            'missing_redirects': 0,
            'invalid_retention': 0,
            'missing_superseding': 0
        }

    def validate_all(self) -> bool:
        """Run all validation checks"""
        print(f"Validating archive structure in {self.docs_root}...\n")

        # Check archive folder exists
        if not self.archive_root.exists():
            self.errors.append(f"Archive folder does not exist: {self.archive_root}")
            return False

        # Validate archived documents
        archived_files = list(self.archive_root.rglob('*.md'))
        self.stats['archived_files'] = len(archived_files)

        for archived_file in archived_files:
            self._validate_archived_document(archived_file)

        # Find redirect stubs
        redirect_stubs = self._find_redirect_stubs()
        self.stats['redirect_stubs'] = len(redirect_stubs)

        for redirect_file in redirect_stubs:
            self._validate_redirect_stub(redirect_file)

        # Print report
        self._print_report()

        return len(self.errors) == 0

    def _validate_archived_document(self, file_path: Path):
        """Validate an archived document"""
        try:
            content = file_path.read_text(encoding='utf-8')

            # Extract frontmatter
            frontmatter_match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
            if not frontmatter_match:
                self.errors.append(f"[{file_path.relative_to(self.docs_root)}] No frontmatter found")
                return

            try:
                frontmatter = yaml.safe_load(frontmatter_match.group(1))
            except yaml.YAMLError as e:
                self.errors.append(f"[{file_path.relative_to(self.docs_root)}] Invalid YAML: {e}")
                return

            # Check status
            status = frontmatter.get('status', '')
            if status != 'deprecated':
                self.warnings.append(
                    f"[{file_path.relative_to(self.docs_root)}] "
                    f"Archive file has status '{status}' instead of 'deprecated'"
                )

            # Check required fields
            for field in self.REQUIRED_DEPRECATED_FIELDS:
                if field not in frontmatter:
                    self.errors.append(
                        f"[{file_path.relative_to(self.docs_root)}] Missing required field: {field}"
                    )

            # Validate 2-year retention
            if 'deprecated-date' in frontmatter and 'removal-date' in frontmatter:
                deprecated_date = datetime.strptime(str(frontmatter['deprecated-date']), '%Y-%m-%d')
                removal_date = datetime.strptime(str(frontmatter['removal-date']), '%Y-%m-%d')
                expected_removal = deprecated_date + timedelta(days=730)  # 2 years

                if removal_date != expected_removal:
                    self.errors.append(
                        f"[{file_path.relative_to(self.docs_root)}] "
                        f"Removal date {removal_date.date()} does not match 2-year retention policy "
                        f"(expected {expected_removal.date()})"
                    )
                    self.stats['invalid_retention'] += 1

            # Check superseding reference exists
            if 'superseded-by' in frontmatter:
                superseding_ref = frontmatter['superseded-by']
                if superseding_ref.startswith('docs/'):
                    superseding_path = self.docs_root.parent / superseding_ref
                else:
                    superseding_path = file_path.parent / superseding_ref

                if not superseding_path.exists():
                    self.errors.append(
                        f"[{file_path.relative_to(self.docs_root)}] "
                        f"Superseding document does not exist: {superseding_ref}"
                    )
                    self.stats['missing_superseding'] += 1

        except Exception as e:
            self.errors.append(f"[{file_path.relative_to(self.docs_root)}] Error reading file: {e}")

    def _validate_redirect_stub(self, file_path: Path):
        """Validate a redirect stub"""
        try:
            content = file_path.read_text(encoding='utf-8')

            frontmatter_match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
            if not frontmatter_match:
                self.errors.append(f"[{file_path.relative_to(self.docs_root)}] Redirect stub missing frontmatter")
                return

            try:
                frontmatter = yaml.safe_load(frontmatter_match.group(1))
            except yaml.YAMLError as e:
                self.errors.append(f"[{file_path.relative_to(self.docs_root)}] Invalid YAML: {e}")
                return

            # Check required fields
            for field in self.REQUIRED_REDIRECT_FIELDS:
                if field not in frontmatter:
                    self.errors.append(
                        f"[{file_path.relative_to(self.docs_root)}] "
                        f"Redirect stub missing required field: {field}"
                    )

            # Verify points to archive
            if 'superseded-by' in frontmatter:
                superseding_ref = frontmatter['superseded-by']
                if not ('archive/' in superseding_ref or 'archive\\' in superseding_ref):
                    self.warnings.append(
                        f"[{file_path.relative_to(self.docs_root)}] "
                        f"Redirect stub does not point to archive: {superseding_ref}"
                    )

        except Exception as e:
            self.errors.append(f"[{file_path.relative_to(self.docs_root)}] Error reading redirect: {e}")

    def _find_redirect_stubs(self) -> List[Path]:
        """Find all redirect stub files"""
        redirects = []
        for md_file in self.docs_root.rglob('*.md'):
            if 'archive' in md_file.parts:
                continue

            try:
                content = md_file.read_text(encoding='utf-8')
                if 'status: "redirect"' in content or "status: 'redirect'" in content or 'status: redirect' in content:
                    redirects.append(md_file)
            except Exception:
                pass

        return redirects

    def _print_report(self):
        """Print validation report"""
        print("=" * 60)
        print("ARCHIVE VALIDATION REPORT")
        print("=" * 60)
        print()

        print("Statistics:")
        print(f"  Archived files: {self.stats['archived_files']}")
        print(f"  Redirect stubs: {self.stats['redirect_stubs']}")
        print(f"  Invalid retention periods: {self.stats['invalid_retention']}")
        print(f"  Missing superseding docs: {self.stats['missing_superseding']}")
        print()

        if self.warnings:
            print(f"Warnings ({len(self.warnings)}):")
            for warning in self.warnings[:10]:
                print(f"  ⚠️  {warning}")
            if len(self.warnings) > 10:
                print(f"  ... and {len(self.warnings) - 10} more")
            print()

        if self.errors:
            print(f"Errors ({len(self.errors)}):")
            for error in self.errors[:15]:
                print(f"  ❌ {error}")
            if len(self.errors) > 15:
                print(f"  ... and {len(self.errors) - 15} more")
            print()

        if self.errors:
            print("✗ Validation FAILED")
        else:
            print("✓ Validation PASSED")

        print("=" * 60)


def main():
    if len(sys.argv) < 2:
        print("Usage: python validate-archive.py <docs-directory>")
        sys.exit(1)

    docs_root = Path(sys.argv[1])
    validator = ArchiveValidator(docs_root)

    success = validator.validate_all()
    sys.exit(0 if success else 1)


if __name__ == '__main__':
    main()
