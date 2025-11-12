# Research & Technical Decisions

**Feature**: Documentation Validation and Synchronization
**Branch**: `010-docs-validation-sync`
**Date**: 2025-11-11

## Overview

This document captures technical research and decisions for the documentation validation feature. Minimal research required as spec has few unknowns - primary decisions involve library selection and integration approach.

## Research Questions

### 1. Markdown Parsing Library Selection

**Question**: Which Python markdown parser best suits extracting C# code blocks from documentation?

**Options Evaluated**:

1. **mistune** (v3.0+)
   - Pros: Fast, CommonMark compliant, active maintenance, simple API
   - Cons: Less mature plugin ecosystem vs markdown-it-py
   - Performance: ~2-3x faster than python-markdown

2. **python-markdown** (stdlib-adjacent)
   - Pros: Mature, extensive extensions, well-documented
   - Cons: Slower parsing, heavier dependencies
   - Performance: Baseline performance

3. **markdown-it-py**
   - Pros: Port of popular markdown-it.js, plugin ecosystem
   - Cons: Additional dependency overhead
   - Performance: Between mistune and python-markdown

**Decision**: **mistune v3.0+**

**Rationale**:
- Performance critical for <60s validation goal (SC-008 from spec)
- Simple code block extraction doesn't require advanced extensions
- CommonMark compliance ensures consistent parsing
- Already familiar to Python developers (pip install mistune)
- Active maintenance (last release 2024)

**Alternatives Rejected**:
- python-markdown: Too slow for our performance target
- markdown-it-py: Unnecessary complexity for simple code extraction

**Implementation Approach**:
```python
import mistune

# Extract code blocks with language specification
markdown = mistune.create_markdown(renderer='ast')
ast = markdown(doc_content)
csharp_blocks = [node for node in ast if node['type'] == 'code' and node['lang'] == 'csharp']
```

---

### 2. C# Code Compilation Validation

**Question**: How to validate C# code examples compile without creating full project files for each snippet?

**Options Evaluated**:

1. **Roslyn Compiler API** (Microsoft.CodeAnalysis.CSharp)
   - Pros: Programmatic compilation, detailed diagnostics, no external process
   - Cons: Complex API, requires .NET SDK, memory overhead
   - Performance: In-process compilation ~100-200ms per snippet

2. **dotnet CLI subprocess** (`dotnet build` with temp projects)
   - Pros: Simple, uses same compiler as real builds, reliable
   - Cons: Process overhead, temp file creation/cleanup
   - Performance: ~500ms-1s per snippet (process spawn + compilation)

3. **csc.exe direct invocation** (C# compiler)
   - Pros: Lighter than dotnet CLI, faster startup
   - Cons: Platform-specific, harder to configure references
   - Performance: ~300-500ms per snippet

**Decision**: **dotnet CLI with temp project approach**

**Rationale**:
- Reliability trumps raw performance (validation runs in CI, not tight loop)
- Same compiler guarantees as actual builds
- Easy reference management via .csproj
- Cross-platform (Linux/macOS/Windows)
- Existing S7Tools projects provide reference template
- Simplified cleanup with temp directories

**Performance Optimization**:
- Batch compile related examples in single project where possible
- Parallel compilation of independent examples
- Cache compilation results (checksum-based)

**Alternatives Rejected**:
- Roslyn API: Overkill complexity for simple compilation check
- csc.exe: Platform compatibility concerns

**Implementation Approach**:
```python
import subprocess
import tempfile
from pathlib import Path

def compile_csharp_example(code: str, references: list[str]) -> bool:
    with tempfile.TemporaryDirectory() as tmpdir:
        # Create minimal .csproj
        csproj = Path(tmpdir) / "Example.csproj"
        csproj.write_text(generate_csproj(references))

        # Write code to Program.cs
        (Path(tmpdir) / "Program.cs").write_text(code)

        # Compile
        result = subprocess.run(
            ["dotnet", "build", "--configuration", "Release"],
            cwd=tmpdir,
            capture_output=True,
            timeout=30
        )
        return result.returncode == 0
```

---

### 3. Integration with Existing Validation Infrastructure

**Question**: How to integrate new documentation validation with existing `scripts/validate-all.sh` and CI pipeline?

**Options Evaluated**:

1. **Standalone script with separate invocation**
   - Pros: Independent execution, easy to test
   - Cons: Requires manual CI config updates

2. **Integration into validate-all.sh**
   - Pros: Single entry point, automatic CI inclusion
   - Cons: Tighter coupling with existing validation

3. **GitHub Actions workflow**
   - Pros: Dedicated workflow, parallel execution
   - Cons: More CI config files to maintain

**Decision**: **Option 2 - Integration into validate-all.sh**

**Rationale**:
- Consistent with existing validation patterns (frontmatter, links, archive)
- Single command for complete validation (`./scripts/validate-all.sh`)
- Automatic CI inclusion (validate-all.sh already in CI)
- Developers familiar with existing workflow

**Implementation Approach**:
```bash
# Add to scripts/validate-all.sh
echo "Running documentation validation..."
python3 scripts/validate-documentation.py || exit_code=1
```

**Output Format**:
- Machine-readable JSON to `docs/.metadata/validation-results.json` (for CI parsing)
- Human-readable Markdown to `docs/.metadata/validation-report.md` (for developers)
- Console output with color-coded errors/warnings

**CI Integration**:
- Existing GitHub Actions workflow calls `validate-all.sh`
- Parse JSON output for fail-fast behavior
- Annotate PR with validation errors

**Alternatives Rejected**:
- Standalone script: Breaks existing validation workflow consistency
- Separate GitHub Actions: Unnecessary workflow proliferation

---

## Dependencies Summary

**Production Dependencies**:
```
mistune>=3.0.0        # Markdown parsing
```

**Development Dependencies**:
```
pytest>=7.0.0         # Testing framework
pytest-cov>=4.0.0     # Coverage reporting
```

**System Requirements**:
- Python 3.10+ (aligns with existing scripts)
- .NET 8 SDK (for dotnet CLI compilation)
- Git (for repository operations)

**No Additional Infrastructure Required**:
- Uses existing file system for documentation/source
- Output to existing docs/.metadata/ directory
- Integrates with existing CI pipeline

---

## Risk Assessment

### Low Risk
- ✅ Markdown parsing (well-established libraries)
- ✅ File path validation (stdlib pathlib)
- ✅ Namespace pattern matching (regex)

### Medium Risk
- ⚠️ Code compilation timeouts - **Mitigation**: 30s timeout per example, parallel processing
- ⚠️ Simplified examples missing imports - **Mitigation**: Template headers with common usings (see spec edge case EC-002)

### Negligible Risk
- External dependencies (mistune only)
- CI integration (extends existing validate-all.sh)

---

## Implementation Phases (Preview)

**Phase 1** (Foundation):
- Markdown parser wrapper
- Code block extractor
- Basic file path validator

**Phase 2** (Core Validation):
- C# compilation validator
- Namespace convention validator
- Pattern implementation checker

**Phase 3** (Integration):
- Link validator
- EditorConfig vs code-style comparator
- Report generators (JSON + Markdown)

**Phase 4** (Polish):
- Performance optimization (caching, parallelization)
- CI integration
- Documentation and quickstart guide

---

## Open Questions

**None** - All technical decisions resolved. Ready for Phase 1 (Design & Contracts).

---

**Next Steps**:
1. Create data-model.md with validation entities
2. Generate contracts/validation-report-schema.json
3. Create quickstart.md for running validation
4. Update agent context via `.specify/scripts/bash/update-agent-context.sh copilot`
