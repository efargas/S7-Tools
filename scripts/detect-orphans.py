#!/usr/bin/env python3
"""
Orphan File Detector for S7Tools Documentation

Identifies documentation files with no incoming links (potential orphans).

Usage:
    python scripts/detect-orphans.py <docs_root> [--exclude=<pattern>] [--report]

Requirements:
    Python 3.7+ (no external dependencies)
"""

import sys
import os
import re
from pathlib import Path
from typing import Set, Dict, List
from dataclasses import dataclass
from datetime import datetime


@dataclass
class OrphanFile:
    """Represents a potentially orphaned file"""
    path: str
    last_modified: str
    recommendation: str


class OrphanDetector:
    """Detects orphaned documentation files"""

    def __init__(self, docs_root: str, exclude_patterns: List[str] = None):
        self.docs_root = Path(docs_root)
        self.exclude_patterns = exclude_patterns or ['INDEX.md', 'README.md', '_index.md']
        self.all_files: Set[str] = set()
        self.linked_files: Set[str] = set()
        self.orphans: List[OrphanFile] = []

    def analyze(self):
        """Analyze all files for orphans"""
        # Step 1: Collect all markdown files
        md_files = list(self.docs_root.rglob('*.md'))
        md_files = [f for f in md_files if '.metadata' not in f.parts]

        print(f"Checking for orphaned files...\n")

        for md_file in md_files:
            self.all_files.add(str(md_file))

        # Step 2: Find all links in all files
        for md_file in md_files:
            try:
                content = md_file.read_text(encoding='utf-8')
                self.extract_links(content, md_file)
            except Exception as e:
                print(f"Warning: Could not read {md_file}: {e}", file=sys.stderr)

        # Step 3: Identify orphans
        self.identify_orphans()

    def extract_links(self, content: str, source_file: Path):
        """Extract markdown links from content and resolve target files"""
        # Match markdown links: [text](path/to/file.md) or [text](path/to/file.md#anchor)
        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'

        for match in re.finditer(link_pattern, content):
            link_target = match.group(2)

            # Skip external links
            if link_target.startswith('http://') or link_target.startswith('https://'):
                continue

            # Remove anchor if present
            if '#' in link_target:
                link_target = link_target.split('#')[0]

            # Skip empty links (pure anchors)
            if not link_target:
                continue

            # Resolve relative path
            try:
                target_path = (source_file.parent / link_target).resolve()
                if target_path.exists():
                    self.linked_files.add(str(target_path))
            except Exception:
                pass  # Invalid link, skip

    def identify_orphans(self):
        """Identify files with no incoming links"""
        for file_path in self.all_files:
            # Check if excluded
            if self.is_excluded(file_path):
                continue

            # Check if linked
            if file_path not in self.linked_files:
                path_obj = Path(file_path)
                try:
                    last_modified = datetime.fromtimestamp(path_obj.stat().st_mtime).strftime('%Y-%m-%d')
                except:
                    last_modified = "unknown"

                # Determine recommendation
                recommendation = self.get_recommendation(last_modified)

                self.orphans.append(OrphanFile(
                    path=file_path,
                    last_modified=last_modified,
                    recommendation=recommendation
                ))

    def is_excluded(self, file_path: str) -> bool:
        """Check if file matches exclusion patterns"""
        file_name = Path(file_path).name
        return any(pattern in file_name for pattern in self.exclude_patterns)

    def get_recommendation(self, last_modified: str) -> str:
        """Get recommendation based on last modified date"""
        if last_modified == "unknown":
            return "Review and add links or archive"

        try:
            mod_date = datetime.strptime(last_modified, '%Y-%m-%d')
            days_old = (datetime.now() - mod_date).days

            if days_old > 365:
                return "Archive (outdated)"
            elif days_old > 180:
                return "Add to relevant guides or archive"
            else:
                return "Add to relevant guides (recent)"
        except:
            return "Review and add links or archive"

    def print_results(self):
        """Print orphan detection results"""
        if not self.orphans:
            print("✓ No orphaned files found.\n")
            return

        print("ORPHANED FILES (no incoming links):")
        for orphan in sorted(self.orphans, key=lambda x: x.last_modified):
            print(f"  - {self.make_relative(orphan.path)}")
            print(f"    Last updated: {orphan.last_modified}")
            print(f"    Recommendation: {orphan.recommendation}\n")

        # Show excluded files
        excluded_files = [f for f in self.all_files if self.is_excluded(f)]
        if excluded_files:
            print("Excluded (intentional orphans):")
            for file_path in sorted(excluded_files):
                file_name = Path(file_path).name
                print(f"  - {self.make_relative(file_path)} (entry point)")
            print()

        # Summary
        print("Summary:")
        print(f"  Orphans found: {len(self.orphans)}")
        print(f"  Excluded: {len(excluded_files)}")
        print(f"  Review required: {len(self.orphans)}")

    def make_relative(self, file_path: str) -> str:
        """Make file path relative to repository root"""
        try:
            return str(Path(file_path).relative_to(self.docs_root.parent))
        except ValueError:
            return file_path


def main():
    if len(sys.argv) < 2:
        print("Usage: python scripts/detect-orphans.py <docs_root> [--exclude=<pattern>] [--report]")
        sys.exit(1)

    docs_root = sys.argv[1]
    exclude_patterns = None

    for arg in sys.argv[2:]:
        if arg.startswith("--exclude="):
            exclude_patterns = arg.split("=")[1].split(",")

    if not os.path.isdir(docs_root):
        print(f"Error: {docs_root} is not a directory", file=sys.stderr)
        sys.exit(1)

    detector = OrphanDetector(docs_root, exclude_patterns)
    detector.analyze()
    detector.print_results()


if __name__ == "__main__":
    main()
