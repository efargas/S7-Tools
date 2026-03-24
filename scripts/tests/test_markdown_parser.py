"""
Unit tests for markdown parser and code extractor.

Tests extraction of code blocks, file references, and links.
"""

import pytest
from pathlib import Path
import tempfile

# Add parent directory to path for imports
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from extractors.markdown_parser import (
    MarkdownParser,
    parse_markdown_file,
    extract_code_blocks,
    extract_file_references,
    extract_links
)


class TestMarkdownParser:
    """Tests for MarkdownParser class."""

    def test_parse_markdown_file(self, tmp_path):
        """Test parsing a complete markdown file."""
        # Create test markdown file
        test_file = tmp_path / "docs" / "patterns" / "test.md"
        test_file.parent.mkdir(parents=True, exist_ok=True)

        content = """---
title: Test Pattern
version: 1.0.0
---

# Test Pattern

This is a test pattern.
"""
        test_file.write_text(content)

        doc_file = parse_markdown_file(test_file)

        assert doc_file.path == test_file
        assert doc_file.category == "patterns"
        assert doc_file.format == "markdown"
        assert doc_file.frontmatter is not None
        assert doc_file.frontmatter.get('title') == 'Test Pattern'

    def test_extract_code_blocks_csharp(self):
        """Test extracting C# code blocks."""
        content = """
# Test Document

Here's a code example:

```csharp
using System;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
}
```

Another example:

```csharp
// ... simplified
public class SimplifiedExample { }
```
"""

        examples = extract_code_blocks(content, "test.md")

        assert len(examples) == 2
        assert examples[0].language == "csharp"
        assert "public class MyService" in examples[0].code
        assert examples[0].is_simplified is False
        assert "System" in examples[0].required_usings

        # Second example is simplified
        assert examples[1].is_simplified is True

    def test_extract_file_references(self):
        """Test extracting file path references."""
        content = """
# Test Document

File references:
- `src/S7Tools/Services/MyService.cs`
- `docs/patterns/profile-management.md`
- `tests/S7Tools.Tests/MyTest.cs`

And a relative link: [related pattern](../patterns/other-pattern.md)
"""

        references = extract_file_references(content, "test.md")

        assert len(references) >= 3

        # Check project-relative paths
        src_refs = [r for r in references if r.referenced_path.startswith('src/')]
        assert len(src_refs) >= 1
        assert src_refs[0].path_type == "project_relative"

        # Check relative paths
        relative_refs = [r for r in references if r.path_type == "relative"]
        assert len(relative_refs) >= 1

    def test_extract_links(self):
        """Test extracting internal markdown links."""
        content = """
# Test Document

See also:
- [Profile Management](../patterns/profile-management.md)
- [Architecture Overview](../architecture/overview.md)
- [External link](https://example.com) (should be ignored)
"""

        links = extract_links(content)

        # Should find 2 internal links (exclude external)
        assert len(links) >= 2
        assert any('profile-management.md' in link for link in links)
        assert not any('example.com' in link for link in links)

    def test_infer_category(self):
        """Test category inference from file path."""
        parser = MarkdownParser()

        patterns_path = Path("/repo/docs/patterns/test.md")
        assert parser._infer_category(patterns_path) == "patterns"

        guides_path = Path("/repo/docs/guides/test.md")
        assert parser._infer_category(guides_path) == "guides"

        architecture_path = Path("/repo/docs/architecture/test.md")
        assert parser._infer_category(architecture_path) == "architecture"

    def test_is_simplified_example(self):
        """Test simplified example detection."""
        parser = MarkdownParser()

        simplified_code = "public class Test { }\n// ... simplified"
        assert parser._is_simplified_example(simplified_code) is True

        normal_code = "public class Test { }"
        assert parser._is_simplified_example(normal_code) is False

    def test_extract_usings(self):
        """Test using statement extraction."""
        parser = MarkdownParser()

        code = """
using System;
using System.Threading;
using ReactiveUI;

public class MyClass : ReactiveObject
{
    private readonly ILogger<MyClass> _logger;
}
"""

        usings = parser._extract_usings(code)

        assert "System" in usings
        assert "System.Threading" in usings
        assert "ReactiveUI" in usings
        assert "Microsoft.Extensions.Logging" in usings  # Inferred from ILogger


class TestCodeExampleExtraction:
    """Tests for code example extraction edge cases."""

    def test_extract_multiple_languages(self):
        """Test that only C# code blocks are extracted."""
        content = """
```csharp
public class CSharpClass { }
```

```python
class PythonClass:
    pass
```

```bash
echo "Hello World"
```
"""

        examples = extract_code_blocks(content, "test.md")

        # Only C# should be extracted
        assert len(examples) == 1
        assert examples[0].language == "csharp"

    def test_extract_with_language_variants(self):
        """Test extraction with different C# language tags."""
        content = """
```cs
public class Test1 { }
```

```c#
public class Test2 { }
```

```csharp
public class Test3 { }
```
"""

        examples = extract_code_blocks(content, "test.md")

        # All should be extracted and normalized to 'csharp'
        assert len(examples) == 3
        assert all(ex.language == "csharp" for ex in examples)


class TestFileReferenceExtraction:
    """Tests for file reference extraction edge cases."""

    def test_extract_various_extensions(self):
        """Test extraction of different file types."""
        content = """
- `src/S7Tools/MyService.cs`
- `src/S7Tools/S7Tools.csproj`
- `src/S7Tools/Views/MainWindow.axaml`
- `docs/.metadata/validation-report.json`
"""

        references = extract_file_references(content, "test.md")

        # Check that various extensions are captured
        extensions = [ref.referenced_path.split('.')[-1] for ref in references]
        assert 'cs' in extensions
        assert 'csproj' in extensions or 'axaml' in extensions


class TestLinkExtraction:
    """Tests for link extraction edge cases."""

    def test_exclude_anchors(self):
        """Test that anchor links are excluded."""
        content = """
- [Section](#section-name)
- [Other file](other-file.md)
"""

        links = extract_links(content)

        # Anchor links should be excluded
        assert not any(link.startswith('#') for link in links)

    def test_exclude_external_links(self):
        """Test that external links are excluded."""
        content = """
- [External](https://example.com)
- [Email](mailto:test@example.com)
- [Internal](./internal.md)
"""

        links = extract_links(content)

        # Only internal link should be included
        assert not any('http' in link for link in links)
        assert not any('mailto' in link for link in links)
        assert len(links) >= 1


class TestCodeBlockFiltering:
    """Tests for code block filtering in file reference and link extraction."""

    def test_file_refs_excluded_from_fenced_block(self):
        """Paths inside fenced code blocks must not be extracted as file references."""
        content = """\
Normal prose references:
- `src/S7Tools/Services/Profiles/StandardProfileManager.cs`

```text
src/Does/Not/Exist.cs
docs/imaginary-file.md
tests/Fake/FakeTest.cs
```
"""
        references = extract_file_references(content, "test.md")
        paths = [r.referenced_path for r in references]

        # Prose reference must be captured
        assert "src/S7Tools/Services/Profiles/StandardProfileManager.cs" in paths
        # Paths inside fenced block must NOT be captured
        assert "src/Does/Not/Exist.cs" not in paths
        assert "docs/imaginary-file.md" not in paths
        assert "tests/Fake/FakeTest.cs" not in paths

    def test_file_refs_excluded_from_tilde_fence(self):
        """Paths inside tilde-fenced code blocks must not be extracted."""
        content = """\
~~~bash
src/Fake/Service.cs
tests/Fake/Test.cs
~~~

- `src/S7Tools/Services/Socat/SocatService.cs`
"""
        references = extract_file_references(content, "test.md")
        paths = [r.referenced_path for r in references]

        assert "src/Fake/Service.cs" not in paths
        assert "tests/Fake/Test.cs" not in paths
        assert "src/S7Tools/Services/Socat/SocatService.cs" in paths

    def test_markdown_links_excluded_from_fenced_block(self):
        """Markdown link syntax inside fenced blocks must not produce file references."""
        content = """\
Real link: [see pattern](../patterns/profile.md)

```markdown
[fake link inside fence](totally-imaginary.md)
```
"""
        references = extract_file_references(content, "test.md")
        paths = [r.referenced_path for r in references]

        assert "totally-imaginary.md" not in paths

    def test_links_in_inline_code_not_extracted(self):
        """Links inside backtick inline code must not be reported as broken links."""
        content = "The text `[not a link](no-such-file.md)` is inline code, not a link.\n"
        references = extract_file_references(content, "test.md")
        paths = [r.referenced_path for r in references]

        assert "no-such-file.md" not in paths

    def test_vbnet_method_call_not_extracted_as_link(self):
        """VB.NET method calls like _random.[Next](0, count) inside a fenced block
        must not be reported as broken internal links.
        """
        content = """\
```vbnet
Dim index As Integer = _random.[Next](0, _eventNames.Count)
Return _messages(_random.[Next](0, _messages.Count))
```
"""
        references = extract_file_references(content, "test.md")
        assert references == []

    def test_four_backtick_fence_handled(self):
        """Four-backtick fences must be treated as code blocks."""
        content = """\
````markdown
[fake link](fake-document.md)
src/Does/Not/Exist.cs
````
"""
        references = extract_file_references(content, "test.md")
        paths = [r.referenced_path for r in references]

        assert "fake-document.md" not in paths
        assert "src/Does/Not/Exist.cs" not in paths


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
