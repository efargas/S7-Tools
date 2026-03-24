"""
Markdown parser and code extractor for S7Tools documentation validation.

This module uses mistune v3 to parse markdown files and extract:
- Code blocks with language tags (C#, Python, Bash, etc.)
- File path references (src/..., docs/..., relative paths)
- Internal markdown links ([text](../path/to/file.md))
- Simplified code example annotations (// ... simplified)
"""

import re
from pathlib import Path
from typing import Optional
import mistune

# Add parent directory to path for imports
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import DocumentationFile, CodeExample, FilePathReference


class MarkdownParser:
    """Parser for extracting validation-relevant content from markdown files."""

    def __init__(self):
        """Initialize markdown parser with AST renderer."""
        self.markdown = mistune.create_markdown(renderer='ast')

    def parse_markdown_file(self, file_path: Path) -> DocumentationFile:
        """
        Parse a markdown file and return DocumentationFile entity.

        Args:
            file_path: Absolute path to markdown file

        Returns:
            DocumentationFile entity with parsed content
        """
        content = file_path.read_text(encoding='utf-8')

        # Extract frontmatter if present
        frontmatter = self._extract_frontmatter(content)

        # Determine category from path
        category = self._infer_category(file_path)

        return DocumentationFile(
            path=file_path,
            relative_path=str(file_path.relative_to(file_path.parents[2])),  # Relative to repo root
            category=category,
            format="markdown",
            content=content,
            frontmatter=frontmatter,
            last_modified=file_path.stat().st_mtime,
            size_bytes=file_path.stat().st_size
        )

    def extract_code_blocks(self, content: str, source_file: str) -> list[CodeExample]:
        """
        Extract code blocks from markdown content.

        Args:
            content: Markdown file content
            source_file: Path to source file (for CodeExample entity)

        Returns:
            List of CodeExample entities
        """
        ast = self.markdown(content)
        code_examples = []

        # Walk AST to find code blocks
        line_number = 1
        for node in self._walk_ast(ast):
            # Skip non-dict nodes
            if not isinstance(node, dict):
                continue

            if node.get('type') == 'block_code':
                # Language info is in attrs dict
                attrs = node.get('attrs', {})
                language = attrs.get('info', '').lower().strip()
                code = node.get('raw', '')

                # Skip non-C# code blocks for now (focus on C# compilation)
                if language not in ['csharp', 'cs', 'c#']:
                    continue

                # Check for simplified annotation
                is_simplified = self._is_simplified_example(code)

                # Extract required usings
                required_usings = self._extract_usings(code)

                code_examples.append(CodeExample(
                    source_file=source_file,
                    line_number=line_number,
                    language='csharp',  # Normalize to 'csharp'
                    code=code,
                    is_simplified=is_simplified,
                    required_usings=required_usings
                ))

                line_number += code.count('\n')

        return code_examples

    def extract_file_references(self, content: str, source_file: str) -> list[FilePathReference]:
        """
        Extract file path references from markdown content.

        Patterns matched:
        - src/S7Tools/...
        - docs/...
        - tests/...
        - Relative paths: ../path/to/file

        Content inside fenced code blocks is excluded to avoid false positives
        from code examples that reference hypothetical or template paths.

        Args:
            content: Markdown file content
            source_file: Path to source file

        Returns:
            List of FilePathReference entities
        """
        references = []

        # Inline code patterns – these target explicit backtick-wrapped paths in prose
        inline_patterns = [
            r'`(src/[^`]+\.(cs|csproj|axaml|json))`',
            r'`(docs/[^`]+\.md)`',
            r'`(tests/[^`]+\.(cs|csproj))`',
        ]

        # Link patterns – match markdown link syntax [text](path)
        link_patterns = [
            r'\]\((\.\./[^)]+\.md)\)',       # Relative markdown links
            r'\]\(([^)]+\.(cs|md|json|axaml))\)',  # Any file in markdown links
        ]

        lines = content.split('\n')
        in_code_fence = False

        for line_num, line in enumerate(lines, start=1):
            # Track fenced code block boundaries
            stripped = line.strip()
            if stripped.startswith('```') or stripped.startswith('~~~'):
                in_code_fence = not in_code_fence
                continue

            # Always extract inline backtick patterns (safe – content is inside backticks)
            for pattern in inline_patterns:
                for match in re.finditer(pattern, line):
                    referenced_path = match.group(1)
                    path_type = "relative" if referenced_path.startswith('../') else (
                        "absolute" if referenced_path.startswith('/') else "project_relative"
                    )
                    references.append(FilePathReference(
                        source_file=source_file,
                        line_number=line_num,
                        referenced_path=referenced_path,
                        path_type=path_type,
                        exists=False
                    ))

            # Only extract link patterns outside fenced code blocks
            if not in_code_fence:
                # Temporarily remove inline code spans to avoid matching inside them
                line_no_inline = re.sub(r'`[^`]+`', '', line)
                for pattern in link_patterns:
                    for match in re.finditer(pattern, line_no_inline):
                        referenced_path = match.group(1)
                        path_type = "relative" if referenced_path.startswith('../') else (
                            "absolute" if referenced_path.startswith('/') else "project_relative"
                        )
                        references.append(FilePathReference(
                            source_file=source_file,
                            line_number=line_num,
                            referenced_path=referenced_path,
                            path_type=path_type,
                            exists=False
                        ))

        return references

    def extract_links(self, content: str) -> list[str]:
        """
        Extract internal markdown links from content.

        Args:
            content: Markdown file content

        Returns:
            List of internal link targets
        """
        links = []

        # Match markdown links: [text](link)
        link_pattern = r'\]\(([^)]+)\)'

        for match in re.finditer(link_pattern, content):
            link = match.group(1)

            # Filter to internal links only (exclude HTTP/HTTPS)
            if not link.startswith(('http://', 'https://', 'mailto:', '#')):
                links.append(link)

        return links

    def _extract_frontmatter(self, content: str) -> Optional[dict]:
        """Extract YAML frontmatter from markdown content."""
        frontmatter_pattern = r'^---\n(.*?)\n---'
        match = re.match(frontmatter_pattern, content, re.DOTALL)

        if match:
            try:
                import yaml
                return yaml.safe_load(match.group(1))
            except Exception:
                return None

        return None

    def _infer_category(self, file_path: Path) -> str:
        """Infer documentation category from file path."""
        parts = file_path.parts

        if 'architecture' in parts:
            return 'architecture'
        elif 'patterns' in parts:
            return 'patterns'
        elif 'guides' in parts:
            return 'guides'
        elif 'templates' in parts:
            return 'templates'
        elif 'reviews' in parts:
            return 'reviews'
        elif 'archive' in parts:
            return 'archive'
        else:
            return 'guides'  # Default fallback

    def _is_simplified_example(self, code: str) -> bool:
        """Check if code contains simplified example annotation.

        Detects various patterns indicating code is for illustration only:
        - // ... simplified
        - // simplified
        - // Simplified
        - // ... (ellipsis indicating omitted code)
        - /* ... */ (block comment ellipsis)
        - Contains placeholder markers like <TParameter>
        - Contains obvious incomplete syntax patterns
        """
        code_lower = code.lower()

        # Explicit simplified markers
        if 'simplified' in code_lower:
            return True

        # Ellipsis patterns (code omission indicators)
        if '// ...' in code or '/* ... */' in code:
            return True

        # Placeholder/template patterns
        if '<T' in code or '<TOptions' in code or '<TResult' in code:
            # Check if it's in a generic type declaration context
            # If it's standalone without class/interface definition, it's simplified
            if not re.search(r'(class|interface|struct)\s+\w+<T', code):
                return True

        # Check for incomplete/placeholder patterns
        incomplete_patterns = [
            r'//\s*\.\.\..*existing\s+code',  # // ... existing code
            r'//\s*implementation',           # // implementation
            r'//\s*work',                     # // work
            r'/\*\s*\.\.\.\s*\*/',           # /* ... */
        ]

        for pattern in incomplete_patterns:
            if re.search(pattern, code, re.IGNORECASE):
                return True

        return False

    def _extract_usings(self, code: str) -> list[str]:
        """Extract required using statements from C# code."""
        usings = []

        # Match: using System.Threading;
        using_pattern = r'using\s+([\w\.]+);'

        for match in re.finditer(using_pattern, code):
            namespace = match.group(1)
            usings.append(namespace)

        # Infer common usings from type usage
        if 'ReactiveObject' in code or 'ReactiveCommand' in code:
            usings.append('ReactiveUI')
        if 'ILogger' in code:
            usings.append('Microsoft.Extensions.Logging')
        if 'Task' in code and 'System.Threading.Tasks' not in usings:
            usings.append('System.Threading.Tasks')

        return list(set(usings))  # Remove duplicates

    def _walk_ast(self, node):
        """Recursively walk mistune AST."""
        yield node

        if isinstance(node, dict):
            children = node.get('children', [])
            for child in children:
                yield from self._walk_ast(child)
        elif isinstance(node, list):
            for item in node:
                yield from self._walk_ast(item)


def parse_markdown_file(path: Path) -> DocumentationFile:
    """Convenience function to parse a markdown file."""
    parser = MarkdownParser()
    return parser.parse_markdown_file(path)


def extract_code_blocks(content: str, source_file: str) -> list[CodeExample]:
    """Convenience function to extract code blocks."""
    parser = MarkdownParser()
    return parser.extract_code_blocks(content, source_file)


def extract_file_references(content: str, source_file: str) -> list[FilePathReference]:
    """Convenience function to extract file references."""
    parser = MarkdownParser()
    return parser.extract_file_references(content, source_file)


def extract_links(content: str) -> list[str]:
    """Convenience function to extract internal links."""
    parser = MarkdownParser()
    return parser.extract_links(content)
