# Command Handler Pattern Implementation Guide

## Overview

The Command Handler Pattern provides a structured approach to handling application commands with centralized execution, validation, and error handling. This guide explains how to implement and use this pattern in S7Tools.

## Why Command Handler Pattern?

### Benefits

1. **Separation of Concerns** - Separates command definition from execution logic
2. **Testability** - Easier to test command handlers in isolation
3. **Reusability** - Command handlers can be reused across different contexts
4. **Validation** - Centralized validation logic for command parameters
5. **Error Handling** - Consistent error handling across all commands
6. **Logging** - Automatic logging of command execution and errors
7. **Cancellation** - Built-in support for cancellation tokens

### Current State vs. Desired State

**Current (ReactiveCommand Only)**:
```csharp
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public MyViewModel()
{
    SaveCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        // Inline command logic
        await _service.SaveAsync();
    });
}
```

**Desired (Command Handler Pattern)**:
```csharp
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public MyViewModel(ICommandDispatcher commandDispatcher)
{
    SaveCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        var command = new SaveDataCommand { Data = CurrentData };
        var result = await commandDispatcher.ExecuteAsync(command);
        if (!result.IsSuccess)
        {
            // Handle error
        }
    });
}
```

## Architecture

### Core Components

1. **ICommand** - Marker interface for commands
2. **ICommandHandler&lt;TCommand&gt;** - Interface for command handlers
3. **ICommandDispatcher** - Dispatches commands to their handlers
4. **CommandResult** - Represents the result of command execution
5. **CommandValidationException** - Thrown for validation errors

### Class Diagram

```
┌─────────────────┐
│   ICommand      │ (Marker Interface)
└────────┬────────┘
         │
         │ implements
         ▼
┌─────────────────────────┐
│  SaveDataCommand        │
│  - Data: string         │
└─────────────────────────┘
         │
         │ handled by
         ▼
┌──────────────────────────────────────┐
│  ICommandHandler<SaveDataCommand>    │
│  + ExecuteAsync(cmd, token): Result  │
└──────────────────────────────────────┘
         ▲
         │ implements
         │
┌──────────────────────────────────────┐
│  SaveDataCommandHandler              │
│  - _service: IDataService            │
│  + ExecuteAsync(cmd, token): Result  │
└──────────────────────────────────────┘
```

## Implementation Steps

### Step 1: Define Core Interfaces

Create `src/S7Tools.Core/Commands/ICommand.cs`:

```csharp
namespace S7Tools.Core.Commands;

/// <summary>
/// Marker interface for all commands in the application.
/// </summary>
public interface ICommand
{
}
```

Create `src/S7Tools.Core/Commands/ICommandHandler.cs`:

```csharp
namespace S7Tools.Core.Commands;

/// <summary>
/// Defines a handler for a command.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the execution of a command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<CommandResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

Create `src/S7Tools.Core/Commands/CommandResult.cs`:

```csharp
namespace S7Tools.Core.Commands;

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets a value indicating whether the command executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets any additional data from the command execution.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    public static CommandResult Success(object? data = null) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed command result.
    /// </summary>
    public static CommandResult Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}
```

Create `src/S7Tools.Core/Commands/ICommandDispatcher.cs`:

```csharp
namespace S7Tools.Core.Commands;

/// <summary>
/// Dispatches commands to their registered handlers.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Executes a command by dispatching it to the appropriate handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : ICommand;
}
```

### Step 2: Implement Command Dispatcher

Create `src/S7Tools/Services/CommandDispatcher.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services;

/// <summary>
/// Dispatches commands to their registered handlers using dependency injection.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
    /// </summary>
    public CommandDispatcher(IServiceProvider serviceProvider, ILogger<CommandDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var commandName = typeof(TCommand).Name;
        _logger.LogDebug("Executing command: {CommandName}", commandName);

        try
        {
            // Resolve the command handler from DI container
            var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
            
            if (handler == null)
            {
                _logger.LogError("No handler registered for command: {CommandName}", commandName);
                return CommandResult.Failure($"No handler registered for command: {commandName}");
            }

            // Execute the command
            var result = await handler.ExecuteAsync(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Command executed successfully: {CommandName}", commandName);
            }
            else
            {
                _logger.LogWarning("Command failed: {CommandName}. Error: {Error}", commandName, result.Error);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Command cancelled: {CommandName}", commandName);
            return CommandResult.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception executing command: {CommandName}", commandName);
            return CommandResult.Failure($"Unhandled exception: {ex.Message}");
        }
    }
}
```

### Step 3: Register Services

Update `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`:

```csharp
// Add to ConfigureServices method
services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

// Register command handlers
services.AddTransient<ICommandHandler<SaveDataCommand>, SaveDataCommandHandler>();
services.AddTransient<ICommandHandler<ExportLogsCommand>, ExportLogsCommandHandler>();
// ... register other handlers
```

### Step 4: Create Commands and Handlers

#### Example Command

Create `src/S7Tools.Core/Commands/ExportLogsCommand.cs`:

```csharp
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Models;
using System.Collections.Generic;

namespace S7Tools.Core.Commands;

/// <summary>
/// Command to export log entries to a file.
/// </summary>
public class ExportLogsCommand : ICommand
{
    /// <summary>
    /// Gets or sets the log entries to export.
    /// </summary>
    public required IEnumerable<LogModel> Logs { get; init; }

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public required ExportFormat Format { get; init; }

    /// <summary>
    /// Gets or sets the optional file path. If null, a default path is used.
    /// </summary>
    public string? FilePath { get; init; }
}
```

#### Example Command Handler

Create `src/S7Tools/Services/CommandHandlers/ExportLogsCommandHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Core.Commands;
using S7Tools.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services.CommandHandlers;

/// <summary>
/// Handler for the ExportLogsCommand.
/// </summary>
public class ExportLogsCommandHandler : ICommandHandler<ExportLogsCommand>
{
    private readonly ILogExportService _exportService;
    private readonly ILogger<ExportLogsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportLogsCommandHandler"/> class.
    /// </summary>
    public ExportLogsCommandHandler(
        ILogExportService exportService, 
        ILogger<ExportLogsCommandHandler> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(
        ExportLogsCommand command, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            // Validate command
            if (command.Logs == null || !command.Logs.Any())
            {
                return CommandResult.Failure("No logs provided for export");
            }

            _logger.LogInformation(
                "Exporting {Count} logs to {Format} format", 
                command.Logs.Count(), 
                command.Format);

            // Execute export
            var result = await _exportService.ExportLogsAsync(
                command.Logs, 
                command.Format, 
                command.FilePath);

            return result.IsSuccess 
                ? CommandResult.Success() 
                : CommandResult.Failure(result.Error ?? "Export failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ExportLogsCommand");
            return CommandResult.Failure($"Export failed: {ex.Message}");
        }
    }
}
```

### Step 5: Use in ViewModels

#### Before (Direct Service Call)

```csharp
public class LogViewerViewModel : ViewModelBase
{
    private readonly ILogExportService _exportService;

    public ReactiveCommand<string, Unit> ExportLogsCommand { get; }

    public LogViewerViewModel(ILogExportService exportService)
    {
        _exportService = exportService;

        ExportLogsCommand = ReactiveCommand.CreateFromTask<string>(async formatString =>
        {
            var format = ParseFormat(formatString);
            var logs = FilteredLogEntries.ToList();
            
            // Inline logic and error handling
            if (!logs.Any())
            {
                await ShowErrorAsync("No logs to export");
                return;
            }

            var result = await _exportService.ExportLogsAsync(logs, format);
            // ... handle result
        });
    }
}
```

#### After (Command Handler Pattern)

```csharp
public class LogViewerViewModel : ViewModelBase
{
    private readonly ICommandDispatcher _commandDispatcher;

    public ReactiveCommand<string, Unit> ExportLogsCommand { get; }

    public LogViewerViewModel(ICommandDispatcher commandDispatcher)
    {
        _commandDispatcher = commandDispatcher;

        ExportLogsCommand = ReactiveCommand.CreateFromTask<string>(async formatString =>
        {
            var command = new ExportLogsCommand
            {
                Logs = FilteredLogEntries.ToList(),
                Format = ParseFormat(formatString),
                FilePath = null // Use default
            };

            var result = await _commandDispatcher.ExecuteAsync(command);
            
            if (result.IsSuccess)
            {
                await ShowSuccessAsync("Logs exported successfully");
            }
            else
            {
                await ShowErrorAsync(result.Error ?? "Export failed");
            }
        });
    }
}
```

## Advanced Patterns

### Command with Return Value

```csharp
public class CommandResult<T> : CommandResult
{
    public T? Value { get; init; }

    public static CommandResult<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public new static CommandResult<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}

// Usage
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand
{
    Task<CommandResult<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

### Command Validation

```csharp
public abstract class ValidatableCommand : ICommand
{
    public virtual ValidationResult Validate()
    {
        return ValidationResult.Success();
    }
}

public class ExportLogsCommand : ValidatableCommand
{
    public IEnumerable<LogModel> Logs { get; init; }

    public override ValidationResult Validate()
    {
        if (Logs == null || !Logs.Any())
        {
            return ValidationResult.Error("No logs provided");
        }

        return ValidationResult.Success();
    }
}
```

### Command Pipeline

```csharp
public interface ICommandPipelineBehavior<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(
        TCommand command, 
        Func<Task<CommandResult>> next, 
        CancellationToken cancellationToken);
}

// Logging behavior
public class LoggingBehavior<TCommand> : ICommandPipelineBehavior<TCommand> where TCommand : ICommand
{
    private readonly ILogger<LoggingBehavior<TCommand>> _logger;

    public async Task<CommandResult> HandleAsync(
        TCommand command, 
        Func<Task<CommandResult>> next, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing command: {CommandName}", typeof(TCommand).Name);
        var result = await next();
        _logger.LogInformation("Command executed: {CommandName}, Success: {Success}", 
            typeof(TCommand).Name, result.IsSuccess);
        return result;
    }
}
```

## Testing

### Unit Testing Command Handlers

```csharp
public class ExportLogsCommandHandlerTests
{
    private readonly Mock<ILogExportService> _mockExportService;
    private readonly Mock<ILogger<ExportLogsCommandHandler>> _mockLogger;
    private readonly ExportLogsCommandHandler _handler;

    public ExportLogsCommandHandlerTests()
    {
        _mockExportService = new Mock<ILogExportService>();
        _mockLogger = new Mock<ILogger<ExportLogsCommandHandler>>();
        _handler = new ExportLogsCommandHandler(_mockExportService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var logs = new List<LogModel> { new LogModel { Message = "Test" } };
        var command = new ExportLogsCommand
        {
            Logs = logs,
            Format = ExportFormat.Text
        };

        _mockExportService
            .Setup(x => x.ExportLogsAsync(It.IsAny<IEnumerable<LogModel>>(), It.IsAny<ExportFormat>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.ExecuteAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        _mockExportService.Verify(x => x.ExportLogsAsync(logs, ExportFormat.Text, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyLogs_ReturnsFailure()
    {
        // Arrange
        var command = new ExportLogsCommand
        {
            Logs = Array.Empty<LogModel>(),
            Format = ExportFormat.Text
        };

        // Act
        var result = await _handler.ExecuteAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No logs provided for export", result.Error);
    }
}
```

### Integration Testing

```csharp
public class CommandDispatcherIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ICommandDispatcher _dispatcher;

    public CommandDispatcherIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<ICommandHandler<ExportLogsCommand>, ExportLogsCommandHandler>();
        services.AddSingleton<ILogExportService, LogExportService>();

        _serviceProvider = services.BuildServiceProvider();
        _dispatcher = _serviceProvider.GetRequiredService<ICommandDispatcher>();
    }

    [Fact]
    public async Task Dispatcher_ExecutesRegisteredCommand_Successfully()
    {
        // Arrange
        var command = new ExportLogsCommand
        {
            Logs = new List<LogModel> { new LogModel { Message = "Test" } },
            Format = ExportFormat.Text
        };

        // Act
        var result = await _dispatcher.ExecuteAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
```

## Migration Strategy

### Phase 1: Setup Infrastructure (Week 1)
1. Create core command interfaces and classes
2. Implement CommandDispatcher
3. Register CommandDispatcher in DI container
4. Write unit tests for infrastructure

### Phase 2: Migrate High-Value Commands (Week 2-3)
Priority commands to migrate:
1. **ExportLogsCommand** - Already has complex logic
2. **SaveSettingsCommand** - Used frequently
3. **LoadConfigurationCommand** - Critical path
4. **ConnectToPlcCommand** - Complex with error handling

### Phase 3: Migrate Remaining Commands (Week 4-5)
1. Identify all ReactiveCommand usages
2. Create commands for each
3. Implement handlers
4. Update ViewModels
5. Add tests

### Phase 4: Cleanup and Documentation (Week 6)
1. Remove old inline command logic
2. Update documentation
3. Conduct code review
4. Performance testing

## Best Practices

### DO

✅ **Keep commands immutable** - Use `init` accessors
```csharp
public class MyCommand : ICommand
{
    public required string Data { get; init; }
}
```

✅ **Validate in handlers** - Centralize validation logic
```csharp
public async Task<CommandResult> ExecuteAsync(MyCommand command, CancellationToken cancellationToken)
{
    if (string.IsNullOrEmpty(command.Data))
    {
        return CommandResult.Failure("Data is required");
    }
    // ... execute
}
```

✅ **Log appropriately** - Log command execution and errors
```csharp
_logger.LogInformation("Executing {CommandName}", nameof(MyCommand));
_logger.LogError(ex, "Error executing {CommandName}", nameof(MyCommand));
```

✅ **Handle cancellation** - Respect cancellation tokens
```csharp
public async Task<CommandResult> ExecuteAsync(MyCommand command, CancellationToken cancellationToken)
{
    await _service.ProcessAsync(cancellationToken);
}
```

### DON'T

❌ **Don't put business logic in commands** - Commands are data holders
```csharp
// BAD
public class MyCommand : ICommand
{
    public void Execute() { /* logic */ } // Don't do this
}

// GOOD
public class MyCommand : ICommand
{
    public required string Data { get; init; }
}
```

❌ **Don't access UI from handlers** - Keep handlers UI-agnostic
```csharp
// BAD
public async Task<CommandResult> ExecuteAsync(MyCommand command, CancellationToken cancellationToken)
{
    await ShowDialogAsync(); // Don't access UI
}

// GOOD
public async Task<CommandResult> ExecuteAsync(MyCommand command, CancellationToken cancellationToken)
{
    return CommandResult.Success(); // Return result, let ViewModel handle UI
}
```

❌ **Don't create too many small commands** - Balance granularity
```csharp
// Too granular - probably not needed
public class IncrementCounterCommand : ICommand { }
public class DecrementCounterCommand : ICommand { }

// Better - combine related operations
public class UpdateCounterCommand : ICommand
{
    public required int Delta { get; init; }
}
```

## Benefits in S7Tools Context

1. **Consistency** - All commands follow same pattern
2. **Logging** - Automatic logging of all command executions
3. **Error Handling** - Centralized, consistent error handling
4. **Testing** - Easier to test command logic in isolation
5. **Maintainability** - Clear separation of concerns
6. **Extensibility** - Easy to add new commands and behaviors

## Related Patterns

- **CQRS (Command Query Responsibility Segregation)** - Separate commands from queries
- **Mediator Pattern** - CommandDispatcher acts as a mediator
- **Chain of Responsibility** - Command pipeline behaviors

## References

- [Martin Fowler - Command Pattern](https://martinfowler.com/bliki/CommandOrientedInterface.html)
- [Microsoft - CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Clean Architecture - Commands](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Last Updated**: 2025
**Maintained By**: S7Tools Development Team
