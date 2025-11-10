#!/usr/bin/env python3
"""
Cross-Reference Generator for S7Tools Documentation

Generates bidirectional cross-references and "Related Documentation" sections.

Usage:
    python scripts/generate-cross-references.py <docs_root> [--dry-run] [--output=<file>]

Requirements:
    Python 3.7+ (no external dependencies)
"""

import sys
import os
import re
from pathlib import Path
from typing import Dict, List, Set
from dataclasses import dataclass
import json


@dataclass
class CrossReference:
    """Represents a cross-reference between files"""
    source: str
    target: str
    link_type: str = "relates-to"


class CrossReferenceGenerator:
    """Generates cross-references between documentation files"""

    def __init__(self, docs_root: str, dry_run: bool = False):
        self.docs_root = Path(docs_root)
        self.dry_run = dry_run
        self.graph: Dict[str, Set[str]] = {}  # file -> set of related files
        self.cross_refs: List[CrossReference] = []

    def generate(self):
        """Generate cross-references for all files"""
        print(f"Generating cross-references in {self.docs_root}...\n")

        # Step 1: Build relationship graph from links
        md_files = list(self.docs_root.rglob('*.md'))
        md_files = [f for f in md_files if '.metadata' not in f.parts]

        for md_file in md_files:
            try:
                content = md_file.read_text(encoding='utf-8')
                self.extract_relationships(content, md_file)
            except Exception as e:
                print(f"Warning: Could not read {md_file}: {e}", file=sys.stderr)

        # Step 2: Make relationships bidirectional
        self.bidirectionalize()

        # Step 3: Generate "Related Documentation" sections
        if not self.dry_run:
            self.update_files()

    def extract_relationships(self, content: str, source_file: Path):
        """Extract links to build relationship graph"""
        source_key = str(source_file)
        if source_key not in self.graph:
            self.graph[source_key] = set()

        # Extract markdown links
        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'

        for match in re.finditer(link_pattern, content):
            link_target = match.group(2)

            # Skip external links
            if link_target.startswith('http://') or link_target.startswith('https://'):
                continue

            # Remove anchor
            if '#' in link_target:
                link_target = link_target.split('#')[0]

            if not link_target:
                continue

            # Resolve relative path
            try:
                target_path = (source_file.parent / link_target).resolve()
                if target_path.exists():
                    self.graph[source_key].add(str(target_path))

                    self.cross_refs.append(CrossReference(
                        source=source_key,
                        target=str(target_path),
                        link_type="relates-to"
                    ))
            except Exception:
                pass

    def bidirectionalize(self):
        """Make all relationships bidirectional"""
        # Create reverse graph
        reverse_graph: Dict[str, Set[str]] = {}

        for source, targets in self.graph.items():
            for target in targets:
                if target not in reverse_graph:
                    reverse_graph[target] = set()
                reverse_graph[target].add(source)

        # Merge reverse relationships into main graph
        for target, sources in reverse_graph.items():
            if target not in self.graph:
                self.graph[target] = set()
            self.graph[target].update(sources)

    def update_files(self):
        """Update files with Related Documentation sections"""
        updated_count = 0

        for file_path, related_files in self.graph.items():
            if not related_files:
                continue  # No related files

            try:
                path_obj = Path(file_path)
                content = path_obj.read_text(encoding='utf-8')

                # Remove existing Related Documentation section if present
                content = re.sub(
                    r'\n## Related Documentation\n.*?(?=\n##|\Z)',
                    '',
                    content,
                    flags=re.DOTALL
                )

                # Generate new Related Documentation section
                related_section = self.generate_related_section(file_path, related_files)

                # Append to end of file
                content = content.rstrip() + "\n\n" + related_section + "\n"

                # Write back
                path_obj.write_text(content, encoding='utf-8')
                updated_count += 1

            except Exception as e:
                print(f"Warning: Could not update {file_path}: {e}", file=sys.stderr)

        print(f"✓ Updated {updated_count} files with cross-references")

    def generate_related_section(self, source_file: str, related_files: Set[str]) -> str:
        """Generate Related Documentation markdown section"""
        source_path = Path(source_file)
        lines = [
            "## Related Documentation",
            ""
        ]

        # Sort related files for consistency
        for related_file in sorted(related_files):
            try:
                related_path = Path(related_file)

                # Calculate relative path from source to target
                rel_path = os.path.relpath(related_path, source_path.parent)

                # Extract title from file (simplified - use filename)
                title = related_path.stem.replace('-', ' ').title()

                lines.append(f"- [{title}]({rel_path})")
            except Exception:
                pass

        lines.extend([
            "",
            "---",
            f"*This section is auto-generated. Do not edit manually. Last updated: {self.get_today()}*"
        ])

        return "\n".join(lines)

    def get_today(self) -> str:
        """Get today's date in YYYY-MM-DD format"""
        from datetime import datetime
        return datetime.now().strftime('%Y-%m-%d')

    def export_graph(self, output_file: str):
        """Export cross-reference graph to JSON"""
        graph_data = {
            "nodes": [],
            "edges": [],
            "orphans": []
        }

        # Add nodes
        for file_path in self.graph.keys():
            graph_data["nodes"].append({
                "file": str(Path(file_path).relative_to(self.docs_root.parent)),
                "outgoing_links": len(self.graph[file_path])
            })

        # Add edges
        for ref in self.cross_refs:
            try:
                graph_data["edges"].append({
                    "source": str(Path(ref.source).relative_to(self.docs_root.parent)),
                    "target": str(Path(ref.target).relative_to(self.docs_root.parent)),
                    "type": ref.link_type
                })
            except ValueError:
                pass

        # Write to file
        with open(output_file, 'w') as f:
            json.dump(graph_data, f, indent=2)

        print(f"✓ Cross-reference graph exported to {output_file}")


def main():
    if len(sys.argv) < 2:
        print("Usage: python scripts/generate-cross-references.py <docs_root> [--dry-run] [--output=<file>]")
        sys.exit(1)

    docs_root = sys.argv[1]
    dry_run = False
    output_file = None

    for arg in sys.argv[2:]:
        if arg == "--dry-run":
            dry_run = True
        elif arg.startswith("--output="):
            output_file = arg.split("=")[1]

    if not os.path.isdir(docs_root):
        print(f"Error: {docs_root} is not a directory", file=sys.stderr)
        sys.exit(1)

    generator = CrossReferenceGenerator(docs_root, dry_run)
    generator.generate()

    if output_file:
        generator.export_graph(output_file)

    if dry_run:
        print("\n✓ Dry run complete (no files modified)")


if __name__ == "__main__":
    main()
