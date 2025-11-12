"""Namespace Convention Validator

Validates that C# namespaces follow S7Tools conventions:
- ViewModels: S7Tools.ViewModels.{Category}
- Views: S7Tools.Views.{Category}
- Categories: Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks
"""

import re
from pathlib import Path
from typing import Optional
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import NamespaceValidation


class NamespaceValidator:
    """Validates namespace conventions in C# source files."""

    # Valid ViewModel/View categories
    VALID_CATEGORIES = [
        "Base", "Controls", "Dialogs", "Jobs", "Layout",
        "Pages", "Profiles", "Settings", "Tasks"
    ]

    # Namespace patterns
    VIEWMODEL_PATTERN = r"^S7Tools\.ViewModels\.(\w+)$"
    VIEW_PATTERN = r"^S7Tools\.Views\.(\w+)$"

    def __init__(self, workspace_root: Path):
        """Initialize validator with workspace root.

        Args:
            workspace_root: Path to S7Tools workspace root
        """
        self.workspace_root = workspace_root

    def extract_namespace_from_file(self, file_path: Path) -> Optional[str]:
        """Extract namespace declaration from C# file.

        Supports both traditional and file-scoped namespaces:
        - namespace S7Tools.ViewModels.Pages { }
        - namespace S7Tools.ViewModels.Pages;

        Args:
            file_path: Path to C# source file

        Returns:
            Namespace string or None if not found
        """
        try:
            content = file_path.read_text(encoding='utf-8')

            # Try file-scoped namespace first (C# 10+)
            file_scoped_pattern = r'^namespace\s+([\w\.]+)\s*;'
            match = re.search(file_scoped_pattern, content, re.MULTILINE)
            if match:
                return match.group(1)

            # Try traditional namespace
            traditional_pattern = r'namespace\s+([\w\.]+)\s*\{'
            match = re.search(traditional_pattern, content)
            if match:
                return match.group(1)

            return None
        except Exception:
            return None

    def validate_namespace_convention(
        self,
        file_path: Path,
        category: Optional[str] = None
    ) -> NamespaceValidation:
        """Validate namespace follows S7Tools conventions.

        Args:
            file_path: Path to C# source file
            category: Optional category override (inferred from path if None)

        Returns:
            NamespaceValidation entity with compliance status
        """
        # Extract namespace from file
        declared_namespace = self.extract_namespace_from_file(file_path)

        if not declared_namespace:
            return NamespaceValidation(
                source_file=str(file_path.relative_to(self.workspace_root)),
                declared_namespace="",
                expected_pattern="N/A",
                is_compliant=False,
                category=None,
                violation_details="No namespace declaration found"
            )

        # Infer category from path if not provided
        if category is None:
            category = self._infer_category_from_path(file_path)

        # Determine expected pattern based on file location
        if "ViewModels" in str(file_path):
            expected_pattern = f"S7Tools.ViewModels.{{Category}}"
            expected_namespace = f"S7Tools.ViewModels.{category}" if category else None
        elif "Views" in str(file_path):
            expected_pattern = f"S7Tools.Views.{{Category}}"
            expected_namespace = f"S7Tools.Views.{category}" if category else None
        else:
            # Not a ViewModel or View - no strict convention enforced
            return NamespaceValidation(
                source_file=str(file_path.relative_to(self.workspace_root)),
                declared_namespace=declared_namespace,
                expected_pattern="N/A (not ViewModel/View)",
                is_compliant=True,
                category=None,
                violation_details=None
            )

        # Check compliance
        is_compliant = declared_namespace == expected_namespace
        violation_details = None
        if not is_compliant:
            violation_details = f"Expected: {expected_namespace}, Found: {declared_namespace}"

        return NamespaceValidation(
            source_file=str(file_path.relative_to(self.workspace_root)),
            declared_namespace=declared_namespace,
            expected_pattern=expected_pattern,
            is_compliant=is_compliant,
            category=category,
            violation_details=violation_details
        )

    def scan_viewmodels_and_views(self) -> list[NamespaceValidation]:
        """Scan all ViewModels and Views and validate namespaces.

        Returns:
            List of NamespaceValidation entities
        """
        validations = []

        # Scan ViewModels
        viewmodels_dir = self.workspace_root / "src" / "S7Tools" / "ViewModels"
        if viewmodels_dir.exists():
            for cs_file in viewmodels_dir.rglob("*.cs"):
                validation = self.validate_namespace_convention(cs_file)
                validations.append(validation)

        # Scan Views
        views_dir = self.workspace_root / "src" / "S7Tools" / "Views"
        if views_dir.exists():
            for cs_file in views_dir.rglob("*.cs"):
                # Skip .axaml.cs files (code-behind - namespace in .axaml)
                if cs_file.suffix == ".cs" and not cs_file.stem.endswith(".axaml"):
                    validation = self.validate_namespace_convention(cs_file)
                    validations.append(validation)

        return validations

    def calculate_compliance_rate(
        self,
        validations: list[NamespaceValidation]
    ) -> float:
        """Calculate percentage of compliant namespaces.

        Args:
            validations: List of NamespaceValidation entities

        Returns:
            Percentage of compliant namespaces (0-100)
        """
        if not validations:
            return 100.0

        compliant_count = sum(1 for v in validations if v.is_compliant)
        return (compliant_count / len(validations)) * 100.0

    def _infer_category_from_path(self, file_path: Path) -> Optional[str]:
        """Infer category from file path.

        Args:
            file_path: Path to C# source file

        Returns:
            Category name or None if cannot be inferred
        """
        parts = file_path.parts

        # Look for category folders in path
        for category in self.VALID_CATEGORIES:
            if category in parts:
                return category

        return None


def extract_namespace_from_file(file_path: Path) -> Optional[str]:
    """Convenience function to extract namespace from file.

    Args:
        file_path: Path to C# source file

    Returns:
        Namespace string or None
    """
    validator = NamespaceValidator(Path.cwd())
    return validator.extract_namespace_from_file(file_path)


def validate_namespace_convention(
    file_path: Path,
    workspace_root: Path,
    category: Optional[str] = None
) -> NamespaceValidation:
    """Convenience function to validate namespace convention.

    Args:
        file_path: Path to C# source file
        workspace_root: Path to workspace root
        category: Optional category override

    Returns:
        NamespaceValidation entity
    """
    validator = NamespaceValidator(workspace_root)
    return validator.validate_namespace_convention(file_path, category)


def scan_viewmodels_and_views(workspace_root: Path) -> list[NamespaceValidation]:
    """Convenience function to scan all ViewModels and Views.

    Args:
        workspace_root: Path to workspace root

    Returns:
        List of NamespaceValidation entities
    """
    validator = NamespaceValidator(workspace_root)
    return validator.scan_viewmodels_and_views()
