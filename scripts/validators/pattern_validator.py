"""Pattern Implementation Validator

Verifies that documented architectural patterns exist in the codebase at
the locations specified in documentation.

Core patterns validated:
1. Unified Profile Management
2. Internal Method Pattern
3. Resource Coordination
4. Custom Exceptions
5. Reusable Controls
"""

import re
from pathlib import Path
from typing import Optional
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import PatternImplementation


class PatternValidator:
    """Validates architectural pattern implementations in codebase."""

    def __init__(self, workspace_root: Path):
        """Initialize validator with workspace root.

        Args:
            workspace_root: Path to S7Tools workspace root
        """
        self.workspace_root = workspace_root

    def verify_pattern_implementation(
        self,
        pattern_name: str,
        documented_location: str,
        expected_files: list[str]
    ) -> PatternImplementation:
        """Verify that a pattern is implemented as documented.

        Args:
            pattern_name: Name of the pattern (e.g., "Unified Profile Management")
            documented_location: Where the pattern is documented
            expected_files: List of files that should implement the pattern

        Returns:
            PatternImplementation entity with verification results
        """
        found_files = []
        missing_files = []
        extra_files = []

        # Check each expected file
        for expected_file in expected_files:
            file_path = self.workspace_root / expected_file
            if file_path.exists():
                found_files.append(expected_file)
            else:
                missing_files.append(expected_file)

        # Find additional implementations based on pattern
        pattern_files = self.find_pattern_markers(pattern_name)
        for pattern_file in pattern_files:
            if pattern_file not in expected_files:
                extra_files.append(pattern_file)

        # Pattern is verified if all expected files found
        is_verified = len(missing_files) == 0

        return PatternImplementation(
            pattern_name=pattern_name,
            documented_location=documented_location,
            expected_files=expected_files,
            found_files=found_files,
            is_verified=is_verified,
            missing_files=missing_files,
            extra_files=extra_files
        )

    def find_pattern_markers(self, pattern_name: str) -> list[str]:
        """Find files containing pattern markers in codebase.

        Pattern markers are interfaces, base classes, or method signatures
        that uniquely identify a pattern implementation.

        Args:
            pattern_name: Name of the pattern to find

        Returns:
            List of relative file paths containing pattern markers
        """
        markers = self._get_pattern_markers(pattern_name)
        if not markers:
            return []

        found_files = []
        src_dir = self.workspace_root / "src"

        # Search for markers in source files
        for cs_file in src_dir.rglob("*.cs"):
            try:
                content = cs_file.read_text(encoding='utf-8')
                for marker in markers:
                    if marker in content:
                        relative_path = str(cs_file.relative_to(self.workspace_root))
                        if relative_path not in found_files:
                            found_files.append(relative_path)
                        break
            except Exception:
                continue

        return found_files

    def verify_all_core_patterns(self) -> list[PatternImplementation]:
        """Verify all 5 core S7Tools patterns.

        Returns:
            List of PatternImplementation entities for all core patterns
        """
        patterns = []

        # 1. Unified Profile Management
        patterns.append(self.verify_pattern_implementation(
            pattern_name="Unified Profile Management",
            documented_location="docs/patterns/profile-management.md",
            expected_files=[
                "src/S7Tools/Services/StandardProfileManager.cs",
                "src/S7Tools.Core/Services/Interfaces/IProfileManager.cs",
                "src/S7Tools.Core/Services/Interfaces/IProfileBase.cs"
            ]
        ))

        # 2. Internal Method Pattern
        patterns.append(self.verify_pattern_implementation(
            pattern_name="Internal Method Pattern",
            documented_location="docs/patterns/internal-method.md",
            expected_files=[
                "src/S7Tools/Services/SocatService.cs",
                "src/S7Tools/Services/PowerSupplyService.cs"
            ]
        ))

        # 3. Resource Coordination
        patterns.append(self.verify_pattern_implementation(
            pattern_name="Resource Coordination",
            documented_location="docs/patterns/resource-coordination.md",
            expected_files=[
                "src/S7Tools/Services/Tasking/ResourceCoordinator.cs",
                "src/S7Tools.Core/Services/Interfaces/IResourceCoordinator.cs"
            ]
        ))

        # 4. Custom Exceptions
        patterns.append(self.verify_pattern_implementation(
            pattern_name="Custom Exceptions",
            documented_location="docs/patterns/custom-exceptions.md",
            expected_files=[
                "src/S7Tools.Core/Exceptions/S7ToolsException.cs",
                "src/S7Tools.Core/Exceptions/ProfileException.cs",
                "src/S7Tools.Core/Exceptions/ValidationException.cs",
                "src/S7Tools.Core/Exceptions/ConnectionException.cs"
            ]
        ))

        # 5. Reusable Controls
        patterns.append(self.verify_pattern_implementation(
            pattern_name="Reusable Controls",
            documented_location="docs/patterns/reusable-controls.md",
            expected_files=[
                "src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml",
                "src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml.cs",
                "src/S7Tools/ViewModels/Controls/SerialPortDiscoveryViewModel.cs"
            ]
        ))

        return patterns

    def calculate_verification_rate(
        self,
        patterns: list[PatternImplementation]
    ) -> float:
        """Calculate percentage of verified patterns.

        Args:
            patterns: List of PatternImplementation entities

        Returns:
            Percentage of verified patterns (0-100)
        """
        if not patterns:
            return 100.0

        verified_count = sum(1 for p in patterns if p.is_verified)
        return (verified_count / len(patterns)) * 100.0

    def _get_pattern_markers(self, pattern_name: str) -> list[str]:
        """Get unique markers for a pattern.

        Args:
            pattern_name: Name of the pattern

        Returns:
            List of code markers (interfaces, classes, methods) that identify the pattern
        """
        markers_map = {
            "Unified Profile Management": [
                "class StandardProfileManager<T>",
                "interface IProfileManager<T>",
                "interface IProfileBase"
            ],
            "Internal Method Pattern": [
                "Internal()",
                "InternalAsync(",
                "// Internal (assumes semaphore held)"
            ],
            "Resource Coordination": [
                "class ResourceCoordinator",
                "interface IResourceCoordinator"
            ],
            "Custom Exceptions": [
                "class S7ToolsException",
                "class ProfileException",
                "class ValidationException"
            ],
            "Reusable Controls": [
                "SerialPortDiscoveryControl",
                "SerialPortDiscoveryViewModel"
            ]
        }

        return markers_map.get(pattern_name, [])


def verify_pattern_implementation(
    pattern_name: str,
    documented_location: str,
    expected_files: list[str],
    workspace_root: Path
) -> PatternImplementation:
    """Convenience function to verify a pattern implementation.

    Args:
        pattern_name: Name of the pattern
        documented_location: Where the pattern is documented
        expected_files: List of files that should implement the pattern
        workspace_root: Path to workspace root

    Returns:
        PatternImplementation entity
    """
    validator = PatternValidator(workspace_root)
    return validator.verify_pattern_implementation(
        pattern_name,
        documented_location,
        expected_files
    )


def verify_all_core_patterns(workspace_root: Path) -> list[PatternImplementation]:
    """Convenience function to verify all core patterns.

    Args:
        workspace_root: Path to workspace root

    Returns:
        List of PatternImplementation entities
    """
    validator = PatternValidator(workspace_root)
    return validator.verify_all_core_patterns()
