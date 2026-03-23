"""File Path Reference Validator

Validates file path references in documentation by checking if they exist
in the repository. Supports absolute, relative, and project-relative paths.
"""

import re
from pathlib import Path
from typing import Optional
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import FilePathReference


class FileReferenceValidator:
    """Validates file path references in documentation."""

    def __init__(self, workspace_root: Path):
        """Initialize validator with workspace root.

        Args:
            workspace_root: Path to S7Tools workspace root
        """
        self.workspace_root = workspace_root

    def validate_file_reference(self, ref: FilePathReference) -> FilePathReference:
        """Validate a single file path reference.

        Args:
            ref: FilePathReference entity to validate

        Returns:
            Updated FilePathReference with exists and resolved_path set
        """
        resolved_path = self.resolve_path(ref.referenced_path, ref.source_file)

        if resolved_path and resolved_path.exists():
            ref.exists = True
            ref.resolved_path = str(resolved_path)
        else:
            ref.exists = False
            ref.resolved_path = None

        return ref

    def resolve_path(self, referenced_path: str, source_file: str) -> Optional[Path]:
        """Resolve a path reference to absolute path.

        Args:
            referenced_path: The path mentioned in documentation
            source_file: Documentation file containing the reference

        Returns:
            Resolved absolute Path or None if cannot be resolved
        """
        # Handle project-relative paths (src/..., docs/..., tests/...)
        if referenced_path.startswith(('src/', 'docs/', 'tests/', 'specs/', 'scripts/')):
            return self.workspace_root / referenced_path

        # Handle relative paths (../)
        if referenced_path.startswith('../'):
            source_path = Path(source_file)
            if source_path.is_absolute():
                return (source_path.parent / referenced_path).resolve()
            else:
                # Relative source path - resolve from workspace root
                abs_source = self.workspace_root / source_path
                return (abs_source.parent / referenced_path).resolve()

        # Handle absolute paths
        if referenced_path.startswith('/'):
            return Path(referenced_path)

        # Handle simple paths - try multiple resolution strategies
        # Strategy 1: Relative to source file's directory
        source_path = Path(source_file)
        abs_source = self.workspace_root / source_path if not source_path.is_absolute() else source_path
        candidate = (abs_source.parent / referenced_path).resolve()
        if candidate.exists():
            return candidate

        # Strategy 2: If source is in docs/, try relative to docs/ root
        if source_file.startswith('docs/'):
            docs_candidate = self.workspace_root / 'docs' / referenced_path
            if docs_candidate.exists():
                return docs_candidate

        # Strategy 3: Try from workspace root (fallback)
        return self.workspace_root / referenced_path

    def validate_all_references(
        self,
        references: list[FilePathReference]
    ) -> list[FilePathReference]:
        """Validate multiple file path references.

        Args:
            references: List of FilePathReference entities

        Returns:
            List of validated FilePathReference entities
        """
        validated = []
        for ref in references:
            validated_ref = self.validate_file_reference(ref)
            validated.append(validated_ref)
        return validated

    def calculate_success_rate(self, references: list[FilePathReference]) -> float:
        """Calculate percentage of valid file references.

        Args:
            references: List of validated FilePathReference entities

        Returns:
            Percentage of references that exist (0-100)
        """
        if not references:
            return 100.0

        valid_count = sum(1 for ref in references if ref.exists)
        return (valid_count / len(references)) * 100.0


def validate_file_reference(
    ref: FilePathReference,
    workspace_root: Path
) -> FilePathReference:
    """Convenience function to validate a file reference.

    Args:
        ref: FilePathReference to validate
        workspace_root: Path to workspace root

    Returns:
        Validated FilePathReference
    """
    validator = FileReferenceValidator(workspace_root)
    return validator.validate_file_reference(ref)


def validate_all_references(
    references: list[FilePathReference],
    workspace_root: Path
) -> list[FilePathReference]:
    """Convenience function to validate multiple file references.

    Args:
        references: List of FilePathReference entities
        workspace_root: Path to workspace root

    Returns:
        List of validated FilePathReference entities
    """
    validator = FileReferenceValidator(workspace_root)
    return validator.validate_all_references(references)
