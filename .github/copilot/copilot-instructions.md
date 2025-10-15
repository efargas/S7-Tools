# GitHub Copilot Instructions

## Priority Guidelines

When generating code for this repository:

1. **Version Compatibility**: Always detect and respect the exact versions of languages, frameworks, and libraries used in this project.
2. **Context Files**: Prioritize patterns and standards defined in `.copilot-tracking/memory-bank/systemPatterns.md`, `.editorconfig`, and `Project_Folders_Structure_Blueprint.md`.
3. **Codebase Patterns**: When context files don't provide specific guidance, scan the codebase for established patterns.
4. **Architectural Consistency**: Maintain our Clean Architecture with MVVM (Avalonia + ReactiveUI) and established boundaries.
5. **Code Quality**: Prioritize maintainability, performance, security, accessibility, and testability in all generated code.

## Technology Version Detection

Before generating code, scan the codebase to identify:

1. **Language Versions**:
   - .NET 8.0 (`<TargetFramework>net8.0</TargetFramework>` in `Directory.Build.props`)
   - C# 11+ features only if present in the codebase
   - Avalonia UI 11.3.6, ReactiveUI (see `Project_Folders_Structure_Blueprint.md`)
   - Never use language features beyond .NET 8.0

2. **Framework Versions**:
   - Check `Directory.Build.props`, `.csproj` files for exact versions
   - Respect version constraints when generating code
   - Never suggest features not available in the detected framework versions

3. **Library Versions**:
   - Microsoft.Extensions.Logging, Microsoft.Extensions.DependencyInjection, Moq, xUnit, FluentAssertions
   - Generate code compatible with these specific versions
   - Never use APIs or features not available in the detected versions

## Context Files

Prioritize the following files in order:

1. **systemPatterns.md**: System architecture guidelines, critical lessons learned, patterns, and rules
2. **.editorconfig**: Code style and formatting standards
3. **Project_Folders_Structure_Blueprint.md**: Project organization guidelines
4. **AGENTS.md**: Agent onboarding and best practices

## Codebase Scanning Instructions

When context files don't provide specific guidance:

1. Identify similar files to the one being modified or created
2. Analyze patterns for:
   - Naming conventions (PascalCase for types, camelCase for fields with `_` prefix)
   - Code organization (Clean Architecture layers, feature-based organization)
   - Error handling (never swallow exceptions, always log and rethrow or return failure)
   - Logging approaches (use `ILogger<T>`, structured logging)
   - Documentation style (XML comments for all public APIs)
   - Testing patterns (AAA pattern, xUnit, Moq, FluentAssertions)
3. Follow the most consistent patterns found in the codebase
4. When conflicting patterns exist, prioritize patterns in newer files or files with higher test coverage
5. Never introduce patterns not found in the existing codebase

## Code Quality Standards

### Maintainability
- Write self-documenting code with clear naming
- Follow the naming and organization conventions evident in the codebase
- Follow established patterns for consistency
- Keep functions focused on single responsibilities
- Limit function complexity and length to match existing patterns
- Use PascalCase for public members, camelCase with `_` prefix for private fields
- Place interfaces in S7Tools.Core project, implementations in S7Tools or Infrastructure

### Performance
- Follow existing patterns for memory and resource management
- Use `.ConfigureAwait(false)` in all non-UI library code to avoid deadlocks
- Avoid inefficient bulk UI notifications; use batch operations (e.g., `AddRange`)
- Follow established patterns for asynchronous operations
- Apply caching consistently with existing patterns
- Optimize according to patterns evident in the codebase

### Security
- Follow existing patterns for input validation
- Apply the same sanitization techniques used in the codebase
- Use parameterized queries matching existing patterns
- Follow established authentication and authorization patterns
- Handle sensitive data according to existing patterns

### Accessibility
- Follow existing accessibility patterns in the codebase (if applicable)
- Match ARIA attribute usage with existing components
- Maintain keyboard navigation support consistent with existing code

### Testability
- Follow established patterns for testable code
- Use dependency injection as demonstrated in `ServiceCollectionExtensions.cs`
- Apply the same patterns for managing dependencies
- Follow established mocking and test double patterns (Moq)
- Match the testing style used in existing tests (xUnit, AAA pattern)

## Documentation Requirements

- Follow the exact XML documentation format found in the codebase
- Document all public APIs with XML comments
- Match the style and completeness of existing comments
- Document parameters, returns, and exceptions in the same style
- Follow existing patterns for usage examples
- Match class-level documentation style and content

## Testing Approach

### Unit Testing
- Match the exact structure and style of existing unit tests
- Use xUnit as the testing framework
- Use Moq for mocking dependencies
- Use FluentAssertions for assertions
- Follow Arrange–Act–Assert (AAA) pattern
- Follow the same naming conventions for test classes and methods
- Apply the same mocking approach used in the codebase
- Follow existing patterns for test isolation
- Place tests in appropriate test projects:
  - `S7Tools.Tests` for UI and ViewModel tests
  - `S7Tools.Core.Tests` for domain model and service interface tests
  - `S7Tools.Infrastructure.Logging.Tests` for logging infrastructure tests

### Integration Testing
- Follow the same integration test patterns found in the codebase
- Match existing patterns for test data setup and teardown
- Use the same approach for testing component interactions
- Follow existing patterns for verifying system behavior

## .NET Guidelines

- Detect and strictly adhere to .NET 8.0
- Use only C# language features compatible with the detected version
- Follow LINQ usage patterns exactly as they appear in the codebase
- Match async/await usage patterns from existing code
- Apply the same dependency injection approach used in the codebase (Microsoft.Extensions.DependencyInjection)
- Use the same collection types and patterns found in existing code
- Never register services directly in `Program.cs`; always use `ServiceCollectionExtensions.cs`

### Avalonia UI & ReactiveUI Guidelines
- All ViewModels must inherit from `ReactiveObject` or `ViewModelBase`
- Use `RaiseAndSetIfChanged` for property setters
- Use `ReactiveCommand` for all commands
- Limit `WhenAnyValue` to maximum 12 properties per call
- Prefer individual subscriptions with `Skip(1)` to avoid initial triggers
- Always dispose subscriptions with `DisposeWith(_disposables)`
- Use `IUIThreadService` for all UI thread marshaling
- Never block the UI thread with I/O operations

### Logging Guidelines
- Use `ILogger<T>` for all logging
- Use structured logging, never string interpolation in log messages
- Integrate with the custom DataStore provider for real-time UI logs
- Follow existing log level conventions

### Error Handling Guidelines
- Never swallow exceptions without logging
- Always log and rethrow or return a failure result
- Follow the Result pattern as established in the codebase
- Use appropriate log levels (Error for exceptions, Warning for recoverable issues)

### Threading & Synchronization Guidelines
- Follow the Internal Method Pattern for semaphore usage:
  - Public APIs acquire/release semaphore
  - Internal helpers assume semaphore is already held
- Add debug logging around semaphore acquisition/release
- Use `ConfigureAwait(false)` in library code
- Use `IUIThreadService` for UI thread operations

### Dependency Injection Guidelines
- Register all services in `ServiceCollectionExtensions.cs`
- Use `TryAddSingleton`, `TryAddTransient`, or `TryAddScoped` appropriately
- Follow existing registration patterns
- Never register services directly in `Program.cs`

## Version Control Guidelines

- Follow Semantic Versioning patterns as applied in the codebase
- Match existing patterns for documenting breaking changes
- Follow the same approach for deprecation notices

## General Best Practices

- Follow naming conventions exactly as they appear in existing code
- Match code organization patterns from similar files
- Apply error handling consistent with existing patterns
- Follow the same approach to testing as seen in the codebase
- Match logging patterns from existing code
- Use the same approach to configuration as seen in the codebase
- Run `dotnet format` before committing
- Enforce code style with `.editorconfig`

## Project-Specific Guidance

- Scan the codebase thoroughly before generating any code
- Respect existing architectural boundaries without exception:
  - **S7Tools.Core**: Domain models and interfaces (no external dependencies)
  - **S7Tools.Infrastructure.Logging**: Logging infrastructure only depends on Core + Microsoft.Extensions.Logging
  - **S7Tools**: Main UI, ViewModels, Application services
- Match the style and patterns of surrounding code
- When in doubt, prioritize consistency with existing code over external best practices
- Always check `.copilot-tracking/memory-bank/systemPatterns.md` for critical lessons learned
- Follow the External Code Review Response Protocol when addressing review feedback

## Critical Lessons Learned (MUST READ)

### Semaphore Deadlock Pattern
- **Problem**: Method A acquires semaphore, calls Method B which tries to acquire same semaphore = DEADLOCK
- **Solution**: Create "Internal" versions of methods for use within locked contexts
- **Debug Pattern**: Add emoji logging around semaphore operations (🔒 🔓)

### ReactiveUI Constraints
- **Problem**: Large `WhenAnyValue` tuples are costly and hard to maintain
- **Solution**: Max 12 properties per call; prefer individual subscriptions with `Skip(1)`

### ConfigureAwait Best Practice
- **Problem**: Deadlocks in library code when capturing synchronization context
- **Solution**: Use `.ConfigureAwait(false)` in all non-UI code

### Dispose Pattern
- **Problem**: Resource leaks and memory issues
- **Solution**: Follow established dispose patterns, use `DisposeWith` for ReactiveUI subscriptions

For complete details on all patterns, critical fixes, and templates, always consult `.copilot-tracking/memory-bank/systemPatterns.md`.
