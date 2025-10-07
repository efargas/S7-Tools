# LogViewer Implementation Plan - S7Tools Project

**Implementation Date**: January 2025  
**Project**: S7Tools - Avalonia-based PLC Data Management Application  
**Target Framework**: .NET 8.0  
**Architecture**: MVVM with Avalonia UI + LogViewer Integration

## Executive Summary

This document provides comprehensive implementation instructions for integrating the LogViewer Control system into the S7Tools project. The implementation follows .NET best practices, maintains existing architectural patterns, and introduces a modular logging infrastructure without breaking existing functionality.

**Implementation Approach**: Incremental integration with backward compatibility
**Estimated Timeline**: 15-20 days
**Risk Level**: Medium (due to project restructuring)

## Current Project State Analysis

### ✅ **Existing Patterns to Preserve**
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection with Splat integration
- **MVVM Pattern**: ReactiveUI-based ViewModels with proper data binding
- **Service Layer**: Interface-based services with proper abstractions
- **Avalonia UI**: FluentAvaloniaUI theme integration with FontAwesome icons
- **Project Structure**: Core/UI separation with proper references

### ⚠️ **Patterns to Enhance (Not Break)**
- **Navigation System**: Existing NavigationItemViewModel pattern
- **Service Registration**: Current Program.cs ConfigureServices pattern
- **Configuration**: Extend existing configuration without breaking current setup
- **ViewLocator**: Maintain existing ViewLocator functionality

### 🚫 **What NOT to Change**
- Existing service interfaces (`IGreetingService`, `IClipboardService`, `IDialogService`)
- Current ViewModels (`MainWindowViewModel`, `HomeViewModel`, etc.)
- Existing Views and their DataContext bindings
- Current navigation structure and menu items
- Avalonia project configuration and dependencies

## Phase 1: Project Structure Enhancement

### 1.1 New Project Structure (Additive Only)

```
src/
├── S7Tools/                                    # [EXISTING] Main Avalonia application
│   ├── [EXISTING FILES - DO NOT MODIFY]
│   ├── Views/
│   │   └── Logging/                           # [NEW] Logging views
│   │       ├── LoggingView.axaml
│   │       └── LoggingView.axaml.cs
│   ├── ViewModels/
│   │   └── Logging/                           # [NEW] Logging view models
│   │       └── LoggingViewModel.cs
│   └── appsettings.json                       # [NEW] Configuration file
│
├── S7Tools.Core/                              # [EXISTING] Core business logic
│   └── [EXISTING FILES - DO NOT MODIFY]
│
└── S7Tools.Infrastructure.Logging/            # [NEW] Logging infrastructure
    ├── Core/                                  # Core logging components
    │   ├── Models/
    │   │   ├── LogModel.cs
    │   │   ├── LogEntryColor.cs
    │   │   └── LogDataStoreOptions.cs
    │   ├── Storage/
    │   │   ├── ILogDataStore.cs
    │   │   └── LogDataStore.cs
    │   ├── Configuration/
    │   │   └── DataStoreLoggerConfiguration.cs
    │   └── Interfaces/
    │       └── ILogDataStoreImpl.cs
    ├── Providers/
    │   ├── Microsoft/
    │   │   ├── DataStoreLogger.cs
    │   │   └── DataStoreLoggerProvider.cs
    │   └── Extensions/
    │       └── LoggingServiceCollectionExtensions.cs
    ├── Controls/
    │   ├── LogViewerControl.axaml
    │   ├── LogViewerControl.axaml.cs
    │   └── ViewModels/
    │       └── LogViewerControlViewModel.cs
    ├── Converters/
    │   ├── LogLevelToColorConverter.cs
    │   ├── LogLevelToIconConverter.cs
    │   └── EventIdConverter.cs
    └── S7Tools.Infrastructure.Logging.csproj
```

### 1.2 Implementation Strategy

**Phase 1A: Infrastructure Setup (Days 1-3)**
- Create new logging infrastructure project
- Implement core logging models and storage
- Create Microsoft logger provider

**Phase 1B: Control Development (Days 4-7)**
- Develop LogViewer Avalonia control
- Implement ViewModels with ReactiveUI patterns
- Create value converters for UI binding

**Phase 1C: Integration (Days 8-10)**
- Integrate with existing S7Tools application
- Add logging views and navigation
- Configure dependency injection

**Phase 1D: Testing & Refinement (Days 11-15)**
- Test integration with existing functionality
- Performance optimization
- Documentation and cleanup

## Phase 2: Detailed Implementation Instructions

### 2.1 Create Logging Infrastructure Project

**File**: `src/S7Tools.Infrastructure.Logging/S7Tools.Infrastructure.Logging.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.6" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.3.6" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />
    <PackageReference Include="ReactiveUI" Version="20.1.1" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### 2.2 Core Models Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Core/Models/LogModel.cs`

```csharp
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Represents a single log entry with all associated metadata.
/// </summary>
public class LogModel
{
    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log level (Trace, Debug, Information, Warning, Error, Critical).
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the event identifier associated with the log entry.
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// Gets or sets the category name (typically the logger name/class).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the original state object passed to the logger.
    /// </summary>
    public object? State { get; set; }

    /// <summary>
    /// Gets or sets the formatted log message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the exception information if present.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets the color configuration for displaying this log entry.
    /// </summary>
    public LogEntryColor? Color { get; set; }

    /// <summary>
    /// Gets or sets the scope information for the log entry.
    /// </summary>
    public string? Scope { get; set; }
}
```

**File**: `src/S7Tools.Infrastructure.Logging/Core/Models/LogEntryColor.cs`

```csharp
using System.Drawing;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Defines the color scheme for displaying log entries based on their level.
/// </summary>
public class LogEntryColor
{
    /// <summary>
    /// Gets or sets the foreground (text) color.
    /// </summary>
    public Color Foreground { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color Background { get; set; } = Color.Transparent;
}
```

### 2.3 Storage Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Core/Storage/ILogDataStore.cs`

```csharp
using System.Collections.ObjectModel;
using S7Tools.Infrastructure.Logging.Core.Models;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Defines the contract for storing and retrieving log entries.
/// </summary>
public interface ILogDataStore
{
    /// <summary>
    /// Gets the observable collection of log entries for UI binding.
    /// </summary>
    ObservableCollection<LogModel> Entries { get; }

    /// <summary>
    /// Adds a new log entry to the store.
    /// </summary>
    /// <param name="logModel">The log entry to add.</param>
    void AddEntry(LogModel logModel);

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Retrieves log entries based on specified criteria.
    /// </summary>
    /// <param name="minLevel">Minimum log level to include.</param>
    /// <param name="from">Start date for filtering.</param>
    /// <param name="to">End date for filtering.</param>
    /// <returns>Filtered log entries.</returns>
    Task<IEnumerable<LogModel>> GetEntriesAsync(LogLevel? minLevel = null, DateTime? from = null, DateTime? to = null);
}
```

**File**: `src/S7Tools.Infrastructure.Logging/Core/Storage/LogDataStore.cs`

```csharp
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Models;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Thread-safe implementation of log data storage with circular buffer support.
/// </summary>
public class LogDataStore : ILogDataStore
{
    private static readonly SemaphoreSlim _semaphore = new(1);
    private readonly int _maxEntries;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogDataStore"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the data store.</param>
    public LogDataStore(IOptions<LogDataStoreOptions> options)
    {
        _maxEntries = options.Value.MaxEntries;
    }

    /// <inheritdoc />
    public ObservableCollection<LogModel> Entries { get; } = new();

    /// <inheritdoc />
    public virtual void AddEntry(LogModel logModel)
    {
        ArgumentNullException.ThrowIfNull(logModel);

        _semaphore.Wait();
        try
        {
            // Implement circular buffer logic to prevent memory issues
            if (Entries.Count >= _maxEntries)
            {
                Entries.RemoveAt(0);
            }
            
            Entries.Add(logModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _semaphore.Wait();
        try
        {
            Entries.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LogModel>> GetEntriesAsync(LogLevel? minLevel = null, DateTime? from = null, DateTime? to = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var query = Entries.AsEnumerable();
            
            if (minLevel.HasValue)
                query = query.Where(e => e.LogLevel >= minLevel.Value);
                
            if (from.HasValue)
                query = query.Where(e => e.Timestamp >= from.Value);
                
            if (to.HasValue)
                query = query.Where(e => e.Timestamp <= to.Value);
                
            return query.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 2.4 Configuration Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Core/Configuration/DataStoreLoggerConfiguration.cs`

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Models;
using System.Drawing;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration options for the DataStore logger provider.
/// </summary>
public class DataStoreLoggerConfiguration
{
    /// <summary>
    /// Gets or sets the default event ID to use when none is specified.
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// Gets the color mapping for different log levels.
    /// </summary>
    public Dictionary<LogLevel, LogEntryColor> Colors { get; } = new()
    {
        [LogLevel.Trace] = new() { Foreground = Color.DarkGray },
        [LogLevel.Debug] = new() { Foreground = Color.Gray },
        [LogLevel.Information] = new() { Foreground = Color.Black },
        [LogLevel.Warning] = new() { Foreground = Color.Orange },
        [LogLevel.Error] = new() { Foreground = Color.White, Background = Color.OrangeRed },
        [LogLevel.Critical] = new() { Foreground = Color.White, Background = Color.Red },
        [LogLevel.None] = new() { Foreground = Color.Black },
    };
}

/// <summary>
/// Configuration options for the log data store.
/// </summary>
public class LogDataStoreOptions
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to keep in memory.
    /// Default is 10,000 entries.
    /// </summary>
    public int MaxEntries { get; set; } = 10_000;
}
```

### 2.5 Microsoft Logger Provider Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Providers/Microsoft/DataStoreLogger.cs`

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// Custom logger implementation that stores log entries in a data store for UI display.
/// </summary>
public class DataStoreLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Func<DataStoreLoggerConfiguration> _getCurrentConfig;
    private readonly ILogDataStore _dataStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for this logger.</param>
    /// <param name="getCurrentConfig">Function to get current configuration.</param>
    /// <param name="dataStore">The data store for log entries.</param>
    public DataStoreLogger(
        string categoryName,
        Func<DataStoreLoggerConfiguration> getCurrentConfig,
        ILogDataStore dataStore)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _getCurrentConfig = getCurrentConfig ?? throw new ArgumentNullException(nameof(getCurrentConfig));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        ArgumentNullException.ThrowIfNull(formatter);

        var config = _getCurrentConfig();
        var message = formatter(state, exception);

        _dataStore.AddEntry(new LogModel
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId.Id == 0 && config.EventId.Id != 0 ? config.EventId : eventId,
            Category = _categoryName,
            State = state,
            Message = message,
            Exception = exception?.ToString(),
            Color = config.Colors.GetValueOrDefault(logLevel, new LogEntryColor())
        });
    }
}
```

**File**: `src/S7Tools.Infrastructure.Logging/Providers/Microsoft/DataStoreLoggerProvider.cs`

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using System.Collections.Concurrent;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// Logger provider that creates DataStore loggers for the Microsoft logging framework.
/// </summary>
public class DataStoreLoggerProvider : ILoggerProvider
{
    private readonly IOptionsMonitor<DataStoreLoggerConfiguration> _config;
    private readonly ILogDataStore _dataStore;
    private readonly ConcurrentDictionary<string, DataStoreLogger> _loggers = new();
    private readonly IDisposable? _onChangeToken;
    private DataStoreLoggerConfiguration _currentConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreLoggerProvider"/> class.
    /// </summary>
    /// <param name="config">Configuration monitor for logger options.</param>
    /// <param name="dataStore">The data store for log entries.</param>
    public DataStoreLoggerProvider(
        IOptionsMonitor<DataStoreLoggerConfiguration> config,
        ILogDataStore dataStore)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name =>
            new DataStoreLogger(name, GetCurrentConfig, _dataStore));

    private DataStoreLoggerConfiguration GetCurrentConfig() => _currentConfig;

    /// <inheritdoc />
    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}
```

### 2.6 Service Registration Extensions

**File**: `src/S7Tools.Infrastructure.Logging/Providers/Extensions/LoggingServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

/// <summary>
/// Extension methods for configuring logging services.
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the logging infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLoggingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register core services
        services.AddSingleton<ILogDataStore, LogDataStore>();
        
        // Configure options
        services.Configure<DataStoreLoggerConfiguration>(
            configuration.GetSection("DataStoreLogger"));
        services.Configure<LogDataStoreOptions>(
            configuration.GetSection("DataStoreLogger"));

        return services;
    }

    /// <summary>
    /// Adds the DataStore logger provider to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDataStoreLogger(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, DataStoreLoggerProvider>());
        
        return builder;
    }

    /// <summary>
    /// Adds the DataStore logger provider with custom configuration.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDataStoreLogger(
        this ILoggingBuilder builder,
        Action<DataStoreLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddDataStoreLogger();
        builder.Services.Configure(configure);
        
        return builder;
    }
}
```

### 2.7 LogViewer Control Implementation

**File**: `src/S7Tools.Infrastructure.Logging/Controls/LogViewerControl.axaml`

```xml
<UserControl x:Class="S7Tools.Infrastructure.Logging.Controls.LogViewerControl"
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="clr-namespace:S7Tools.Infrastructure.Logging.Converters"
             xmlns:vm="clr-namespace:S7Tools.Infrastructure.Logging.Controls.ViewModels"
             x:DataType="vm:LogViewerControlViewModel"
             DataContextChanged="OnDataContextChanged"
             DetachedFromLogicalTree="OnDetachedFromLogicalTree">

    <UserControl.Resources>
        <converters:LogLevelToColorConverter x:Key="LogLevelToColorConverter" />
        <converters:LogLevelToIconConverter x:Key="LogLevelToIconConverter" />
        <converters:EventIdConverter x:Key="EventIdConverter" />
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <Border Grid.Row="0" 
                Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}" 
                Padding="8,4"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}"
                BorderThickness="0,0,0,1">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Clear" 
                        Command="{Binding ClearLogsCommand}"
                        ToolTip.Tip="Clear all log entries" />
                <Button Content="Export" 
                        Command="{Binding ExportLogsCommand}"
                        ToolTip.Tip="Export logs to file" />
                <Separator />
                <ComboBox Items="{Binding LogLevels}" 
                         SelectedItem="{Binding SelectedLogLevel}"
                         PlaceholderText="Filter by level"
                         Width="150"
                         ToolTip.Tip="Filter logs by minimum level" />
                <TextBox Text="{Binding SearchText}" 
                        Watermark="Search logs..." 
                        Width="200"
                        ToolTip.Tip="Search in log messages, categories, and exceptions" />
                <Button Content="🔍" 
                        Command="{Binding SearchCommand}"
                        ToolTip.Tip="Apply search filter" />
            </StackPanel>
        </Border>

        <!-- Log Entries -->
        <DataGrid Grid.Row="1" 
                  x:Name="LogDataGrid"
                  Items="{Binding FilteredEntries}"
                  AutoGenerateColumns="False"
                  CanUserSortColumns="True"
                  GridLinesVisibility="Horizontal"
                  IsReadOnly="True"
                  SelectionMode="Extended"
                  LayoutUpdated="OnLayoutUpdated">
            
            <DataGrid.Styles>
                <Style Selector="DataGridRow">
                    <Setter Property="Foreground" 
                            Value="{Binding Color.Foreground, Converter={StaticResource LogLevelToColorConverter}}" />
                    <Setter Property="Background" 
                            Value="{Binding Color.Background, Converter={StaticResource LogLevelToColorConverter}}" />
                </Style>
                <Style Selector="DataGridCell">
                    <Setter Property="FontSize" Value="11" />
                    <Setter Property="Padding" Value="4,2" />
                </Style>
            </DataGrid.Styles>

            <DataGrid.Columns>
                <DataGridTextColumn Header="Time" 
                                   Binding="{Binding Timestamp, StringFormat='{}{0:HH:mm:ss.fff}'}" 
                                   Width="100" />
                <DataGridTemplateColumn Header="Level" Width="80">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{Binding LogLevel, Converter={StaticResource LogLevelToIconConverter}}" 
                                         Width="12" Height="12" />
                                <TextBlock Text="{Binding LogLevel}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="Category" 
                                   Binding="{Binding Category}" 
                                   Width="150" />
                <DataGridTextColumn Header="Event" 
                                   Binding="{Binding EventId, Converter={StaticResource EventIdConverter}}" 
                                   Width="80" />
                <DataGridTextColumn Header="Message" 
                                   Binding="{Binding Message}" 
                                   Width="*" />
                <DataGridTextColumn Header="Exception" 
                                   Binding="{Binding Exception}" 
                                   Width="200" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- Status Bar -->
        <Border Grid.Row="2" 
                Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}" 
                Padding="8,4"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}"
                BorderThickness="0,1,0,0">
            <StackPanel Orientation="Horizontal" Spacing="16">
                <TextBlock Text="{Binding TotalEntries, StringFormat='Total: {0}'}" />
                <TextBlock Text="{Binding FilteredCount, StringFormat='Filtered: {0}'}" />
                <CheckBox Content="Auto Scroll" 
                         IsChecked="{Binding AutoScroll}"
                         ToolTip.Tip="Automatically scroll to new log entries" />
                <CheckBox Content="Word Wrap" 
                         IsChecked="{Binding WordWrap}"
                         ToolTip.Tip="Enable word wrapping in log messages" />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

### 2.8 Integration with S7Tools Application

**File**: `src/S7Tools/appsettings.json` (NEW FILE)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning",
      "S7Tools": "Debug"
    }
  },
  "DataStoreLogger": {
    "MaxEntries": 10000,
    "Colors": {
      "Trace": {
        "Foreground": "Gray",
        "Background": "Transparent"
      },
      "Debug": {
        "Foreground": "DarkGray",
        "Background": "Transparent"
      },
      "Information": {
        "Foreground": "Black",
        "Background": "Transparent"
      },
      "Warning": {
        "Foreground": "DarkOrange",
        "Background": "Transparent"
      },
      "Error": {
        "Foreground": "White",
        "Background": "Red"
      },
      "Critical": {
        "Foreground": "Yellow",
        "Background": "DarkRed"
      }
    }
  }
}
```

**File**: `src/S7Tools/Views/Logging/LoggingView.axaml` (NEW FILE)

```xml
<UserControl x:Class="S7Tools.Views.Logging.LoggingView"
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:logging="clr-namespace:S7Tools.Infrastructure.Logging.Controls;assembly=S7Tools.Infrastructure.Logging"
             xmlns:vm="clr-namespace:S7Tools.ViewModels.Logging"
             x:DataType="vm:LoggingViewModel">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" 
                Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
                Padding="16,8">
            <StackPanel>
                <TextBlock Text="Application Logs" 
                          FontSize="18" 
                          FontWeight="SemiBold" />
                <TextBlock Text="Real-time view of application logging events" 
                          Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}" />
            </StackPanel>
        </Border>

        <!-- LogViewer Control -->
        <logging:LogViewerControl Grid.Row="1" 
                                 DataContext="{Binding LogViewer}" />
    </Grid>
</UserControl>
```

### 2.9 Updated Program.cs Integration

**File**: `src/S7Tools/Program.cs` (MODIFY EXISTING)

```csharp
using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Logging;
using S7Tools.Views;
using Splat.Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Infrastructure.Logging.Controls.ViewModels;

namespace S7Tools;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        IconProvider.Current.Register<FontAwesomeIconProvider>();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
        => AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // [EXISTING] Core Services - DO NOT MODIFY
        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITagRepository, PlcDataService>();
        services.AddSingleton<IS7ConnectionProvider, PlcDataService>();

        // [NEW] Logging Infrastructure
        services.AddLoggingInfrastructure(configuration);

        // [EXISTING] ViewModels - DO NOT MODIFY
        services.AddSingleton<MainWindowViewModel>();

        // [NEW] Logging ViewModels
        services.AddSingleton<LogViewerControlViewModel>();
        services.AddSingleton<LoggingViewModel>();

        // Configure Splat to use the Microsoft.Extensions.DependencyInjection container.
        services.UseMicrosoftDependencyResolver();

        // [EXISTING] Views - DO NOT MODIFY
        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>()
        });

        // [NEW] Configure Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
            builder.AddDataStoreLogger(); // Add our custom logger
        });
    }
}
```

### 2.10 Navigation Integration

**File**: `src/S7Tools/ViewModels/MainWindowViewModel.cs` (MODIFY EXISTING - ADD LOGGING NAVIGATION)

```csharp
// Add this to the existing NavigationItems initialization in MainWindowViewModel constructor:

NavigationItems.Add(new NavigationItemViewModel
{
    Label = "Logs",
    Icon = "fa-file-text-o", // FontAwesome icon
    TargetView = typeof(Views.Logging.LoggingView)
});
```

## Phase 3: Testing and Validation

### 3.1 Integration Testing Checklist

**✅ Functional Tests**
- [ ] LogViewer displays log entries in real-time
- [ ] Color coding works for different log levels
- [ ] Auto-scroll functionality works
- [ ] Search and filtering work correctly
- [ ] Clear logs functionality works
- [ ] Navigation to logging view works
- [ ] Existing functionality remains unaffected

**✅ Performance Tests**
- [ ] UI remains responsive with 1000+ log entries
- [ ] Memory usage is stable with circular buffer
- [ ] No memory leaks in event subscriptions
- [ ] Startup time impact is minimal

**✅ Configuration Tests**
- [ ] appsettings.json configuration is loaded correctly
- [ ] Color customization works
- [ ] Log level filtering works
- [ ] Maximum entries limit is respected

### 3.2 Regression Testing

**✅ Existing Functionality**
- [ ] Home view works correctly
- [ ] Connections view works correctly
- [ ] Settings view works correctly
- [ ] About view works correctly
- [ ] All existing services function properly
- [ ] Navigation between views works
- [ ] Existing ViewModels are not affected

## Phase 4: Documentation and Cleanup

### 4.1 Code Documentation

- Add comprehensive XML documentation to all public APIs
- Create README.md for the logging infrastructure project
- Document configuration options and customization
- Create usage examples and best practices guide

### 4.2 Performance Optimization

- Implement virtualization for large log datasets
- Add background processing for log entry filtering
- Optimize memory usage with weak references where appropriate
- Add performance monitoring and metrics

## Implementation Notes

### ⚠️ **Critical Considerations**

1. **Backward Compatibility**: All existing functionality must continue to work
2. **Thread Safety**: All logging operations must be thread-safe
3. **Memory Management**: Implement circular buffer to prevent memory leaks
4. **Performance**: UI must remain responsive with large numbers of log entries
5. **Configuration**: Support both code-based and file-based configuration

### 🔧 **Best Practices to Follow**

1. **Dependency Injection**: Use constructor injection with proper null checks
2. **Async/Await**: Use async patterns for all I/O operations
3. **Resource Management**: Implement proper disposal patterns
4. **Error Handling**: Add comprehensive error handling and logging
5. **Testing**: Write unit tests for all new components

### 📋 **Success Criteria**

- [ ] LogViewer integrates seamlessly with existing S7Tools application
- [ ] Real-time log display with color coding and filtering
- [ ] No performance degradation of existing functionality
- [ ] Comprehensive documentation and examples
- [ ] All tests pass and code coverage is adequate
- [ ] Configuration is flexible and user-friendly

This implementation plan provides a comprehensive roadmap for integrating the LogViewer system while maintaining the existing S7Tools architecture and following .NET best practices.