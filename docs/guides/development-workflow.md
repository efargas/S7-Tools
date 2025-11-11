---
title: "Development Workflow Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "workflow", "development", "commands"]
related:
  - docs/guides/onboarding.md
  - docs/guides/testing-guide.md
  - docs/guides/code-style.md
  - docs/architecture/overview.md
---

# Development Workflow Guide

This guide covers the day-to-day development workflow for S7Tools, including essential commands, branching strategy, and quality gates.

## Daily Commands (MANDATORY)

**CRITICAL REQUIREMENT**: All developers and AI agents MUST use terminal commands for .NET operations. VS Code tasks are FORBIDDEN for build, test, format, and run operations.

### Morning Routine

```bash
# Navigate to project root
cd /path/to/S7-Tools

# Update from remote
git pull origin main

# Restore dependencies (if package changes)
dotnet restore src/S7Tools.sln
```

### Development Cycle

```bash
# Clean build
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Run tests (maintain 99.7%+ pass rate)
dotnet test src/S7Tools.sln --configuration Debug

# Format code (REQUIRED before commit)
dotnet format src/S7Tools.sln

# Run application
dotnet run --project src/S7Tools --configuration Debug

# Run with diagnostics
dotnet run --project src/S7Tools --configuration Debug -- --diag
```

### Why Terminal Commands Only?

1. **Consistency**: Works across all environments (Linux, macOS, Windows)
2. **Reproducibility**: Same commands in documentation and CI/CD
3. **No Dependencies**: Avoids VS Code task configuration issues
4. **Constitutional Compliance**: Required by project constitution v1.1.0
5. **Explicit Control**: Clear visibility of build parameters

## Branching Strategy

### Branch Types

| Type | Pattern | Purpose | Example |
|------|---------|---------|---------|
| **Main** | `main` | Production-ready code | `main` |
| **Feature** | `feature/XXX-description` | New features from specs | `feature/009-docs-consolidation` |
| **Bugfix** | `bugfix/issue-number` | Bug fixes | `bugfix/123` |
| **Hotfix** | `hotfix/critical-issue` | Urgent production fixes | `hotfix/security-patch` |

### Creating a Feature Branch

```bash
# Start from latest main
git checkout main
git pull origin main

# Create feature branch (use spec number if available)
git checkout -b feature/010-new-feature

# Or for bugs
git checkout -b bugfix/123
```

### Working on a Branch

```bash
# Make changes
# ... edit files ...

# Format code
dotnet format src/S7Tools.sln

# Run tests
dotnet test src/S7Tools.sln

# Stage and commit
git add .
git commit -m "feat(module): Add new functionality

- Implement X
- Add tests for Y
- Update documentation

Refs: #123"

# Push to remote
git push origin feature/010-new-feature
```

## Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, no logic change)
- `refactor`: Code change that neither fixes bug nor adds feature
- `perf`: Performance improvement
- `test`: Adding or updating tests
- `chore`: Build process, dependencies, tooling

### Examples

```bash
# Feature
git commit -m "feat(profiles): Add duplicate profile detection

- Implement MD5 hash comparison
- Add duplicate warning in UI
- Include 5 unit tests

Refs: #123"

# Bug fix
git commit -m "fix(scheduler): Prevent null reference in task promotion

- Add null check before queue promotion
- Add unit test for edge case

Fixes: #456"

# Documentation
git commit -m "docs(patterns): Update profile management pattern

- Add section on concurrent access
- Include new example
- Fix broken cross-reference"
```

## Quality Gates (Pre-Commit)

### Mandatory Checks

```bash
# 1. Format code
dotnet format src/S7Tools.sln

# 2. Build without errors/warnings
dotnet build src/S7Tools.sln --configuration Debug
# Expected: 0 errors, 0 warnings

# 3. Run all tests
dotnet test src/S7Tools.sln --configuration Debug
# Expected: 360/361 passing (99.7%+)

# 4. Validate documentation (if docs changed)
python scripts/validate-frontmatter.py docs/
find docs -name "*.md" -exec markdown-link-check {} \;
```

### Optional Checks (Recommended)

```bash
# Check for code smells
dotnet build src/S7Tools.sln --configuration Release -v detailed

# Run benchmarks (if performance-critical changes)
dotnet run --project benchmarks/S7Tools.Benchmarks/S7Tools.Benchmarks.csproj --configuration Release
```

## Pull Request Workflow

### Before Creating PR

1. **Ensure branch is up to date**
   ```bash
   git checkout main
   git pull origin main
   git checkout feature/your-branch
   git rebase main
   ```

2. **Run all quality gates** (see above)

3. **Update documentation** if needed
   - Architecture changes → Update relevant docs in `docs/architecture/`
   - New patterns → Add to `docs/patterns/`
   - Breaking changes → Update migration guides

### Creating PR

1. **Push branch**
   ```bash
   git push origin feature/your-branch
   ```

2. **Open PR on GitHub**
   - Use descriptive title
   - Reference spec/issue number
   - Fill out PR template
   - Request reviewers

3. **PR Description Template**
   ```markdown
   ## Description
   Brief description of changes

   ## Related Issues
   Closes #123
   Refs: specs/009-docs-consolidation

   ## Changes Made
   - Added X
   - Modified Y
   - Removed Z

   ## Testing
   - [ ] Unit tests passing (360/361)
   - [ ] Manual testing completed
   - [ ] Documentation updated

   ## Checklist
   - [ ] Code formatted (`dotnet format`)
   - [ ] No build warnings
   - [ ] Tests passing
   - [ ] Documentation updated
   ```

### After PR Approval

```bash
# Squash or merge as appropriate
# Delete feature branch after merge
git checkout main
git pull origin main
git branch -d feature/your-branch
git push origin --delete feature/your-branch
```

## Common Workflows

### Workflow: Implement New Feature

1. **Read Specification**
   ```bash
   cat specs/XXX-feature-name/spec.md
   cat specs/XXX-feature-name/plan.md
   cat specs/XXX-feature-name/tasks.md
   ```

2. **Check Architecture Guidance**
   - Read `docs/architecture/clean-architecture.md`
   - Check relevant patterns in `docs/patterns/`
   - Review similar existing features

3. **Create Branch**
   ```bash
   git checkout -b feature/XXX-feature-name
   ```

4. **Implement TDD**
   - Write failing test
   - Implement feature
   - Verify test passes
   - Refactor if needed

5. **Quality Gates**
   ```bash
   dotnet format src/S7Tools.sln
   dotnet test src/S7Tools.sln
   ```

6. **Commit and Push**
   ```bash
   git add .
   git commit -m "feat(module): description"
   git push origin feature/XXX-feature-name
   ```

### Workflow: Fix Bug

1. **Reproduce Bug**
   - Write failing test that demonstrates bug

2. **Identify Root Cause**
   - Review relevant pattern documentation
   - Check code review history

3. **Fix**
   - Implement fix
   - Verify test now passes

4. **Validate**
   ```bash
   dotnet test src/S7Tools.sln
   ```

5. **Commit**
   ```bash
   git commit -m "fix(module): description of fix

Fixes: #issue-number"
   ```

### Workflow: Refactor Code

1. **Ensure Test Coverage**
   - Verify area has good test coverage
   - Add tests if missing

2. **Baseline**
   ```bash
   dotnet test src/S7Tools.sln
   # Note passing test count
   ```

3. **Refactor Incrementally**
   - Make small changes
   - Run tests after each change
   - Commit frequently

4. **Final Validation**
   ```bash
   dotnet test src/S7Tools.sln
   # Verify same tests passing
   ```

## Troubleshooting

### Build Errors

```bash
# Clean and rebuild
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug -v detailed
```

### Test Failures

```bash
# Run specific test
dotnet test src/S7Tools.sln --filter "FullyQualifiedName~TestName"

# Run tests with detailed output
dotnet test src/S7Tools.sln -v detailed
```

### Git Conflicts

```bash
# Update from main
git checkout main
git pull origin main
git checkout your-branch
git rebase main

# Resolve conflicts manually
# ... edit files ...

git add .
git rebase --continue
```

## Environment Setup

### Required Tools

- .NET SDK 8.0+
- Git
- Python 3.11+ (for documentation scripts)
- Node.js 18+ (for markdown validation)

### Verification

```bash
# Check versions
dotnet --version  # Should be 8.0+
git --version
python --version  # Should be 3.11+
node --version    # Should be 18+

# Verify build
dotnet build src/S7Tools.sln --configuration Debug
```

## Best Practices

### DO

✅ Use terminal commands exclusively
✅ Format code before every commit
✅ Run all tests before pushing
✅ Update documentation with code changes
✅ Write descriptive commit messages
✅ Keep branches short-lived (< 1 week)
✅ Rebase frequently to stay current

### DON'T

❌ Use VS Code tasks for .NET operations
❌ Commit without running tests
❌ Push without formatting
❌ Merge without PR review
❌ Leave long-lived feature branches
❌ Commit broken code
❌ Skip documentation updates

## References

- [Onboarding Guide](./onboarding.md)
- [Testing Guide](./testing-guide.md)
- [Code Style Guide](./code-style.md)
- [Architecture Overview](../architecture/overview.md)
- [Pattern Catalog](../patterns/_index.md)

---

*Last Updated*: 2025-11-10

## Related Documentation

- [Index](../INDEX.md)
- [Settings_Schema](../SETTINGS_SCHEMA.md)
- [Overview](../architecture/overview.md)
- [Ai Agent Guide](ai-agent-guide.md)
- [Code Style](code-style.md)
- [Memory Bank Usage](memory-bank-usage.md)
- [_Index](migration/_index.md)
- [Onboarding](onboarding.md)
- [Testing Guide](testing-guide.md)
- [Versioning Guide](versioning-guide.md)
- [_Index](../patterns/_index.md)
- [Profile Manager Example](../patterns/examples/profile-manager-example.md)
- [Resource Coordinator Example](../patterns/examples/resource-coordinator-example.md)
- [Semaphore Pattern Example](../patterns/examples/semaphore-pattern-example.md)
- [_Index](../templates/_index.md)
- [Adr Template](../templates/adr-template.md)
- [Service Template](../templates/service-template.md)
- [Viewmodel Template](../templates/viewmodel-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
