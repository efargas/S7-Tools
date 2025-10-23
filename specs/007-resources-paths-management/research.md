# Research: Resources and Settings Paths Management

**Feature**: 007-resources-paths-management
**Date**: 2025-10-22
**Status**: Complete

## Research Tasks Completed

### 1. Dynamic Path Resolution Patterns in .NET Applications

**Decision**: Use `Assembly.GetEntryAssembly()?.Location` with `Path.GetDirectoryName()` for executable-relative paths

**Rationale**:
- Provides reliable cross-platform path resolution
- Works correctly with published applications
- Handles both development and deployment scenarios
- Avoids hardcoded paths completely

**Alternatives considered**:
- `Environment.CurrentDirectory` - can change during execution
- `AppContext.BaseDirectory` - may not always point to executable location
- Hardcoded relative paths - inflexible and error-prone

### 2. Configuration Management Best Practices for Desktop Applications

**Decision**: Implement layered configuration with user settings override pattern

**Rationale**:
- Follows Microsoft.Extensions.Configuration patterns
- Allows user customization without modifying defaults
- Supports hierarchical configuration sources
- Integrates well with existing Options pattern

**Alternatives considered**:
- Single configuration file - lacks flexibility for user overrides
- Registry-based settings - platform-specific and complex
- Database storage - overkill for desktop application settings

### 3. Error Handling Strategies for File Operations

**Decision**: Use try-catch with specific exception handling and structured logging

**Rationale**:
- Provides detailed error information for debugging
- Allows graceful degradation when possible
- Follows existing logging patterns in S7Tools
- Enables recovery actions (e.g., creating missing directories)

**Alternatives considered**:
- Silent failure handling - provides poor debugging experience
- Generic exception handling - loses important error context
- Throwing all exceptions - would crash application on permission issues

### 4. Settings Persistence Patterns

**Decision**: Use JSON files with Microsoft.Extensions.Configuration.Json

**Rationale**:
- Human-readable format for troubleshooting
- Built-in .NET support with change detection
- Supports complex object hierarchies
- Consistent with existing profile storage approach

**Alternatives considered**:
- XML configuration - more verbose, complex parsing
- Binary formats - not human-readable, harder to debug
- INI files - limited structure support

### 5. Cross-Platform Path Handling

**Decision**: Use `Path.Combine()` and `Path.DirectorySeparatorChar` for all path operations

**Rationale**:
- Ensures cross-platform compatibility
- Handles platform-specific path separators automatically
- Reduces risk of path-related bugs
- Standard .NET approach

**Alternatives considered**:
- String concatenation with hardcoded separators - platform-specific issues
- Forward slash assumption - fails on Windows in some cases
- Platform detection with conditional logic - unnecessary complexity

### 6. Directory Creation and Initialization

**Decision**: Create directories on-demand with proper permission handling

**Rationale**:
- Ensures required folders exist before use
- Handles first-run scenarios gracefully
- Logs creation attempts for debugging
- Fails gracefully in read-only environments

**Alternatives considered**:
- Pre-creation during installation - not always possible
- Assume directories exist - leads to runtime failures
- Create all directories at startup - may be unnecessary overhead

## Implementation Guidelines

Based on research findings:

1. **Path Resolution Service**: Create `IPathService` interface with implementation in Infrastructure
2. **Configuration Layering**: Default settings bundled with application, user settings in user-writable location
3. **Error Recovery**: Attempt directory creation, log failures, provide fallback behaviors
4. **Testing Strategy**: Unit tests with mock file system, integration tests with temp directories
5. **Performance**: Cache resolved paths, lazy initialization of directories
6. **Observability**: Structured logging for all path operations and configuration loading

## Next Steps

Proceed to Phase 1: Data model design and API contracts based on these research findings.
