#!/usr/bin/env python3
"""
Duplicate Content Detector for S7Tools Documentation

Identifies duplicate or highly similar documentation content using MD5 hashing and text similarity.

Usage:
    python scripts/detect-duplicates.py <docs_root> [--threshold=<percent>] [--output=<file>]

Requirements:
    Python 3.7+ (no external dependencies)
"""

import sys
import os
import hashlib
import re
from pathlib import Path
from typing import List, Tuple, Dict
from dataclasses import dataclass
import difflib


@dataclass
class DuplicatePair:
    """Represents a pair of duplicate/similar files"""
    file1: str
    file2: str
    similarity: float
    match_type: str  # "exact" | "similar"
    recommendation: str


class DuplicateDetector:
    """Detects duplicate and similar content in documentation"""

    def __init__(self, docs_root: str, threshold: int = 80):
        self.docs_root = Path(docs_root)
        self.threshold = threshold / 100.0  # Convert percentage to ratio
        self.files_content: Dict[str, str] = {}
        self.files_hash: Dict[str, str] = {}
        self.duplicates: List[DuplicatePair] = []

    def analyze(self):
        """Analyze all markdown files for duplicates"""
        # Step 1: Read all files and compute hashes
        md_files = list(self.docs_root.rglob('*.md'))

        # Skip metadata directory
        md_files = [f for f in md_files if '.metadata' not in f.parts]

        print(f"Analyzing {len(md_files)} files for duplicates...\n")

        for md_file in md_files:
            try:
                content = md_file.read_text(encoding='utf-8')
                # Extract content without frontmatter for comparison
                clean_content = self.remove_frontmatter(content)

                self.files_content[str(md_file)] = clean_content
                self.files_hash[str(md_file)] = self.compute_hash(clean_content)
            except Exception as e:
                print(f"Warning: Could not read {md_file}: {e}", file=sys.stderr)

        # Step 2: Find exact duplicates by hash
        self.find_exact_duplicates()

        # Step 3: Find similar content by text comparison
        self.find_similar_content()

    def remove_frontmatter(self, content: str) -> str:
        """Remove YAML frontmatter from content"""
        match = re.match(r'^---\s*\n.*?\n---\s*\n', content, re.DOTALL)
        if match:
            return content[match.end():]
        return content

    def compute_hash(self, content: str) -> str:
        """Compute MD5 hash of content"""
        return hashlib.md5(content.encode('utf-8')).hexdigest()

    def find_exact_duplicates(self):
        """Find files with identical content hashes"""
        hash_to_files: Dict[str, List[str]] = {}

        for file_path, file_hash in self.files_hash.items():
            if file_hash not in hash_to_files:
                hash_to_files[file_hash] = []
            hash_to_files[file_hash].append(file_path)

        # Report duplicate groups
        for file_hash, files in hash_to_files.items():
            if len(files) > 1:
                # Create pairs
                for i in range(len(files) - 1):
                    self.duplicates.append(DuplicatePair(
                        file1=files[i],
                        file2=files[i + 1],
                        similarity=1.0,
                        match_type="exact",
                        recommendation="Merge into single file; use git mv to preserve history"
                    ))

    def find_similar_content(self):
        """Find files with high similarity using difflib"""
        file_paths = list(self.files_content.keys())

        for i in range(len(file_paths)):
            for j in range(i + 1, len(file_paths)):
                file1 = file_paths[i]
                file2 = file_paths[j]

                # Skip if already found as exact duplicate
                if self.files_hash[file1] == self.files_hash[file2]:
                    continue

                # Calculate similarity ratio
                content1 = self.files_content[file1]
                content2 = self.files_content[file2]

                similarity = difflib.SequenceMatcher(None, content1, content2).ratio()

                if similarity >= self.threshold:
                    self.duplicates.append(DuplicatePair(
                        file1=file1,
                        file2=file2,
                        similarity=similarity,
                        match_type="similar",
                        recommendation="Review for consolidation; preserve unique sections"
                    ))

    def print_results(self, output_file: str = None):
        """Print duplicate detection results"""
        if output_file:
            self.write_csv(output_file)
        else:
            self.print_console()

    def print_console(self):
        """Print results to console"""
        # Group by match type
        exact = [d for d in self.duplicates if d.match_type == "exact"]
        similar = [d for d in self.duplicates if d.match_type == "similar"]

        if exact:
            print("EXACT DUPLICATES (100% match):")
            for dup in exact:
                print(f"  - {self.make_relative(dup.file1)}")
                print(f"  - {self.make_relative(dup.file2)}")
                print(f"  Action: {dup.recommendation}\n")
        else:
            print("No exact duplicates found.\n")

        if similar:
            print(f"HIGH SIMILARITY (>={int(self.threshold * 100)}% match):")
            for dup in similar:
                print(f"  - {self.make_relative(dup.file1)}")
                print(f"  - {self.make_relative(dup.file2)}")
                print(f"  Similarity: {int(dup.similarity * 100)}%")
                print(f"  Action: {dup.recommendation}\n")
        else:
            print(f"No similar content found (threshold: {int(self.threshold * 100)}%).\n")

        # Summary
        total_size_savings = self.calculate_size_savings(exact)
        print("Summary:")
        print(f"  Exact duplicates: {len(exact)} pair(s)")
        print(f"  High similarity: {len(similar)} pair(s)")
        if total_size_savings > 0:
            print(f"  Potential space saved: ~{total_size_savings} KB")

    def write_csv(self, output_file: str):
        """Write results to CSV file"""
        with open(output_file, 'w') as f:
            f.write("File1,File2,Similarity,Type,Recommendation\n")
            for dup in self.duplicates:
                f.write(f"{self.make_relative(dup.file1)},{self.make_relative(dup.file2)},")
                f.write(f"{int(dup.similarity * 100)},{dup.match_type},{dup.recommendation}\n")
        print(f"Results written to {output_file}")

    def make_relative(self, file_path: str) -> str:
        """Make file path relative to repository root"""
        try:
            return str(Path(file_path).relative_to(self.docs_root.parent))
        except ValueError:
            return file_path

    def calculate_size_savings(self, exact_duplicates: List[DuplicatePair]) -> int:
        """Calculate potential size savings from removing exact duplicates (in KB)"""
        total_bytes = 0
        for dup in exact_duplicates:
            try:
                # Count size of one of the duplicates (will be removed)
                total_bytes += Path(dup.file2).stat().st_size
            except:
                pass
        return total_bytes // 1024


def main():
    if len(sys.argv) < 2:
        print("Usage: python scripts/detect-duplicates.py <docs_root> [--threshold=<percent>] [--output=<file>]")
        sys.exit(1)

    docs_root = sys.argv[1]
    threshold = 80  # Default
    output_file = None

    for arg in sys.argv[2:]:
        if arg.startswith("--threshold="):
            threshold = int(arg.split("=")[1])
        elif arg.startswith("--output="):
            output_file = arg.split("=")[1]

    if not os.path.isdir(docs_root):
        print(f"Error: {docs_root} is not a directory", file=sys.stderr)
        sys.exit(1)

    detector = DuplicateDetector(docs_root, threshold)
    detector.analyze()
    detector.print_results(output_file)


if __name__ == "__main__":
    main()
