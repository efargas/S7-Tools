# Phase 1 Implementation Instructions
## Unified S7Tools VSCode UI with Integrated LogViewer

**Phase**: 1 - Foundation & Infrastructure  
**Duration**: 5 days  
**Status**: Ready to Start  
**Dependencies**: None  
**Created**: January 27, 2025

## Overview

Phase 1 establishes the foundational infrastructure for the unified S7Tools implementation. This includes creating the logging infrastructure project and implementing core services that will support the VSCode-like UI and integrated LogViewer functionality.

## Prerequisites

Before starting Phase 1, ensure:
- [ ] You have reviewed the [unified implementation plan](../details/unified-s7tools-implementation-plan.md)
- [ ] You understand the [project structure](../details/unified-project-structure.md)
- [ ] You have access to the current S7Tools codebase
- [ ] You understand Avalonia UI, ReactiveUI, and Microsoft.Extensions.Logging patterns

## Phase 1 Goals

### Primary Objectives
1. **Create S7Tools.Infrastructure.Logging project** with complete logging infrastructure
2. **Implement foundation services** for UI management, theming, and localization
3. **Establish service contracts** that will be used throughout the application
4. **Set up dependency injection** for all new services
5. **Create resource management system** for localization support

### Success Criteria
- [ ] All logging infrastructure compiles and integrates with Microsoft.Extensions.Logging
- [ ] Foundation services are thread-safe and properly registered in DI
- [ ] Resource management system supports localization
- [ ] Theme service can switch between VSCode light/dark themes
- [ ] All code follows established patterns and has comprehensive documentation

## Implementation Steps

### Step 1.1: Create Logging Infrastructure Project

#### 1.1.1: Create Project Structure
Create the new `S7Tools.Infrastructure.Logging` project with the following structure:

```
src/S7Tools.Infrastructure.Logging/
├── Core/
│   ├── Models/
│   ├── Storage/
│   └── Configuration/
├── Providers/
│   ├── Microsoft/
│   └── Extensions/
└── S7Tools.Infrastructure.Logging.csproj
```

#### 1.1.2: Create Project File
**File**: `src/S7Tools.Infrastructure.Logging/S7Tools.Infrastructure.Logging.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="System.Collections.Concurrent" Version="8.0.0" />
  </ItemGroup>

</Project>
```

#### 1.1.3: Implement Core Models

**File**: `src/S7Tools.Infrastructure.Logging/Core/Models/LogModel.cs`

```csharp
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Represents a log entry with all necessary information for display and processing.
/// </summary>
public sealed class LogModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the log level (Information, Warning, Error, etc.).
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the category name (typically the logger name).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception information, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the event ID associated with this log entry.
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// Gets or sets the scope information for this log entry.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets additional properties associated with this log entry.
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets the formatted message including exception details if present.
    /// </summary>
    public string FormattedMessage => Exception != null 
        ? $"{Message}\n{Exception}" 
        : Message;

    /// <summary>
    /// Creates a copy of this log model.
    /// </summary>
    /// <returns>A new LogModel instance with the same values.</returns>
    public LogModel Clone()
    {
        return new LogModel
        {
            Id = Id,
            Timestamp = Timestamp,
            Level = Level,
            Category = Category,
            Message = Message,
            Exception = Exception,
            EventId = EventId,
            Scope = Scope,
            Properties = new Dictionary<string, object?>(Properties)
        };
    }
}
```

**File**: `src/S7Tools.Infrastructure.Logging/Core/Models/LogEntryColor.cs`

```csharp
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Defines color configuration for different log levels.
/// </summary>
public sealed class LogEntryColor
{
    /// <summary>
    /// Gets or sets the foreground color for the log entry.
    /// </summary>
    public string Foreground { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the background color for the log entry.
    /// </summary>
    public string Background { get; set; } = "Transparent";

    /// <summary>
    /// Gets or sets the border color for the log entry.
    /// </summary>
    public string Border { get; set; } = "Transparent";

    /// <summary>
    /// Gets the default color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> DefaultColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#808080", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#A0A0A0", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#FFFFFF", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#FFA500", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#FF4444", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#FF0000" },
        [LogLevel.None] = new() { Foreground = "#FFFFFF", Background = "Transparent" }
    };

    /// <summary>
    /// Gets the VSCode dark theme color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> VSCodeDarkColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#6A9955", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#569CD6", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#D4D4D4", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#DCDCAA", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#F44747", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#F44747" },
        [LogLevel.None] = new() { Foreground = "#D4D4D4", Background = "Transparent" }
    };

    /// <summary>
    /// Gets the VSCode light theme color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> VSCodeLightColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#008000", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#0000FF", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#000000", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#795E26", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#A31515", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#A31515" },
        [LogLevel.None] = new() { Foreground = "#000000", Background = "Transparent" }
    };
}
```

**File**: `src/S7Tools.Infrastructure.Logging/Core/Models/LogDataStoreOptions.cs`

```csharp
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Configuration options for the log data store.
/// </summary>
public sealed class LogDataStoreOptions
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to store in memory.
    /// Default is 10,000 entries.
    /// </summary>
    public int MaxEntries { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the minimum log level to store.
    /// Default is LogLevel.Information.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to include scopes in log entries.
    /// Default is true.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture additional properties from log entries.
    /// Default is true.
    /// </summary>
    public bool CaptureProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the color configuration for different log levels.
    /// </summary>
    public Dictionary<LogLevel, LogEntryColor> Colors { get; set; } = LogEntryColor.VSCodeDarkColors;

    /// <summary>
    /// Gets or sets whether to automatically scroll to new log entries.
    /// Default is true.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for UI updates to improve performance.
    /// Default is 100 entries.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval for batched UI updates in milliseconds.
    /// Default is 100ms.
    /// </summary>
    public int BatchIntervalMs { get; set; } = 100;
}
```

#### 1.1.4: Implement Storage Interface and Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Core/Storage/ILogDataStore.cs`

```csharp
using S7Tools.Infrastructure.Logging.Core.Models;
using System.Collections.Specialized;
using System.ComponentModel;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Interface for storing and retrieving log entries with real-time notifications.
/// </summary>
public interface ILogDataStore : INotifyPropertyChanged, INotifyCollectionChanged, IDisposable
{
    /// <summary>
    /// Gets all log entries currently stored.
    /// </summary>
    IReadOnlyList<LogModel> Entries { get; }

    /// <summary>
    /// Gets the current count of stored log entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the maximum number of entries that can be stored.
    /// </summary>
    int MaxEntries { get; }

    /// <summary>
    /// Gets a value indicating whether the store has reached its maximum capacity.
    /// </summary>
    bool IsFull { get; }

    /// <summary>
    /// Adds a new log entry to the store.
    /// </summary>
    /// <param name="logEntry">The log entry to add.</param>
    void AddEntry(LogModel logEntry);

    /// <summary>
    /// Adds multiple log entries to the store in a batch operation.
    /// </summary>
    /// <param name="logEntries">The log entries to add.</param>
    void AddEntries(IEnumerable<LogModel> logEntries);

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets log entries that match the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter function to apply.</param>
    /// <returns>Filtered log entries.</returns>
    IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter);

    /// <summary>
    /// Gets log entries within the specified time range.
    /// </summary>
    /// <param name="startTime">The start time for filtering.</param>
    /// <param name="endTime">The end time for filtering.</param>
    /// <returns>Log entries within the time range.</returns>
    IEnumerable<LogModel> GetEntriesInTimeRange(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Exports all log entries to a string format.
    /// </summary>
    /// <param name="format">The export format (e.g., "json", "csv", "txt").</param>
    /// <returns>Formatted log entries as a string.</returns>
    Task<string> ExportAsync(string format = "txt");
}
```

Continue with the remaining files in the next step...

### Step 1.2: Create Foundation Services

After completing Step 1.1, proceed to implement the foundation services that will support the VSCode UI:

#### 1.2.1: UI Thread Service
#### 1.2.2: Localization Service  
#### 1.2.3: Layout Service
#### 1.2.4: Activity Bar Service
#### 1.2.5: Theme Service

### Step 1.3: Update Solution and Dependencies

#### 1.3.1: Add Project to Solution
#### 1.3.2: Update Main Project Dependencies
#### 1.3.3: Configure Dependency Injection

## Quality Checklist

Before completing Phase 1, ensure:

### Code Quality
- [ ] All public APIs have comprehensive XML documentation
- [ ] All classes implement proper disposal patterns where needed
- [ ] All async methods use ConfigureAwait(false)
- [ ] All nullable reference types are properly annotated
- [ ] Static analysis passes without warnings

### Architecture Quality
- [ ] Services follow single responsibility principle
- [ ] Interfaces are properly abstracted
- [ ] Dependencies are injected, not created directly
- [ ] Thread safety is maintained for shared resources
- [ ] Performance considerations are addressed

### Integration Quality
- [ ] All services register correctly in DI container
- [ ] No breaking changes to existing functionality
- [ ] Logging infrastructure integrates with Microsoft.Extensions.Logging
- [ ] Resource system supports localization
- [ ] Theme service can switch themes without errors

## Testing Requirements

### Unit Tests Required
- [ ] LogDataStore thread safety and circular buffer functionality
- [ ] DataStoreLogger integration with Microsoft.Extensions.Logging
- [ ] All service implementations with mock dependencies
- [ ] Resource loading and localization functionality
- [ ] Theme switching and persistence

### Integration Tests Required
- [ ] Service dependency injection and resolution
- [ ] End-to-end logging from service to storage
- [ ] Theme service integration with UI components
- [ ] Resource service integration with localization

## Completion Criteria

Phase 1 is complete when:

1. **All Infrastructure Created**: Logging project and foundation services implemented
2. **Services Registered**: All services properly configured in DI container
3. **Tests Passing**: Unit and integration tests pass with >80% coverage
4. **Documentation Complete**: All public APIs documented
5. **Quality Gates Met**: Code analysis passes without warnings
6. **Integration Verified**: Services work together without conflicts

## Next Steps

After completing Phase 1:

1. **Update tracking document** with completed tasks and any issues encountered
2. **Create Phase 2 instructions** based on lessons learned
3. **Prepare handoff documentation** for the next agent
4. **Validate all success criteria** before proceeding

## Support Resources

- [Implementation Plan](../details/unified-s7tools-implementation-plan.md)
- [Project Structure](../details/unified-project-structure.md)
- [Tracking Document](../tracking/unified-s7tools-implementation-tracking.md)
- [Microsoft.Extensions.Logging Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [ReactiveUI Documentation](https://www.reactiveui.net/)

---

**Created**: January 27, 2025  
**Phase**: 1 - Foundation & Infrastructure  
**Status**: Ready for Implementation  
**Estimated Duration**: 5 days