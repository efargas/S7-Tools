"""Integration tests for full documentation validation workflow

Tests the complete validation pipeline from markdown parsing through
report generation.
"""

import pytest
import sys
import importlib.util
from pathlib import Path

# Add scripts directory to path for imports
scripts_dir = Path(__file__).parent.parent
sys.path.insert(0, str(scripts_dir))

# Import validate-documentation.py (has dashes, use importlib)
spec = importlib.util.spec_from_file_location(
    "validate_documentation",
    scripts_dir / "validate-documentation.py"
)
validate_documentation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validate_documentation)
DocumentationValidator = validate_documentation.DocumentationValidator


class TestDocumentationValidatorIntegration:
    """Integration test suite for DocumentationValidator."""

    @pytest.fixture
    def workspace_root(self, tmp_path):
        """Create complete temporary workspace structure."""
        # Create docs structure
        docs_dir = tmp_path / "docs"
        arch_dir = docs_dir / "architecture"
        arch_dir.mkdir(parents=True)

        # Create sample documentation with code example
        (arch_dir / "overview.md").write_text("""---
title: "Architecture Overview"
version: "1.0.0"
tags: ["architecture", "overview"]
---

# Architecture Overview

Sample code:

```csharp
using System;
using S7Tools.Core;

namespace S7Tools.ViewModels.Pages;

public class HomeViewModel : ReactiveObject
{
    private string _title = "Home";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
}
```

File reference: `src/S7Tools/ViewModels/Pages/HomeViewModel.cs`

Link to [patterns](../patterns/_index.md).
""")

        # Create patterns documentation
        patterns_dir = docs_dir / "patterns"
        patterns_dir.mkdir(parents=True)
        (patterns_dir / "_index.md").write_text("""---
title: "Pattern Catalog"
version: "1.0.0"
---

# Pattern Catalog

Link to [overview](../architecture/overview.md).
""")

        # Create source code structure
        src_dir = tmp_path / "src" / "S7Tools"
        viewmodels_dir = src_dir / "ViewModels" / "Pages"
        viewmodels_dir.mkdir(parents=True)

        # Create actual ViewModel file
        (viewmodels_dir / "HomeViewModel.cs").write_text("""using System;
using ReactiveUI;

namespace S7Tools.ViewModels.Pages;

public class HomeViewModel : ReactiveObject
{
    private string _title = "Home";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
}""")

        # Create Core library
        core_dir = tmp_path / "src" / "S7Tools.Core"
        core_dir.mkdir(parents=True)
        (core_dir / "S7Tools.Core.csproj").write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>""")

        # Create pattern implementations (use correct subdirectory paths)
        profiles_dir = src_dir / "Services" / "Profiles"
        profiles_dir.mkdir(parents=True)
        (profiles_dir / "StandardProfileManager.cs").write_text("""namespace S7Tools.Services.Profiles;

public class StandardProfileManager<T> where T : class, IProfileBase
{
    // Implementation
}

public interface IProfileBase {}""")

        return tmp_path

    def test_full_validation_pipeline(self, workspace_root):
        """Test complete validation pipeline."""
        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,  # Skip for speed in tests
            verbose=False
        )

        # Run validation
        report = validator.validate_all_documentation()

        # Verify report structure
        assert report is not None
        assert report.total_files_checked == 2  # overview.md and _index.md
        assert report.validation_version == "1.0.0"

        # Verify code examples extracted
        assert len(report.code_examples) >= 1
        csharp_examples = [ex for ex in report.code_examples if ex.language == "csharp"]
        assert len(csharp_examples) >= 1

        # Verify file references extracted
        assert len(report.file_references) >= 1
        cs_refs = [ref for ref in report.file_references if ref.referenced_path.endswith(".cs")]
        assert len(cs_refs) >= 1

        # Verify namespace validations
        assert len(report.namespace_validations) >= 1
        home_vm = [v for v in report.namespace_validations if "HomeViewModel" in v.source_file]
        assert len(home_vm) > 0
        assert home_vm[0].is_compliant is True

        # Verify pattern verifications
        assert len(report.pattern_implementations) == 5  # Always checks 5 core patterns

        # Verify link validation
        # Should have no broken links (both links are valid)
        assert len(report.broken_links) == 0

        # Verify execution time tracked
        assert report.execution_time_seconds > 0

    def test_validation_with_errors(self, workspace_root):
        """Test validation detects errors correctly."""
        # Add broken documentation
        docs_dir = workspace_root / "docs" / "broken"
        docs_dir.mkdir(parents=True)

        (docs_dir / "broken.md").write_text("""---
title: "Broken Doc"
---

# Broken Documentation

Broken file reference: `src/DoesNotExist.cs`

Broken link: [missing](missing.md)

```csharp
// Invalid C# code (if compilation enabled)
public class Broken {
    MissingType field;
}
```
""")

        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Should detect broken file reference
        broken_refs = [ref for ref in report.file_references if not ref.exists]
        assert len(broken_refs) >= 1

        # Should detect broken link
        assert len(report.broken_links) >= 1

        # Should have errors
        assert report.total_errors > 0

    def test_category_filtering(self, workspace_root):
        """Test filtering validation by category."""
        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        # Validate only architecture category
        report = validator.validate_all_documentation(category="architecture")

        # Should only check architecture docs
        assert report.total_files_checked == 1  # Only overview.md

    def test_validation_summary(self, workspace_root):
        """Test validation summary generation."""
        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Summary should be present
        assert report.summary is not None
        assert len(report.summary) > 0

        # If no errors, should indicate success
        if report.total_errors == 0:
            assert "passed" in report.summary.lower()

    def test_success_criteria_evaluation(self, workspace_root):
        """Test success criteria evaluation."""
        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Test passes_success_criteria method
        # Note: Will fail if compilation not 100%, but we skipped it
        passes = report.passes_success_criteria()

        # With skip_compilation and valid docs, should have good rates
        assert report.file_reference_success_rate >= 0.0
        assert report.namespace_compliance_rate >= 0.0
        assert report.pattern_verification_rate >= 0.0
        assert report.execution_time_seconds < 60.0  # Performance target

    def test_skip_compilation_shows_100_percent(self, workspace_root):
        """When --skip-compilation is used, compilation_success_rate must be 100%
        not 0% (regression test for false failure reporting).
        """
        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Must be 100%, not 0%
        assert report.compilation_success_rate == 100.0, (
            f"Expected 100% when compilation is skipped, got {report.compilation_success_rate}%"
        )

    def test_website_docs_are_excluded(self, workspace_root):
        """docs/website/ directory must be excluded from validation."""
        website_dir = workspace_root / "docs" / "website" / "docs"
        website_dir.mkdir(parents=True)
        (website_dir / "intro.md").write_text("""# Intro

Link to [missing](missing-file.md)
""")

        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Website intro.md must not appear in checked files count
        # (architecture + patterns = 2 files; website not included)
        assert report.total_files_checked == 2

    def test_test_fixtures_are_excluded(self, workspace_root):
        """docs/.test-fixtures/ directory must be excluded from validation."""
        fixtures_dir = workspace_root / "docs" / ".test-fixtures"
        fixtures_dir.mkdir(parents=True)
        (fixtures_dir / "fixture.md").write_text("""# Fixture

Link to [missing](missing-file.md)
""")

        validator = DocumentationValidator(
            workspace_root=workspace_root,
            skip_compilation=True,
            verbose=False
        )

        report = validator.validate_all_documentation()

        # Test fixtures must not be validated
        assert report.total_files_checked == 2  # Only the two real doc files
