"""Unit tests for FileReferenceValidator and NamespaceValidator

Tests file path resolution and namespace convention validation.
"""

import pytest
from pathlib import Path
from validators.file_reference import FileReferenceValidator
from validators.namespace_validator import NamespaceValidator
from entities import FilePathReference, NamespaceValidation


class TestFileReferenceValidator:
    """Test suite for FileReferenceValidator."""

    @pytest.fixture
    def workspace_root(self, tmp_path):
        """Create temporary workspace structure."""
        # Create directory structure
        (tmp_path / "src" / "S7Tools" / "ViewModels").mkdir(parents=True)
        (tmp_path / "docs" / "architecture").mkdir(parents=True)

        # Create sample files
        (tmp_path / "src" / "S7Tools" / "ViewModels" / "HomeViewModel.cs").write_text(
            "namespace S7Tools.ViewModels;"
        )
        (tmp_path / "docs" / "architecture" / "overview.md").write_text("# Overview")

        return tmp_path

    @pytest.fixture
    def validator(self, workspace_root):
        """Create FileReferenceValidator instance."""
        return FileReferenceValidator(workspace_root)

    def test_validate_existing_file_reference(self, validator):
        """Test validation of existing file."""
        ref = FilePathReference(
            source_file="docs/architecture/overview.md",
            line_number=10,
            referenced_path="src/S7Tools/ViewModels/HomeViewModel.cs",
            path_type="project_relative",
            exists=False  # Will be updated by validator
        )
        validator.validate_file_reference(ref)
        # validate_file_reference modifies the ref object and returns it
        assert ref.exists is True
        assert ref.resolved_path is not None

    def test_validate_missing_file_reference(self, validator):
        """Test validation of non-existent file."""
        ref = FilePathReference(
            source_file="docs/test.md",
            line_number=5,
            referenced_path="src/DoesNotExist.cs",
            path_type="project_relative",
            exists=False
        )
        validator.validate_file_reference(ref)
        # validate_file_reference modifies the ref object and returns it
        assert ref.exists is False

    def test_resolve_absolute_path(self, validator, workspace_root):
        """Test resolving absolute path."""
        absolute_path = str(workspace_root / "src" / "S7Tools" / "ViewModels" / "HomeViewModel.cs")
        resolved = validator.resolve_path(absolute_path, "docs/test.md")
        assert resolved == Path(absolute_path)
        assert resolved.exists()

    def test_resolve_relative_path(self, validator, workspace_root):
        """Test resolving relative path from source file."""
        # From docs/architecture/overview.md to src/S7Tools/ViewModels/HomeViewModel.cs
        resolved = validator.resolve_path(
            "../../src/S7Tools/ViewModels/HomeViewModel.cs",
            str(workspace_root / "docs" / "architecture" / "overview.md")
        )
        assert resolved.exists()
        assert resolved.name == "HomeViewModel.cs"

    def test_resolve_project_relative_path(self, validator):
        """Test resolving project-relative path."""
        resolved = validator.resolve_path(
            "src/S7Tools/ViewModels/HomeViewModel.cs",
            "docs/test.md"
        )
        assert resolved.exists()
        assert resolved.name == "HomeViewModel.cs"

    def test_calculate_success_rate(self, validator):
        """Test calculating file reference success rate."""
        references = [
            FilePathReference("test.md", 1, "src/Exists1.cs", "project_relative", True),
            FilePathReference("test.md", 2, "src/Exists2.cs", "project_relative", True),
            FilePathReference("test.md", 3, "src/Missing.cs", "project_relative", False),
        ]
        rate = validator.calculate_success_rate(references)
        assert rate == pytest.approx(66.67, rel=0.01)  # 2 out of 3 = 66.67%

    def test_empty_references_list(self, validator):
        """Test success rate with empty list."""
        rate = validator.calculate_success_rate([])
        assert rate == 100.0


class TestNamespaceValidator:
    """Test suite for NamespaceValidator."""

    @pytest.fixture
    def workspace_root(self, tmp_path):
        """Create temporary workspace structure."""
        # Create ViewModels directory structure
        viewmodels_dir = tmp_path / "src" / "S7Tools" / "ViewModels" / "Pages"
        viewmodels_dir.mkdir(parents=True)

        # Create sample ViewModel with file-scoped namespace
        (viewmodels_dir / "HomeViewModel.cs").write_text(
            "namespace S7Tools.ViewModels.Pages;\n\npublic class HomeViewModel {}"
        )

        # Create ViewModel with traditional namespace
        (tmp_path / "src" / "S7Tools" / "ViewModels" / "Base").mkdir(parents=True)
        (tmp_path / "src" / "S7Tools" / "ViewModels" / "Base" / "ViewModelBase.cs").write_text(
            "namespace S7Tools.ViewModels.Base\n{\n    public class ViewModelBase {}\n}"
        )

        # Create non-compliant ViewModel
        (viewmodels_dir / "BadViewModel.cs").write_text(
            "namespace WrongNamespace;\n\npublic class BadViewModel {}"
        )

        return tmp_path

    @pytest.fixture
    def validator(self, workspace_root):
        """Create NamespaceValidator instance."""
        return NamespaceValidator(workspace_root)

    def test_extract_file_scoped_namespace(self, validator, workspace_root):
        """Test extracting file-scoped namespace (C# 10+)."""
        file_path = workspace_root / "src" / "S7Tools" / "ViewModels" / "Pages" / "HomeViewModel.cs"
        namespace = validator.extract_namespace_from_file(file_path)
        assert namespace == "S7Tools.ViewModels.Pages"

    def test_extract_traditional_namespace(self, validator, workspace_root):
        """Test extracting traditional namespace."""
        file_path = workspace_root / "src" / "S7Tools" / "ViewModels" / "Base" / "ViewModelBase.cs"
        namespace = validator.extract_namespace_from_file(file_path)
        assert namespace == "S7Tools.ViewModels.Base"

    def test_validate_compliant_namespace(self, validator, workspace_root):
        """Test validation of compliant namespace."""
        file_path = workspace_root / "src" / "S7Tools" / "ViewModels" / "Pages" / "HomeViewModel.cs"
        validation = validator.validate_namespace_convention(file_path, "Pages")

        assert isinstance(validation, NamespaceValidation)
        assert validation.is_compliant is True
        assert validation.declared_namespace == "S7Tools.ViewModels.Pages"
        # expected_pattern is template format, not resolved
        assert "{Category}" in validation.expected_pattern or "Pages" in validation.expected_pattern

    def test_validate_non_compliant_namespace(self, validator, workspace_root):
        """Test validation of non-compliant namespace."""
        file_path = workspace_root / "src" / "S7Tools" / "ViewModels" / "Pages" / "BadViewModel.cs"
        validation = validator.validate_namespace_convention(file_path, "Pages")

        assert validation.is_compliant is False
        assert validation.declared_namespace == "WrongNamespace"
        # expected_pattern is template format, not resolved
        assert "{Category}" in validation.expected_pattern or "Pages" in validation.expected_pattern

    def test_scan_viewmodels_and_views(self, validator):
        """Test scanning all ViewModels and Views."""
        validations = validator.scan_viewmodels_and_views()

        assert len(validations) > 0
        # At least one should be compliant (HomeViewModel)
        compliant = [v for v in validations if v.is_compliant]
        assert len(compliant) > 0

    def test_calculate_compliance_rate(self, validator):
        """Test calculating namespace compliance rate."""
        validations = [
            NamespaceValidation(
                source_file="test1.cs",
                declared_namespace="S7Tools.ViewModels.Pages",
                expected_pattern="S7Tools.ViewModels.Pages",
                is_compliant=True,
                category="Pages"
            ),
            NamespaceValidation(
                source_file="test2.cs",
                declared_namespace="Wrong",
                expected_pattern="S7Tools.ViewModels.Pages",
                is_compliant=False,
                category="Pages"
            ),
        ]
        rate = validator.calculate_compliance_rate(validations)
        assert rate == 50.0  # 1 out of 2 = 50%

    def test_valid_categories(self, validator):
        """Test that all expected categories are recognized."""
        expected_categories = [
            "Base", "Controls", "Dialogs", "Jobs", "Layout",
            "Pages", "Profiles", "Settings", "Tasks"
        ]
        assert validator.VALID_CATEGORIES == expected_categories
