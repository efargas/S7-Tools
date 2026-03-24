"""C# Code Compilation Validator

Validates C# code examples from documentation by compiling them with dotnet CLI.
Supports batch compilation, timeout handling, and detailed error reporting.
"""

import subprocess
import tempfile
import shutil
from pathlib import Path
from typing import Optional
from datetime import datetime
import time
import re
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from entities import CodeExample, CompilationResult


class CodeCompiler:
    """Compiles C# code examples using dotnet CLI."""

    # Template .csproj for compilation with S7Tools references
    CSPROJ_TEMPLATE = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="ReactiveUI" Version="19.5.31" />
  </ItemGroup>

  <!-- Reference S7Tools projects if available -->
  <ItemGroup Condition="Exists('../../src/S7Tools.Core/S7Tools.Core.csproj')">
    <ProjectReference Include="../../src/S7Tools.Core/S7Tools.Core.csproj" />
  </ItemGroup>
</Project>
"""

    # Default usings for S7Tools code
    DEFAULT_USINGS = [
        "using System;",
        "using System.Collections.Generic;",
        "using System.Linq;",
        "using System.Threading.Tasks;",
    ]

    # Compilation timeout (30 seconds per research.md risk mitigation)
    COMPILATION_TIMEOUT = 30.0

    def __init__(self, workspace_root: Optional[Path] = None):
        """Initialize compiler with workspace root for project references.

        Args:
            workspace_root: Path to S7Tools workspace root (for ProjectReference resolution)
        """
        self.workspace_root = workspace_root or Path.cwd()

    def compile_csharp_example(
        self,
        code: CodeExample,
        additional_usings: Optional[list[str]] = None
    ) -> CompilationResult:
        """Compile a single C# code example.

        Args:
            code: CodeExample entity with code to compile
            additional_usings: Optional list of additional using statements

        Returns:
            CompilationResult with success status, errors, warnings, and execution time
        """
        # Skip compilation for simplified examples
        if code.is_simplified:
            return CompilationResult(
                success=True,
                errors=[],
                warnings=[],
                execution_time_ms=0,
                exit_code=0
            )

        # Create temporary project
        temp_dir = None
        try:
            temp_dir = self.create_temp_project(
                code.code,
                code.required_usings + (additional_usings or [])
            )

            # Compile using dotnet build
            start_time = time.time()
            result = subprocess.run(
                ["dotnet", "build", str(temp_dir / "TempProject.csproj")],
                capture_output=True,
                text=True,
                timeout=self.COMPILATION_TIMEOUT,
                cwd=temp_dir
            )
            execution_time = int((time.time() - start_time) * 1000)  # Convert to ms as int

            # Parse compilation output
            errors = self._parse_errors(result.stdout + result.stderr)
            warnings = self._parse_warnings(result.stdout + result.stderr)

            return CompilationResult(
                success=result.returncode == 0 and len(errors) == 0,
                errors=errors,
                warnings=warnings,
                execution_time_ms=execution_time,
                exit_code=result.returncode
            )

        except subprocess.TimeoutExpired:
            return CompilationResult(
                success=False,
                errors=[f"Compilation timed out after {self.COMPILATION_TIMEOUT}s"],
                warnings=[],
                execution_time_ms=int(self.COMPILATION_TIMEOUT * 1000),
                exit_code=-1
            )
        except Exception as e:
            return CompilationResult(
                success=False,
                errors=[f"Compilation failed: {str(e)}"],
                warnings=[],
                execution_time_ms=0,
                exit_code=-1
            )
        finally:
            # Cleanup temp directory
            if temp_dir and temp_dir.exists():
                shutil.rmtree(temp_dir, ignore_errors=True)

    def create_temp_project(
        self,
        code: str,
        usings: list[str]
    ) -> Path:
        """Create temporary .NET project with code and usings.

        Args:
            code: C# code to compile
            usings: List of using statements to include

        Returns:
            Path to temporary project directory
        """
        temp_dir = Path(tempfile.mkdtemp(prefix="s7tools_validate_"))

        # Create .csproj file
        csproj_path = temp_dir / "TempProject.csproj"
        csproj_path.write_text(self.CSPROJ_TEMPLATE)

        # Create .cs file with usings and code
        cs_path = temp_dir / "TempCode.cs"
        cs_content = self._build_cs_file(code, usings)
        cs_path.write_text(cs_content)

        return temp_dir

    def batch_compile_examples(
        self,
        examples: list[CodeExample]
    ) -> list[CompilationResult]:
        """Compile multiple code examples.

        Args:
            examples: List of CodeExample entities to compile

        Returns:
            List of CompilationResult entities (one per example)
        """
        results = []
        for example in examples:
            result = self.compile_csharp_example(example)
            results.append(result)
        return results

    def _build_cs_file(self, code: str, usings: list[str]) -> str:
        """Build complete .cs file with usings and code.

        Args:
            code: C# code snippet
            usings: List of using statements

        Returns:
            Complete C# file content
        """
        # Combine default usings with provided usings
        all_usings = set(self.DEFAULT_USINGS + usings)
        using_lines = "\n".join(sorted(all_usings))

        # Wrap code in namespace if not already present
        if "namespace " not in code:
            return f"""{using_lines}

namespace TempValidation;

{code}
"""
        else:
            return f"""{using_lines}

{code}
"""

    def _parse_errors(self, output: str) -> list[str]:
        """Extract compilation errors from dotnet build output.

        Args:
            output: Raw compiler output

        Returns:
            List of error messages
        """
        errors = []
        # Match lines like "Program.cs(5,10): error CS0246: ..."
        error_pattern = re.compile(r".*:\s*error\s+(CS\d+):\s*(.+)")
        for line in output.splitlines():
            match = error_pattern.search(line)
            if match:
                error_code = match.group(1)
                error_msg = match.group(2).strip()
                errors.append(f"{error_code}: {error_msg}")
        return errors

    def _parse_warnings(self, output: str) -> list[str]:
        """Extract compilation warnings from dotnet build output.

        Args:
            output: Raw compiler output

        Returns:
            List of warning messages
        """
        warnings = []
        # Match lines like "Program.cs(5,10): warning CS0168: ..."
        warning_pattern = re.compile(r".*:\s*warning\s+(CS\d+):\s*(.+)")
        for line in output.splitlines():
            match = warning_pattern.search(line)
            if match:
                warning_code = match.group(1)
                warning_msg = match.group(2).strip()
                warnings.append(f"{warning_code}: {warning_msg}")
        return warnings
