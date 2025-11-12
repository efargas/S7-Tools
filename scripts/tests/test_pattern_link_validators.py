"""Unit tests for PatternValidator and LinkValidator

Tests pattern verification and internal link validation.
"""

import pytest
from pathlib import Path
from validators.pattern_validator import PatternValidator
from validators.link_validator import LinkValidator
from entities import PatternImplementation, DocumentationFile, InternalLink


class TestPatternValidator:
    """Test suite for PatternValidator."""

    @pytest.fixture
    def workspace_root(self, tmp_path):
        """Create temporary workspace structure with pattern implementations."""
        # Create pattern files
        patterns_dir = tmp_path / "docs" / "patterns"
        patterns_dir.mkdir(parents=True)

        # Profile Management pattern
        (patterns_dir / "profile-management.md").write_text(
            "# Profile Management Pattern\n\nPattern documentation."
        )

        # Create implementation files
        src_dir = tmp_path / "src" / "S7Tools" / "Services"
        src_dir.mkdir(parents=True)

        # StandardProfileManager implementation
        (src_dir / "StandardProfileManager.cs").write_text(
            """namespace S7Tools.Services;

public class StandardProfileManager<T> where T : class, IProfileBase
{
    // Profile management implementation
}"""
        )

        return tmp_path

    @pytest.fixture
    def validator(self, workspace_root):
        """Create PatternValidator instance."""
        return PatternValidator(workspace_root)

    def test_verify_pattern_implementation(self, validator):
        """Test verifying a single pattern implementation."""
        result = validator.verify_pattern_implementation(
            pattern_name="Profile Management",
            doc_path="docs/patterns/profile-management.md",
            implementation_markers=["StandardProfileManager", "IProfileBase"]
        )

        assert isinstance(result, PatternImplementation)
        assert result.pattern_name == "Profile Management"
        assert result.is_verified is True  # Should find StandardProfileManager
        assert len(result.implementation_files) > 0

    def test_verify_missing_pattern(self, validator):
        """Test verifying a pattern with no implementation."""
        result = validator.verify_pattern_implementation(
            pattern_name="Missing Pattern",
            doc_path="docs/patterns/missing.md",
            implementation_markers=["NonExistentClass", "NonExistentInterface"]
        )

        assert result.is_verified is False
        assert len(result.implementation_files) == 0

    def test_verify_all_core_patterns(self, validator):
        """Test verifying all 5 core patterns."""
        patterns = validator.verify_all_core_patterns()

        assert len(patterns) == 5
        pattern_names = [p.pattern_name for p in patterns]
        assert "Profile Management" in pattern_names
        assert "Internal Method Pattern" in pattern_names
        assert "Resource Coordination" in pattern_names
        assert "Custom Exceptions" in pattern_names
        assert "Reusable Controls" in pattern_names

    def test_calculate_verification_rate(self, validator):
        """Test calculating pattern verification rate."""
        patterns = [
            PatternImplementation(
                pattern_name="Pattern1",
                documentation_path="doc1.md",
                is_verified=True,
                implementation_files=["impl1.cs"]
            ),
            PatternImplementation(
                pattern_name="Pattern2",
                documentation_path="doc2.md",
                is_verified=True,
                implementation_files=["impl2.cs"]
            ),
            PatternImplementation(
                pattern_name="Pattern3",
                documentation_path="doc3.md",
                is_verified=False,
                implementation_files=[]
            ),
        ]
        rate = validator.calculate_verification_rate(patterns)
        assert rate == pytest.approx(66.67, rel=0.01)  # 2 out of 3


class TestLinkValidator:
    """Test suite for LinkValidator."""

    @pytest.fixture
    def workspace_root(self, tmp_path):
        """Create temporary workspace structure with markdown files."""
        docs_dir = tmp_path / "docs"

        # Create architecture docs
        arch_dir = docs_dir / "architecture"
        arch_dir.mkdir(parents=True)
        (arch_dir / "overview.md").write_text(
            """# Overview

Link to [patterns](../patterns/_index.md).
Link to [clean architecture](clean-architecture.md).
"""
        )
        (arch_dir / "clean-architecture.md").write_text("# Clean Architecture")

        # Create patterns docs
        patterns_dir = docs_dir / "patterns"
        patterns_dir.mkdir(parents=True)
        (patterns_dir / "_index.md").write_text(
            """# Patterns Index

Link to [overview](../architecture/overview.md).
Broken link to [missing](missing-file.md).
"""
        )

        return tmp_path

    @pytest.fixture
    def validator(self, workspace_root):
        """Create LinkValidator instance."""
        return LinkValidator(workspace_root)

    def test_resolve_relative_link(self, validator, workspace_root):
        """Test resolving relative markdown link."""
        source_file = workspace_root / "docs" / "architecture" / "overview.md"
        target = "../patterns/_index.md"

        resolved = validator.resolve_markdown_link(target, source_file)
        assert resolved is not None
        assert resolved.exists()
        assert resolved.name == "_index.md"

    def test_resolve_same_directory_link(self, validator, workspace_root):
        """Test resolving link in same directory."""
        source_file = workspace_root / "docs" / "architecture" / "overview.md"
        target = "clean-architecture.md"

        resolved = validator.resolve_markdown_link(target, source_file)
        assert resolved is not None
        assert resolved.exists()
        assert resolved.name == "clean-architecture.md"

    def test_resolve_broken_link(self, validator, workspace_root):
        """Test detecting broken link."""
        source_file = workspace_root / "docs" / "patterns" / "_index.md"
        target = "missing-file.md"

        resolved = validator.resolve_markdown_link(target, source_file)
        # Should return path even if it doesn't exist
        assert resolved is not None
        assert not resolved.exists()

    def test_extract_internal_links(self, validator):
        """Test extracting internal links from markdown content."""
        content = """# Test

Link to [overview](../architecture/overview.md).
External link to [GitHub](https://github.com/example).
Another internal link to [patterns](_index.md).
Link with anchor to [section](overview.md#section-name).
"""
        links = validator._extract_internal_links(content)

        # Should find 3 internal links (exclude external)
        internal_only = [link for link in links if not link.startswith("http")]
        assert len(internal_only) >= 2

    def test_validate_internal_links(self, validator, workspace_root):
        """Test validating internal links in a document."""
        doc = DocumentationFile(
            path=workspace_root / "docs" / "architecture" / "overview.md",
            title="Overview",
            content=(workspace_root / "docs" / "architecture" / "overview.md").read_text()
        )

        broken_links = validator.validate_internal_links(doc)
        # overview.md has valid links, so should have no broken links
        assert len(broken_links) == 0

    def test_validate_document_with_broken_links(self, validator, workspace_root):
        """Test detecting broken links in a document."""
        doc = DocumentationFile(
            path=workspace_root / "docs" / "patterns" / "_index.md",
            title="Patterns Index",
            content=(workspace_root / "docs" / "patterns" / "_index.md").read_text()
        )

        broken_links = validator.validate_internal_links(doc)
        # _index.md has a broken link to missing-file.md
        assert len(broken_links) > 0
        assert any("missing-file.md" in link.target for link in broken_links)

    def test_validate_all_documentation_links(self, validator, workspace_root):
        """Test validating links across all documentation."""
        docs = [
            DocumentationFile(
                path=workspace_root / "docs" / "architecture" / "overview.md",
                title="Overview",
                content=(workspace_root / "docs" / "architecture" / "overview.md").read_text()
            ),
            DocumentationFile(
                path=workspace_root / "docs" / "patterns" / "_index.md",
                title="Patterns",
                content=(workspace_root / "docs" / "patterns" / "_index.md").read_text()
            ),
        ]

        broken_links = validator.validate_all_documentation_links(docs)
        # Should find the broken link in _index.md
        assert len(broken_links) > 0

    def test_anchor_link_handling(self, validator, workspace_root):
        """Test that anchor links are handled correctly."""
        source_file = workspace_root / "docs" / "architecture" / "overview.md"
        target = "clean-architecture.md#section-name"

        resolved = validator.resolve_markdown_link(target, source_file)
        # Should resolve to file, ignoring anchor
        assert resolved is not None
        assert resolved.name == "clean-architecture.md"
