---
title: "Developer Onboarding Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "onboarding", "developers", "getting-started"]
related:
  - docs/INDEX.md
  - docs/architecture/overview.md
  - docs/guides/development-workflow.md
  - docs/guides/ai-agent-guide.md
---

# Developer Onboarding Guide

Welcome to **S7Tools**! This guide will help you get up to speed quickly with the project structure, architecture, and development workflow.

## Quick Start (30 Minutes)

### 1. Setup Environment (10 minutes)

```bash
# Clone the repository
git clone https://github.com/efargas/S7-Tools.git
cd S7-Tools

# Verify prerequisites
dotnet --version  # Should be .NET 8.0+
git --version     # Any recent version

# Build the project
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Run tests (should see 360/361 tests passing, 1 intentionally skipped)
dotnet test src/S7Tools.sln --configuration Debug
```

### 2. Understand the Architecture (10 minutes)

Read these documents in order:

1. **[Project Overview](../architecture/overview.md)** (5 min)
   - Learn about Clean Architecture + MVVM + ReactiveUI
   - Understand layer boundaries and dependencies
   - Review technology stack

2. **[Clean Architecture Guide](../architecture/clean-architecture.md)** (5 min)
   - Dependency flow rules
   - Layer responsibilities
   - Common anti-patterns to avoid

### 3. Explore Key Patterns (10 minutes)

Browse the [Pattern Catalog](../patterns/_index.md) and focus on:

- **[Profile Management](../patterns/profile-management.md)** - Core pattern for configuration
- **[Internal Method Pattern](../patterns/internal-method.md)** - Thread safety best practice
- **[Resource Coordination](../patterns/resource-coordination.md)** - Startup optimization

## Project Structure

```
S7-Tools/
├── src/
│   ├── S7Tools/                    # Main UI application
│   │   ├── ViewModels/             # Categorized ViewModels
│   │   ├── Views/                  # Categorized Views
│   │   ├── Services/               # Application services
│   │   └── Extensions/             # DI registration
│   ├── S7Tools.Core/               # Domain layer (no external deps)
│   │   ├── Models/                 # Domain entities
│   │   ├── Services/Interfaces/    # Service contracts
│   │   └── Exceptions/             # Custom exceptions
│   └── S7Tools.Infrastructure.*/   # Infrastructure layers
├── tests/                          # Test projects
├── docs/                           # Consolidated documentation
│   ├── INDEX.md                    # Documentation master index
│   ├── architecture/               # Architecture docs & ADRs
│   ├── patterns/                   # Implementation patterns
│   ├── guides/                     # Development guides
│   └── templates/                  # Code templates
├── specs/                          # Feature specifications
└── README.md                       # Project overview
```

## Development Workflow

### Daily Commands (MANDATORY)

**CRITICAL**: Always use terminal commands, never VS Code tasks.

```bash
# Morning routine
git pull
dotnet restore src/S7Tools.sln

# During development
dotnet build src/S7Tools.sln --configuration Debug
dotnet test src/S7Tools.sln --configuration Debug

# Before commit (REQUIRED)
dotnet format src/S7Tools.sln

# Run application
dotnet run --project src/S7Tools --configuration Debug

# With diagnostics
dotnet run --project src/S7Tools --configuration Debug -- --diag
```

### Making Changes

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Check Existing Patterns**
   - Review [Pattern Catalog](../patterns/_index.md)
   - Look for similar implementations
   - Use [Templates](../templates/) as starting point

3. **Implement Following Architecture**
   - Core layer: Domain models and interfaces only
   - Application layer: ViewModels, Views, Services
   - Infrastructure layer: External concerns (logging, data)

4. **Write Tests First** (Test-Driven Development)
   - Follow AAA pattern (Arrange-Act-Assert)
   - Maintain 99%+ test pass rate
   - See [Testing Guide](./testing-guide.md)

5. **Format and Validate**
   ```bash
   dotnet format src/S7Tools.sln
   dotnet test src/S7Tools.sln
   ```

6. **Commit with Good Message**
   ```bash
   git add .
   git commit -m "feat(profiles): Add duplicate profile detection

   - Implement MD5 hash comparison
   - Add duplicate warning in UI
   - Include 5 unit tests

   Refs: #123"
   ```

## Key Conventions

### Namespaces

- **ViewModels**: `S7Tools.ViewModels.{Category}`
  - Categories: Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks
- **Views**: `S7Tools.Views.{Category}` (mirrors ViewModels)
- **Core Models**: `S7Tools.Core.Models.{Domain}`
- **Services**: `S7Tools.Services.{Domain}`

### ViewLocator Pattern

The ViewLocator automatically maps ViewModels to Views:

```csharp
// ViewModel: S7Tools.ViewModels.Pages.HomeViewModel
// View:      S7Tools.Views.Pages.HomeView
// ✅ Auto-resolved, no manual wiring needed
```

### Service Registration

**ALWAYS** register services in `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddS7ToolsProfileServices(this IServiceCollection services)
{
    services.TryAddSingleton<ISerialProfileService, SerialProfileService>();
    return services;
}
```

**NEVER** register services directly in `Program.cs`.

### MVVM with ReactiveUI

All ViewModels must:

```csharp
public class MyViewModel : ReactiveObject
{
    private string _myProperty = string.Empty;
    public string MyProperty
    {
        get => _myProperty;
        set => this.RaiseAndSetIfChanged(ref _myProperty, value);
    }

    public MyViewModel()
    {
        // Use reactive commands
        var canExecute = this.WhenAnyValue(x => x.IsValid);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canExecute);
    }
}
```

### Thread Safety

**NEVER** block the UI thread:

```csharp
// ❌ BAD
var result = await Task.Run(() => service.LoadData()).Result;

// ✅ GOOD
var result = await service.LoadDataAsync().ConfigureAwait(false);
await _uiThreadService.InvokeAsync(() => {
    // UI updates here
});
```

See [Internal Method Pattern](../patterns/internal-method.md) for semaphore safety.

## Common Tasks

### Task: Add a New Feature

1. Read feature specification in `specs/XXX-feature-name/spec.md`
2. Check [Architecture Guide](../architecture/clean-architecture.md) for layer placement
3. Find similar features for reference
4. Use [ViewModel Template](../templates/viewmodel-template.cs) as starting point
5. Write tests using [Test Template](../templates/test-template.cs)
6. Implement following established patterns
7. Run `dotnet format` and `dotnet test`
8. Commit with descriptive message

### Task: Fix a Bug

1. Write a failing test that reproduces the bug
2. Read relevant [Pattern Documentation](../patterns/_index.md)
3. Check [Latest Review](../reviews/LATEST.md) for known issues
4. Fix the bug
5. Verify test now passes
6. Run full test suite
7. Update pattern docs if bug revealed misunderstanding

### Task: Refactor Code

1. Ensure test coverage exists for the area
2. Run tests to establish baseline
3. Make incremental changes
4. Run tests after each change
5. Use `git commit` frequently to checkpoint progress
6. Final validation with full test suite

## Quality Standards

**Build**: 0 errors, 0 warnings
**Tests**: 360/361 passing (99.7% pass rate)
**Code Quality**: A+ grade (98/100)

### Before Every Commit

```bash
# Format code
dotnet format src/S7Tools.sln

# Run tests
dotnet test src/S7Tools.sln --configuration Debug

# Check for errors
dotnet build src/S7Tools.sln --configuration Debug
```

### Code Review Checklist

- [ ] Follows Clean Architecture (dependencies flow inward)
- [ ] ViewModels use ReactiveUI patterns
- [ ] Services registered in ServiceCollectionExtensions
- [ ] Thread safety respected (no UI blocking)
- [ ] Tests written following AAA pattern
- [ ] Code formatted with `dotnet format`
- [ ] XML documentation on public APIs
- [ ] No hardcoded strings (use resources)
- [ ] Patterns documented if new

See [Latest Review](../reviews/LATEST.md) for current quality baseline.

## Documentation

### When to Update Documentation

**Always Update**:
- New architectural pattern introduced
- Existing pattern modified
- New coding standard adopted
- Breaking change in public API

**Sometimes Update**:
- Bug fix revealed pattern misunderstanding
- New example added for existing pattern

### How to Update

1. Find the right file in [`docs/`](../INDEX.md) structure
2. Add/update content
3. Update frontmatter (version, last-updated)
4. Run validation:
   ```bash
   python scripts/validate-frontmatter.py docs/
   find docs -name "*.md" -exec markdown-link-check {} \;
   ```
5. Commit documentation with code changes

## Getting Help

**Questions About**:
- Architecture → Read [Architecture Overview](../architecture/overview.md)
- Patterns → Browse [Pattern Catalog](../patterns/_index.md)
- Workflow → Read [Development Workflow](./development-workflow.md)
- Quality → Check [Latest Review](../reviews/LATEST.md)

**Stuck?**:
- Search existing code for similar implementations
- Check [Pattern Examples](../patterns/examples/)
- Review [Templates](../templates/)
- Ask in PR comments or issues

## Next Steps

**After Onboarding**:

1. **Pick a Small Task** - Start with a "good first issue" label
2. **Read Relevant Patterns** - Focus on patterns used in your task area
3. **Study Existing Code** - Find similar features to understand conventions
4. **Make Your First PR** - Follow the workflow above
5. **Request Code Review** - Learn from feedback

**Continuous Learning**:

- Review [Latest Code Reviews](../reviews/LATEST.md) periodically
- Read new [Pattern Documentation](../patterns/_index.md) as it's added
- Study [Architecture Decisions](../architecture/decisions/) to understand "why"
- Keep [Quick Reference](../INDEX.md) bookmarked

---

Welcome to the team! 🚀

## Related Documentation

- [Index](../INDEX.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [Overview](../architecture/overview.md)
- [Development Workflow](development-workflow.md)
- [Testing Guide](testing-guide.md)
- [_Index](../patterns/_index.md)
- [Internal Method](../patterns/internal-method.md)
- [Profile Management](../patterns/profile-management.md)
- [Resource Coordination](../patterns/resource-coordination.md)
- [2025 11 10 Quality Improvements](../reviews/2025-11-10-quality-improvements.md)
- [Latest](../reviews/LATEST.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
