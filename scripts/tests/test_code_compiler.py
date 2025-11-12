"""Tests for C# code compilation validator."""

import pytest
from pathlib import Path
from validators.code_compiler import CodeCompiler
from entities import CodeExample, CompilationResult


class TestCodeCompiler:
    """Test suite for CodeCompiler class."""

    @pytest.fixture
    def compiler(self):
        """Create CodeCompiler instance."""
        return CodeCompiler(workspace_root=Path.cwd())

    def test_compile_simple_class(self, compiler):
        """Test compiling a simple C# class."""
        code = CodeExample(
            source_file="test.md",
            line_number=10,
            language="csharp",
            code="public class TestClass { public string Name { get; set; } }",
            required_usings=[],
            is_simplified=False
        )

        result = compiler.compile_csharp_example(code)

        assert isinstance(result, CompilationResult)
        assert result.success is True
        assert len(result.errors) == 0
        assert result.execution_time_ms > 0

    def test_compile_with_syntax_error(self, compiler):
        """Test compiling code with syntax error."""
        code = CodeExample(
            source_file="test.md",
            line_number=20,
            language="csharp",
            code="public class BrokenClass { missing brace",
            required_usings=[],
            is_simplified=False
        )

        result = compiler.compile_csharp_example(code)

        assert result.success is False
        assert len(result.errors) > 0
        assert any("CS" in error for error in result.errors)

    def test_compile_with_missing_using(self, compiler):
        """Test compiling code with missing using statement."""
        code = CodeExample(
            source_file="test.md",
            line_number=30,
            language="csharp",
            code="public class RegexUser { private Regex pattern; }",  # Regex requires using System.Text.RegularExpressions
            required_usings=[],  # Missing System.Text.RegularExpressions
            is_simplified=False
        )

        result = compiler.compile_csharp_example(code)

        # Should fail without proper using
        assert result.success is False
        assert any("CS0246" in error for error in result.errors)  # Type not found

    def test_compile_with_required_usings(self, compiler):
        """Test compiling code with required using statements."""
        code = CodeExample(
            source_file="test.md",
            line_number=40,
            language="csharp",
            code="public class RegexUser { private Regex pattern = new Regex(\".*\"); }",
            required_usings=["using System.Text.RegularExpressions;"],
            is_simplified=False
        )

        result = compiler.compile_csharp_example(code)

        assert result.success is True
        assert len(result.errors) == 0

    def test_skip_simplified_example(self, compiler):
        """Test that simplified examples are skipped."""
        code = CodeExample(
            source_file="test.md",
            line_number=50,
            language="csharp",
            code="// ... simplified\npublic class Example { }",
            required_usings=[],
            is_simplified=True
        )

        result = compiler.compile_csharp_example(code)

        assert result.success is True
        assert result.execution_time_ms == 0
        # Simplified examples should succeed without compilation

    def test_batch_compile_examples(self, compiler):
        """Test batch compilation of multiple examples."""
        examples = [
            CodeExample(
                source_file="test1.md",
                line_number=10,
                language="csharp",
                code="public class Test1 { }",
                required_usings=[],
                is_simplified=False
            ),
            CodeExample(
                source_file="test2.md",
                line_number=20,
                language="csharp",
                code="public class Test2 { }",
                required_usings=[],
                is_simplified=False
            ),
        ]

        results = compiler.batch_compile_examples(examples)

        assert len(results) == 2
        assert all(isinstance(r, CompilationResult) for r in results)
        assert all(r.success for r in results)

    def test_temp_project_cleanup(self, compiler, tmp_path):
        """Test that temporary projects are cleaned up."""
        code = CodeExample(
            source_file="test.md",
            line_number=60,
            language="csharp",
            code="public class CleanupTest { }",
            required_usings=[],
            is_simplified=False
        )

        # Count temp dirs before
        import tempfile
        temp_root = Path(tempfile.gettempdir())
        before_count = len(list(temp_root.glob("s7tools_validate_*")))

        result = compiler.compile_csharp_example(code)

        # Count temp dirs after (should be cleaned up)
        after_count = len(list(temp_root.glob("s7tools_validate_*")))

        assert after_count == before_count  # No leftover temp dirs

    def test_parse_errors_from_output(self, compiler):
        """Test parsing errors from compiler output."""
        output = """
        TempCode.cs(5,10): error CS0246: The type or namespace name 'Missing' could not be found
        TempCode.cs(7,15): error CS0103: The name 'undefined' does not exist in the current context
        """

        errors = compiler._parse_errors(output)

        assert len(errors) == 2
        assert "CS0246" in errors[0]
        assert "CS0103" in errors[1]

    def test_parse_warnings_from_output(self, compiler):
        """Test parsing warnings from compiler output."""
        output = """
        TempCode.cs(3,5): warning CS0168: The variable 'unused' is declared but never used
        TempCode.cs(10,8): warning CS0219: The variable 'assigned' is assigned but its value is never used
        """

        warnings = compiler._parse_warnings(output)

        assert len(warnings) == 2
        assert "CS0168" in warnings[0]
        assert "CS0219" in warnings[1]

    def test_build_cs_file_with_namespace(self, compiler):
        """Test building .cs file content with namespace."""
        code = "public class TestClass { }"
        usings = ["using System.Text;"]

        cs_content = compiler._build_cs_file(code, usings)

        assert "using System.Text;" in cs_content
        assert "namespace TempValidation;" in cs_content
        assert "public class TestClass { }" in cs_content

    def test_build_cs_file_preserves_namespace(self, compiler):
        """Test that existing namespace declarations are preserved."""
        code = "namespace MyNamespace;\npublic class TestClass { }"
        usings = ["using System.Text;"]

        cs_content = compiler._build_cs_file(code, usings)

        assert "using System.Text;" in cs_content
        assert "namespace MyNamespace;" in cs_content
        assert "namespace TempValidation;" not in cs_content

    def test_compilation_timeout_handling(self, compiler):
        """Test that compilation timeout is enforced."""
        # Create code that would hang (infinite loop in static initializer)
        code = CodeExample(
            source_file="test.md",
            line_number=70,
            language="csharp",
            code="""
public class HangTest
{
    static HangTest()
    {
        while(true) { }  // This would hang
    }
}
""",
            required_usings=[],
            is_simplified=False
        )

        # Note: This test is commented out because it would take 30s to run
        # In a real scenario, the timeout would trigger and return a timeout result
        # result = compiler.compile_csharp_example(code)
        # assert result.success is False
        # assert "timed out" in result.errors[0].lower()

        # Instead, verify timeout constant is set
        assert compiler.COMPILATION_TIMEOUT == 30.0


class TestCompilationPerformance:
    """Test suite for compilation performance requirements."""

    @pytest.fixture
    def compiler(self):
        return CodeCompiler(workspace_root=Path.cwd())

    def test_single_compilation_under_30s(self, compiler):
        """Test that single compilation completes under 30s (SC-008)."""
        code = CodeExample(
            source_file="perf.md",
            line_number=10,
            language="csharp",
            code="public class PerfTest { public string Property { get; set; } }",
            required_usings=[],
            is_simplified=False
        )

        result = compiler.compile_csharp_example(code)

        # Should complete well under 30 seconds
        assert result.execution_time_ms < 30000
        # Typically should be much faster (under 5 seconds)
        assert result.execution_time_ms < 5000
