"""Unit tests for validate-frontmatter.py

Tests frontmatter validation including website exclusion, required fields,
date formats, semantic versioning, status values, and the test-fixtures
exclusion.
"""

import importlib.util
import sys
from pathlib import Path

import pytest

# Load validate-frontmatter.py (contains dashes, use importlib)
scripts_dir = Path(__file__).parent.parent
spec = importlib.util.spec_from_file_location(
    "validate_frontmatter",
    scripts_dir / "validate-frontmatter.py",
)
validate_frontmatter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validate_frontmatter)
FrontmatterValidator = validate_frontmatter.FrontmatterValidator


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_docs(tmp_path, files: dict[str, str]) -> Path:
    """Create files under tmp_path/docs and return the docs root."""
    docs = tmp_path / "docs"
    for rel, content in files.items():
        target = docs / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(content, encoding="utf-8")
    return docs


VALID_FM = """\
---
title: "Test Document"
version: "1.0.0"
created: "2026-01-01"
last-updated: "2026-03-24"
status: "current"
tags: ["test"]
---

# Test Document

Content here.
"""


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


class TestFrontmatterValidatorBasics:
    """Tests for well-formed frontmatter acceptance."""

    def test_valid_document_passes(self, tmp_path):
        """A document with all required fields in correct format should have 0 errors."""
        docs = _make_docs(tmp_path, {"guides/valid.md": VALID_FM})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        assert v.summary.errors == 0
        assert v.summary.files_checked == 1

    def test_no_frontmatter_reports_error(self, tmp_path):
        """A document with no frontmatter block should report META-001."""
        docs = _make_docs(tmp_path, {"guides/no-fm.md": "# No Frontmatter\n\nContent.\n"})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        assert v.summary.errors >= 1
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-001" in rule_ids

    def test_missing_required_fields(self, tmp_path):
        """Missing required fields each produce a META-001 error."""
        content = """\
---
title: "Only Title"
---

# Only Title
"""
        docs = _make_docs(tmp_path, {"guides/missing.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        # version, created, last-updated, status, tags are all missing
        assert v.summary.errors >= 5

    def test_invalid_semver_version(self, tmp_path):
        """Non-semver version string should produce META-002 error."""
        content = VALID_FM.replace('version: "1.0.0"', 'version: "not-semver"')
        docs = _make_docs(tmp_path, {"guides/bad-ver.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-002" in rule_ids

    def test_invalid_date_format(self, tmp_path):
        """Date in MM/DD/YYYY format should produce META-003 error."""
        content = VALID_FM.replace('created: "2026-01-01"', 'created: "01/01/2026"')
        docs = _make_docs(tmp_path, {"guides/bad-date.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-003" in rule_ids

    def test_invalid_status_value(self, tmp_path):
        """Unknown status value should produce META-004 error."""
        content = VALID_FM.replace('status: "current"', 'status: "unknown-status"')
        docs = _make_docs(tmp_path, {"guides/bad-status.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-004" in rule_ids

    def test_empty_tags_list(self, tmp_path):
        """An empty tags list should produce META-005 error."""
        content = VALID_FM.replace("tags: [\"test\"]", "tags: []")
        docs = _make_docs(tmp_path, {"guides/empty-tags.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-005" in rule_ids


class TestFrontmatterDirectoryExclusion:
    """Tests for directory exclusion rules."""

    def test_website_directory_excluded(self, tmp_path):
        """Files under docs/website/ must be skipped (they follow different frontmatter)."""
        docs = _make_docs(tmp_path, {
            "website/blog/post.md": "# Blog Post\n\nNo frontmatter.\n",
            "website/docs/intro.md": "# Intro\n\nNo frontmatter.\n",
            "guides/valid.md": VALID_FM,
        })
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        # Only guides/valid.md should be checked (website files skipped)
        assert v.summary.files_checked == 1
        assert v.summary.errors == 0

    def test_metadata_directory_excluded(self, tmp_path):
        """Files under docs/.metadata/ must be skipped."""
        docs = _make_docs(tmp_path, {
            ".metadata/report.md": "# Report\n\nNo frontmatter.\n",
            "guides/valid.md": VALID_FM,
        })
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        assert v.summary.files_checked == 1
        assert v.summary.errors == 0

    def test_test_fixtures_directory_excluded(self, tmp_path):
        """Files under docs/.test-fixtures/ must be skipped."""
        docs = _make_docs(tmp_path, {
            ".test-fixtures/fixture.md": "# Fixture\n\nNo frontmatter.\n",
            "guides/valid.md": VALID_FM,
        })
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        assert v.summary.files_checked == 1
        assert v.summary.errors == 0


class TestFrontmatterDeprecatedDocuments:
    """Tests for deprecated document validation rules."""

    def test_deprecated_requires_deprecated_date(self, tmp_path):
        """Deprecated documents without deprecated-date must produce META-007 error."""
        content = VALID_FM.replace('status: "current"', 'status: "deprecated"')
        docs = _make_docs(tmp_path, {"guides/dep.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-007" in rule_ids

    def test_deprecated_without_superseded_by_warns(self, tmp_path):
        """Deprecated documents without superseded-by should produce META-008 warning."""
        content = VALID_FM.replace(
            'status: "current"',
            'status: "deprecated"'
        ) + "deprecated-date: \"2026-01-01\"\n"
        docs = _make_docs(tmp_path, {"guides/dep2.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        rule_ids = [vi.rule_id for vi in v.summary.violations]
        assert "META-008" in rule_ids

    def test_valid_deprecated_document(self, tmp_path):
        """A fully valid deprecated document should have no errors."""
        content = """\
---
title: "Old Document"
version: "1.0.0"
created: "2025-01-01"
last-updated: "2026-01-01"
status: "deprecated"
tags: ["archive"]
deprecated-date: "2026-01-01"
superseded-by: "guides/new-document.md"
---

# Old Document

This is deprecated.
"""
        # Create the superseded-by file too so META-006 doesn't fire
        docs = _make_docs(tmp_path, {
            "guides/old.md": content,
            "guides/new-document.md": VALID_FM,
        })
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        errors = [vi for vi in v.summary.violations if vi.severity == "error"]
        assert len(errors) == 0


class TestFrontmatterSemver:
    """Tests for semantic version validation."""

    @pytest.mark.parametrize("version", [
        "1.0.0",
        "2.1.3",
        "0.0.1",
        "10.20.30",
        "1.0.0-beta.1",
        "1.0.0-alpha.1",
        "3.2.1+build.123",
    ])
    def test_valid_semver_accepted(self, tmp_path, version):
        """All valid semver strings should be accepted without error."""
        import re as _re
        content = VALID_FM.replace('version: "1.0.0"', f'version: "{version}"')
        safe_name = _re.sub(r'[^a-zA-Z0-9_]', '_', version)
        docs = _make_docs(tmp_path, {f"guides/v{safe_name}.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        meta002 = [vi for vi in v.summary.violations if vi.rule_id == "META-002"]
        assert meta002 == [], f"Unexpected META-002 for version '{version}': {meta002}"

    @pytest.mark.parametrize("version", [
        "1.0",
        "v1.0.0",
        "1",
        "not-semver",
        "1.0.0.0",
    ])
    def test_invalid_semver_rejected(self, tmp_path, version):
        """Non-semver strings must produce META-002 error."""
        import re as _re
        content = VALID_FM.replace('version: "1.0.0"', f'version: "{version}"')
        safe_name = _re.sub(r'[^a-zA-Z0-9_]', '_', version)
        docs = _make_docs(tmp_path, {f"guides/bad_{safe_name}.md": content})
        v = FrontmatterValidator(str(docs))
        v.validate_all()
        meta002 = [vi for vi in v.summary.violations if vi.rule_id == "META-002"]
        assert meta002, f"Expected META-002 for invalid version '{version}'"


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
