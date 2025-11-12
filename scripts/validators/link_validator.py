"""Link Validator

Validates internal markdown links in documentation to ensure they point
to existing files and sections.
"""

import re
from pathlib import Path
from typing import Optional
from urllib.parse import urlparse, unquote
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import DocumentationFile


class LinkValidator:
    """Validates internal links in markdown documentation."""

    def __init__(self, workspace_root: Path):
        """Initialize validator with workspace root.

        Args:
            workspace_root: Path to S7Tools workspace root
        """
        self.workspace_root = workspace_root

    def validate_internal_links(
        self,
        doc_file: DocumentationFile
    ) -> list[dict]:
        """Validate all internal links in a documentation file.

        Args:
            doc_file: DocumentationFile entity to validate

        Returns:
            List of broken links (empty if all links valid)
            Each broken link is a dict with:
                - source_file: str
                - target_path: str
                - line_number: int
                - reason: str
        """
        broken_links = []

        # Extract all internal links
        links = self._extract_internal_links(doc_file.content)

        for link, line_number in links:
            # Resolve link target
            if not self.resolve_markdown_link(link, doc_file.path):
                broken_links.append({
                    "source_file": str(doc_file.path.relative_to(self.workspace_root)),
                    "target_path": link,
                    "line_number": line_number,
                    "reason": "Target file not found"
                })

        return broken_links

    def resolve_markdown_link(
        self,
        link: str,
        source: Path
    ) -> bool:
        """Check if a markdown link resolves to an existing file.

        Args:
            link: Link target (relative path or absolute path)
            source: Source documentation file containing the link

        Returns:
            True if link resolves to existing file, False otherwise
        """
        # Parse link to separate path and anchor
        if '#' in link:
            link_path, anchor = link.split('#', 1)
        else:
            link_path = link
            anchor = None

        # Skip empty paths (anchor-only links)
        if not link_path:
            return True

        # Resolve relative link
        if link_path.startswith('../') or link_path.startswith('./'):
            target_path = (source.parent / link_path).resolve()
        elif link_path.startswith('/'):
            # Absolute path from repo root
            target_path = self.workspace_root / link_path.lstrip('/')
        else:
            # Relative to docs root
            target_path = self.workspace_root / "docs" / link_path

        # Check if target exists
        if not target_path.exists():
            return False

        # TODO: If anchor specified, validate section exists in target file
        # This would require parsing the target markdown and checking for heading

        return True

    def validate_all_documentation_links(
        self,
        doc_files: list[DocumentationFile]
    ) -> list[dict]:
        """Validate links in all documentation files.

        Args:
            doc_files: List of DocumentationFile entities

        Returns:
            List of all broken links across all files
        """
        all_broken_links = []

        for doc_file in doc_files:
            broken_links = self.validate_internal_links(doc_file)
            all_broken_links.extend(broken_links)

        return all_broken_links

    def _extract_internal_links(self, content: str) -> list[tuple[str, int]]:
        """Extract internal markdown links from content.

        Args:
            content: Markdown file content

        Returns:
            List of (link, line_number) tuples for internal links
        """
        links = []

        # Match markdown links: [text](link)
        link_pattern = re.compile(r'\]\(([^)]+)\)')

        for line_num, line in enumerate(content.split('\n'), start=1):
            for match in link_pattern.finditer(line):
                link = match.group(1)

                # Filter to internal links only (exclude HTTP/HTTPS, mailto, etc.)
                if not link.startswith(('http://', 'https://', 'mailto:')):
                    # Decode URL encoding if present
                    link = unquote(link)
                    links.append((link, line_num))

        return links


def validate_internal_links(
    doc_file: DocumentationFile,
    workspace_root: Path
) -> list[dict]:
    """Convenience function to validate internal links.

    Args:
        doc_file: DocumentationFile to validate
        workspace_root: Path to workspace root

    Returns:
        List of broken links
    """
    validator = LinkValidator(workspace_root)
    return validator.validate_internal_links(doc_file)


def validate_all_documentation_links(
    doc_files: list[DocumentationFile],
    workspace_root: Path
) -> list[dict]:
    """Convenience function to validate all documentation links.

    Args:
        doc_files: List of DocumentationFile entities
        workspace_root: Path to workspace root

    Returns:
        List of all broken links
    """
    validator = LinkValidator(workspace_root)
    return validator.validate_all_documentation_links(doc_files)
