# CODEPROJECT

Source: <https://www.codeproject.com/articles/LogViewer-Control-for-WinForms-WPF-and-Avalonia-in>

---

* [Download LogViewerControl_v1.30 - 7.8 MB](https://api-main.codeproject.com/v1/article/5357417/downloadAttachment?src=5357417/LogViewerControl_v1.30.zip) [NEW]
* [Download LogViewerControl_v1.20a - 7.7 MB](https://api-main.codeproject.com/v1/article/5357417/downloadAttachment?src=5357417/LogViewerControl_v1.20a.zip) [OBSOLETE]
* [Download LogViewerControl_v1.20 - 7.4 MB](https://api-main.codeproject.com/v1/article/5357417/downloadAttachment?src=5357417/LogViewerControl_v1.20.zip) [OBSOLETE]
* [Download LogViewerControl_v1.10 - 8 MB](https://api-main.codeproject.com/v1/article/5357417/downloadAttachment?src=5357417/LogViewerControl_v1.10.zip) [OBSOLETE]
* [Download LogViewerControl_v1.00 - 7.2 MB](https://api-main.codeproject.com/v1/article/5357417/downloadAttachment?src=5357417/LogViewerControl_v1.00.zip) [OBSOLETE]

## Introduction

I was working on a solution that required a Viewer for **Logger** entries in the app itself for live viewing of what was happening behind the scene.

I wanted something prettier than the console output and something that could be added to a **Winforms**, **WPF**, or **Avalonia** application that felt part of the application, and possibly something that a user may need to view - i.e., User Friendly, not the following:

![Article image](https://cloudfront.codeproject.com/articles/5357417/console_logging_640.png)

The requirements for the `LoggerViewer` are:

* Defined as a **control** that could be added or injected via dependency injection
* **Native** for **WinForms**, **WPF**, and **Avalonia** applications
* Support multiple Operating Systems - **Windows**, **MacOS**, **Linux**
* Support multiple Logging Frameworks - **Microsoft** (default), **Serilog**, and **NLog**
* Support **colorization** (custom colors as a bonus)
* **Dependency Injection** (DI) and non-DI usage
* **MVVM** (Model View ViewModel design pattern) and non-MVVM usage
* **History** viewable in any list control, a `ListView` / `DataGrid` control
* Selectable **auto-scrolling** to keep the latest entry visible
* **AppSettings.Json** file support for configurable logging
* Capture **framework API logging**
* Work in parallel with other Loggers

We will be looking into Logging - how it works and look at the framework code that makes it work.

As we will be covering **WPF**, **WinForms**, and **Avalonia** project types, **Microsoft** and **Serilog** loggers, and also using / not using Dependency Injection, this article will be a bit lengthy.

If you are not interested in how it all works, then see the animations in the [Preview](#preview) section below, download the code, and run the application(s) that are applicable to your use case in the language that you work in.

### Preview

Before we get started, let's look at what we want to achieve. The **WPF**, **WinForms**, and **Avalonia** versions of the `LogViewerControl` look almost identical and work the same for both the **C#** & **VB** versions.

Here is a GIF with **default** colorization for the **WinForms** version in **C#**, using Dependency Injection and data-binding:

![Article image](https://cloudfront.codeproject.com/articles/5357417/winform_logviewer.gif)

Here is a GIF with **custom** colorization for the **WPF** version, minimal implementation in **VB**, no Dependency injection, three lines of code:

![Article image](https://cloudfront.codeproject.com/articles/5357417/wpf_logviewer.gif)

Lastly, here is proof that you can develop an application for **Mac OS** using **VB**, yes Visual Basic, using the **Avalonia** Framework! Whilst VB is not supported out-of-the-box, as there are no included Application, Class, or Control library templates with the exception of a [Github repository](https://github.com/mevdschee/avalonia-vb-template-app) that is not complete, I will cover how to get **VB** to use the **Avalonia** framework for both application and control project types.

![Article image](https://cloudfront.codeproject.com/articles/5357417/mac_avalonia_logging_vb.gif)

***Note**: The three animated GIFs may take a moment to load...*

## Contents

* [Introduction](#introduction)
    * [Preview](#preview)
* [Prerequisites](#prerequisites)
* [Solution Setup](#solution-setup)
    * [Logging Flow](#logging-flow)
    * [Application Architecture](#application-architecture)
    * [Solution Architecture](#solution-architecture)
* [How Does Logging Work?](#how-does-logging-work)
    * [Logger Internals](#logger-internals)
* [Summary](#summary)
* [References](#references)
    * [Documentation, Articles, etc](#documentation-articles-etc)
* [History](#history)

## Prerequisites

The code that accompanies this article is for .NET Core only. Version 7.03 was used and Nullable is enabled. However, if required, it can be modified to support .NET 3.1 or later.

The solution was built using **Visual Studio 2022 v17.4.5** and fully tested with **Rider 2022.3.2**.

The NuGet packages used in this article are listed in the References section at the end of this article.

The `AppSettings` helper class was used to simplify reading the configuration settings from the *appsettings*.json* files. There is an article that deep-dives into how this works: [.NET App Settings Demystified (C# & VB | CodeProject)](https://www.codeproject.com/Articles/5354478/NET-App-Settings-Demystified-Csharp-VB).

If you are not familiar with Logging, then take a moment to read this [Logging in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line) which covers the fundamentals.

As we are implementing a Custom Logger and Provider, and you're not familiar with creating a custom logger and provider, please take a moment to read [Implement a custom logging provider in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider).

We will also be covering Dependency Injection (DI). I provide solutions that use and do not use DI, so DI is not essential. If you are interested in learning more, please read this: [Dependency injection in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection).

Lastly, we will be covering MVVM (Model View ViewModel design pattern). I provide solutions that use and do not use MVVM, so MVVM is not essential. If you are interested in learning more, please read this: [Model-View-ViewModel (MVVM) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm#the-mvvm-pattern).

## Solution Setup

As we are covering 3 project types, the structure of the solution attempts to minimize duplication of code. Also, the projects are broken into 4 parts: Application, Controls, Core, and Background Service:

1. The application demonstrates how to implement in your own applications.
2. Controls are what you add to your own applications for the UI component.
3. Core contains common code, application type-specific code, and custom logger implementations. The custom logger implementations are independent of the controls, and choose which one or roll your own for another logger framework.
4. The Background Service is simply a dummy service to simulate the generation of logging messages. The Service is common to all application types.

### Logging Flow

We can simplify the design concept with the diagram below:

![Article image](https://cloudfront.codeproject.com/articles/5357417/design_640.png)

The logic flow, as per the diagram above, is as follows:

1. Application logs an event (`Trace`, `Debug`, `Information`, `Warning`, `Error`, or `Critical`) with the appropriate information.
2. The `Logger` Framework passes the `Log` Event to all registered `Logger`s, including our custom logger(s).
3. The `Logger`s store the `Log` Event in the `DataStore`.
4. The `LogViewer` control receives a data-binding notification and displays the Log Event.

### Application Architecture

The application architecture is the same for all application types:

![Article image](https://cloudfront.codeproject.com/articles/5357417/architecture_v1.10.png)

### Notes on application architecture

* Application, Controls, and Common parts are UI & application type dependant.
* Logger Providers are Logging Framework specific.
* Controls and Common parts are application type specific.
* Logger Providers, Random Logging Service, and Controls are all independent of each other.

### Solution Architecture

Both **VB** and **C#** solutions are included and have identical layouts. The only difference is the **VB** version has **VB** at the end of the project name.

![Article image](https://cloudfront.codeproject.com/articles/5357417/solution_layout_v1.20.png)

### Notes

* The application project names are made up of 3 parts: **ApplicationType + Logger + Implementation**
  1. Application Type: **Avalonia**, **WinForms**, **Wpf**
  2. Logger: Logger (Default .NET Implementation) or Serilog
  3. Implementation: DI = Dependency Injection; NoDI = Manual / No Dependency Injection
* For supporting Projects, the Name Suffix identifies the project type:
  1. **.Core** for common code
  2. **.Avalonia**, **.WinForms**, **.Wpf** for application-specific types

## How Does Logging Work?

Before we dig into the solutions, let us quickly look at how the .NET Logging Framework works.

There are three parts:

1. Logger
2. Registering Loggers
3. Processing Log Entries

We will be using the [Microsoft Logger Framework](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging). This will allow us to not only capture the application's logging but all **.NET Framework** and 3rd-party library logging.

The implementation in this article will be using a singleton `DataStore` for storage, Custom Logger, and Logging Provider. There is also a `Configuration` class for custom options, like custom colorization.

This is just a brief summation and look at the internal code. If you require more information, please see the links provided above and in the [References](#references) section at the end of this article.

### Logger Internals

Loggers are made up of four parts:

1. **Logger** - logging implementation
2. **LoggingProvider** - generates the Logger instance
3. **Processor** / **Storage** - where the logger outputs the logging to
4. **Configuration** (optional) - parameters for generating output

![Article image](https://cloudfront.codeproject.com/articles/5357417/logging_provider_flow_394.png)

Every time the `LoggingFactory` creates a `Logger` instance, the `LoggingFactory` will cycle through all of the registered `Logger Providers` and generate internal `Logger` instances for the returned concrete `Logger`. All calls to the `Log` method on the concrete `Logger` will cycle through all of the internal `Logger` instances.

![Article image](https://cloudfront.codeproject.com/articles/5357417/logger_factory_flow_600.png)

To understand this better, let's look at the code in the **.NET Framework** `LoggerFactory` class that creates the `Logger` instance that we use:

```csharp
public ILogger CreateLogger(string categoryName)
{
    if (CheckDisposed())
    {
        throw new ObjectDisposedException(nameof(LoggerFactory));
    }

    lock (_sync)
    {
        if (!_loggers.TryGetValue(categoryName, out Logger? logger))
        {
            logger = new Logger(CreateLoggers(categoryName));

            (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);

            _loggers[categoryName] = logger;
        }

        return logger;
    }
}

private LoggerInformation[] CreateLoggers(string categoryName)
{
    var loggers = new LoggerInformation[_providerRegistrations.Count];
    for (int i = 0; i < _providerRegistrations.Count; i++)
    {
        loggers[i] = new LoggerInformation(_providerRegistrations[i].Provider,
                     categoryName);
    }
    return loggers;
}

internal readonly struct LoggerInformation
{
    public LoggerInformation(ILoggerProvider provider, string category) : this()
    {
        ProviderType = provider.GetType();
        Logger = provider.CreateLogger(category);
        Category = category;
        ExternalScope = provider is ISupportExternalScope;
    }

    public ILogger Logger { get; }

    public string Category { get; }

    public Type ProviderType { get; }

    public bool ExternalScope { get; }
}
```

Here, we see everything being wired up, including the `LoggerProvier` generating the internal `Loggers` via the `CreateLoggers` method.

Then, every time we Log an entry via our `Logger`, the information is passed to every internal `Logger`.

Here is the concrete **.NET Framework** internal `Logger` that is substantiated by the `LoggerFactory`. We will look specifically at the `Log` method:

```csharp
  internal sealed class Logger : ILogger
  {
       public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                               Exception? exception, Func<TState, Exception?,
                               string> formatter)
      {
          MessageLogger[]? loggers = MessageLoggers;
          if (loggers == null)
          {
              return;
          }

          List<Exception>? exceptions = null;
          for (int i = 0; i < loggers.Length; i++)
          {
              ref readonly MessageLogger loggerInfo = ref loggers[i];
              if (!loggerInfo.IsEnabled(logLevel))
              {
                  continue;
              }

              LoggerLog(logLevel, eventId, loggerInfo.Logger, exception,
                        formatter, ref exceptions, state);
          }

          if (exceptions != null && exceptions.Count > 0)
          {
              ThrowLoggingError(exceptions);
          }

          static void LoggerLog(LogLevel logLevel, EventId eventId, ILogger logger,
                                Exception? exception, Func<TState, Exception?, string> formatter,
                                ref List<Exception>? exceptions, TState state)
```

... trimmed ...

To see the updated `RandomLoggingService` in action, download the code and run the `WpfLoggingAttrDI` project in the *MSlogger/Attribute* solution folder.

---

Merged additions (from full capture):

## Custom Loggers

### Shared Logging Data

#### Storage - LogDataStore and LogModel classes

```csharp
public interface ILogDataStore
{
    ObservableCollection<LogModel> Entries { get; }
    void AddEntry(LogModel logModel);
}

public class LogDataStore : ILogDataStore
{
    private static readonly SemaphoreSlim _semaphore = new(initialCount: 1);

    public ObservableCollection<LogModel> Entries { get; } = new();

    public virtual void AddEntry(LogModel logModel)
    {
        _semaphore.Wait();
        try
        {
            Entries.Add(logModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

```vbnet
Public Interface ILogDataStore
  ReadOnly Property Entries As ObservableCollection(Of LogModel)
  Sub AddEntry(logModel As LogModel)
End Interface

Public Class LogDataStore : Implements ILogDataStore
  Private Shared ReadOnly _semaphore As New SemaphoreSlim(1)
  Public ReadOnly Property Entries As New ObservableCollection(Of LogModel) _
    Implements ILogDataStore.Entries
  Public Sub AddEntry(logModel As LogModel) Implements ILogDataStore.AddEntry
    _semaphore.Wait()
    Try
      Entries.Add(logModel)
    Finally
      _semaphore.Release()
    End Try
  End Sub
End Class
```

```csharp
public class LogModel
{
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public object? State { get; set; }
    public string? Exception { get; set; }
    public LogEntryColor? Color { get; set; }
}
```

```vbnet
Public Class LogModel
  Public Property Timestamp As DateTime
  Public Property LogLevel As LogLevel
  Public Property EventId As EventId
  Public Property State As Object
  Public Property Exception As String
  Public Property Color As LogEntryColor
End Class
```

#### Configuration - DataStoreLoggerConfiguration class and LogEntryColor class

```csharp
public class DataStoreLoggerConfiguration
{
    public EventId EventId { get; set; }
    public Dictionary<LogLevel, LogEntryColor> Colors { get; } = new()
    {
        [LogLevel.Trace] = new() { Foreground = Color.DarkGray },
        [LogLevel.Debug] = new() { Foreground = Color.Gray },
        [LogLevel.Information] = new(),
        [LogLevel.Warning] = new() { Foreground = Color.Orange },
        [LogLevel.Error] = new() { Foreground = Color.White, Background = Color.OrangeRed },
        [LogLevel.Critical] = new() { Foreground = Color.White, Background = Color.Red },
        [LogLevel.None] = new(),
    };
}
```

```vbnet
Public Class DataStoreLoggerConfiguration
  Public Property EventId As EventId
  Public ReadOnly Property Colors As New Dictionary(Of LogLevel, LogEntryColor) From {
    { LogLevel.Trace, New LogEntryColor With {.Foreground = Color.DarkGray} },
    { LogLevel.Debug, New LogEntryColor With {.Foreground = Color.Gray} },
    { LogLevel.Information, New LogEntryColor() },
    { LogLevel.Warning, New LogEntryColor With {.Foreground = Color.Orange} },
    { LogLevel.[Error], New LogEntryColor With {.Foreground = Color.White, .Background = Color.OrangeRed} },
    { LogLevel.Critical, New LogEntryColor With {.Foreground = Color.White, .Background = Color.Red} },
    { LogLevel.None, New LogEntryColor() }
  }
End Class
```

```csharp
public class LogEntryColor
{
    public Color Foreground { get; set; } = Color.Black;
    public Color Background { get; set; } = Color.Transparent;
}
```

```vbnet
Public Class LogEntryColor
  Public Property Foreground As Color = Color.Black
  Public Property Background As Color = Color.Transparent
End Class
```

### Custom Microsoft Logger Implementation

#### Registration - ServicesExtension class

```csharp
public static class ServicesExtension
{
    public static ILoggingBuilder AddDefaultDataStoreLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, DataStoreLoggerProvider>());
        return builder;
    }

    public static ILoggingBuilder AddDefaultDataStoreLogger(
        this ILoggingBuilder builder,
        Action<DataStoreLoggerConfiguration> configure)
    {
        builder.AddDefaultDataStoreLogger();
        builder.Services.Configure(configure);
        return builder;
    }
}
```

```vbnet
Public Module ServicesExtension
  <Extension>
  Public Function AddDefaultDataStoreLogger(builder As ILoggingBuilder) As ILoggingBuilder
    builder.Services.TryAddEnumerable(
      ServiceDescriptor.Singleton(Of ILoggerProvider, DataStoreLoggerProvider))
    Return builder
  End Function

  <Extension>
  Public Function AddDefaultDataStoreLogger( _
    builder As ILoggingBuilder,
    configure As Action(Of DataStoreLoggerConfiguration)) As ILoggingBuilder
    builder.AddDefaultDataStoreLogger()
    builder.Services.Configure(configure)
    Return builder
  End Function
End Module
```

#### Dependency Injection / App wiring

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.AddLogViewer();
builder.Logging.AddDefaultDataStoreLogger();

_host = builder.Build();
```

```vbnet
Dim builder As HostApplicationBuilder = Host.CreateApplicationBuilder()

builder.AddLogViewer()
builder.Logging.AddDefaultDataStoreLogger()

_host = builder.Build()
```

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.AddLogViewer();
builder.Logging.AddDefaultDataStoreLogger(options =>
{
    options.Colors[LogLevel.Trace] = new()
    {
        Foreground = Color.White,
        Background = Color.DarkGray
    };
    options.Colors[LogLevel.Debug] = new()
    {
        Foreground = Color.White,
        Background = Color.Gray
    };
    options.Colors[LogLevel.Information] = new()
    {
        Foreground = Color.White,
        Background = Color.DodgerBlue
    };
    options.Colors[LogLevel.Warning] = new()
    {
        Foreground = Color.White,
        Background = Color.Orchid
    };
});

_host = builder.Build();
```

```vbnet
Dim builder As HostApplicationBuilder = Host.CreateApplicationBuilder()

builder.AddLogViewer()
builder.Logging.AddDefaultDataStoreLogger(
  Sub(options)
    options.Colors(LogLevel.Trace) = New LogEntryColor() With { .Foreground = Color.White, .Background = Color.DarkGray }
    options.Colors(LogLevel.Debug) = New LogEntryColor() With { .Foreground = Color.White, .Background = Color.Gray }
    options.Colors(LogLevel.Information) = New LogEntryColor() With { .Foreground = Color.White, .Background = Color.DodgerBlue }
    options.Colors(LogLevel.Warning) = New LogEntryColor() With { .Foreground = Color.White, .Background = Color.Orchid }
  End Sub)

_host = builder.Build()
```

### Generating Sample Log Messages

#### Background Service - RandomLoggingService Class

```csharp
public class RandomLoggingService : BackgroundService
{
    private readonly ILogger _logger;
    public RandomLoggingService(ILogger<RandomLoggingService> logger) => _logger = logger;
}
```

```vbnet
Public Class RandomLoggingService : Inherits BackgroundService
  Private ReadOnly _logger As ILogger
  Public Sub New(logger As ILogger(Of RandomLoggingService))
    _logger = logger
  End Sub
End Class
```

#### Getting an ILogger without DI (manual)

```csharp
ILogger<class_name> logger = _host.Services.GetRequiredService<ILogger<class_name>>();
```

```vbnet
Dim logger As ILogger(Of class_name) = _host.Services.GetRequiredService(Of ILogger(Of class_name))
```

```csharp
Logger<class_name> logger = new Logger<class_name>(LoggingHelper.Factory);
```

```vbnet
Dim logger As Logger(Of class_name) = New Logger(Of class_name)(LoggingHelper.Factory)
```

```csharp
RandomLoggingService service = new(new Logger<RandomLoggingService>(LoggingHelper.Factory));
```

```vbnet
Dim service As RandomLoggingService = New RandomLoggingService(New Logger(Of RandomLoggingService)(LoggingHelper.Factory))
```

---

### Custom Serilog Logger Implementation

Serilog sinks (loggers) have a different implementation to the Microsoft Logger. To work within Microsoft.Extensions.Logging, Serilog integrates via a provider and we add a custom sink that writes into our data store.

#### Logger — DataStoreLoggerSink class

```csharp
public class DataStoreLoggerSink : ILogEventSink
{
    protected readonly Func<ILogDataStore> _dataStoreProvider;
    private readonly IFormatProvider? _formatProvider;
    private readonly Func<DataStoreLoggerConfiguration>? _getCurrentConfig;

    public DataStoreLoggerSink(Func<ILogDataStore> dataStoreProvider,
                               Func<DataStoreLoggerConfiguration>? getCurrentConfig = null,
                               IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
        _dataStoreProvider = dataStoreProvider;
        _getCurrentConfig = getCurrentConfig;
    }

    public void Emit(LogEvent logEvent)
    {
        LogLevel logLevel = logEvent.Level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information
        };

        DataStoreLoggerConfiguration config =
            _getCurrentConfig?.Invoke() ?? new DataStoreLoggerConfiguration();

        EventId eventId = EventIdFactory(logEvent);
        if (eventId.Id == 0 && config.EventId != 0)
            eventId = config.EventId;

        string message = logEvent.RenderMessage(_formatProvider);
        string exception = logEvent.Exception?.Message ?? (logEvent.Level >= LogEventLevel.Error ? message : string.Empty);
        LogEntryColor color = config.Colors[logLevel];

        AddLogEntry(logLevel, eventId, message, exception, color);
    }

    protected virtual void AddLogEntry(LogLevel logLevel, EventId eventId, string message, string exception, LogEntryColor color)
    {
        ILogDataStore? dataStore = _dataStoreProvider.Invoke();
        if (dataStore == null)
            return; // app is shutting down

        dataStore.AddEntry(new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId,
            State = message,
            Exception = exception,
            Color = color
        });
    }

    private static EventId EventIdFactory(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? src))
            return new();

        int? id = null;
        string? eventName = null;
        StructureValue? value = src as StructureValue;

        LogEventProperty? idProperty = value!.Properties.FirstOrDefault(x => x.Name.Equals("Id"));
        if (idProperty is not null)
            id = int.Parse(idProperty.Value.ToString());

        LogEventProperty? nameProperty = value.Properties.FirstOrDefault(x => x.Name.Equals("Name"));
        if (nameProperty is not null)
            eventName = nameProperty.Value.ToString().Trim('"');

        return new EventId(id ?? 0, eventName ?? string.Empty);
    }
}
```

```vbnet
Public Class DataStoreLoggerSink : Implements ILogEventSink

  Protected ReadOnly _dataStoreProvider As Func(Of ILogDataStore)
  Private ReadOnly _formatProvider As IFormatProvider
  Private ReadOnly _getCurrentConfig As Func(Of DataStoreLoggerConfiguration)

  Public Sub New(dataStoreProvider As Func(Of ILogDataStore), _
         Optional getCurrentConfig As Func(Of DataStoreLoggerConfiguration) = Nothing, _
         Optional formatProvider As IFormatProvider = Nothing)
    _dataStoreProvider = dataStoreProvider
    _formatProvider = formatProvider
    _getCurrentConfig = getCurrentConfig
  End Sub

  Public Sub Emit(logEvent As LogEvent) Implements ILogEventSink.Emit
    Dim logLevel As LogLevel
    Select Case logEvent.Level
      Case LogEventLevel.Verbose : logLevel = LogLevel.Trace
      Case LogEventLevel.Debug : logLevel = LogLevel.Debug
      Case LogEventLevel.Warning : logLevel = LogLevel.Warning
      Case LogEventLevel.Error : logLevel = LogLevel.Error
      Case LogEventLevel.Fatal : logLevel = LogLevel.Critical
      Case Else : logLevel = LogLevel.Information
    End Select

    Dim config As DataStoreLoggerConfiguration = If(_getCurrentConfig Is Nothing, _
                            New DataStoreLoggerConfiguration(), _
                            _getCurrentConfig.Invoke())

    Dim eventId As EventId = EventIdFactory(logEvent)
    If eventId.Id = 0 AndAlso config.EventId <> 0 Then
      eventId = config.EventId
    End If

    Dim message As String = logEvent.RenderMessage(_formatProvider)
    Dim exception As String = If(logEvent.Exception Is Nothing, _
                  If(logEvent.Level >= LogEventLevel.Error, message, String.Empty), _
                  logEvent.Exception.Message)
    Dim color As LogEntryColor = config.Colors(logLevel)
    AddLogEntry(logLevel, eventId, message, exception, color)
  End Sub

  Protected Overridable Sub AddLogEntry(logLevel As LogLevel, eventId As EventId, _
                                        message As String, exception As String, _
                                        color As LogEntryColor)
    Dim dataStore As ILogDataStore = _dataStoreProvider.Invoke()
    If dataStore Is Nothing Then Return
    dataStore.AddEntry(New LogModel() With {
        .Timestamp = DateTime.UtcNow, _
        .LogLevel = logLevel, _
        .EventId = eventId, _
        .State = message, _
        .Exception = exception, _
        .Color = color })
  End Sub

  Private Shared Function EventIdFactory(logEvent As LogEvent) As EventId
    Dim src As LogEventPropertyValue
    If Not logEvent.Properties.TryGetValue("EventId", src) Then Return New EventId()
    Dim id As Integer = Nothing
    Dim eventName As String = Nothing
    Dim value As StructureValue = DirectCast(src, StructureValue)
    Dim idProperty As LogEventProperty = value.Properties.FirstOrDefault(Function(x) x.Name.Equals("Id"))
    If idProperty IsNot Nothing Then id = Integer.Parse(idProperty.Value.ToString())
    Dim nameProperty As LogEventProperty = value.Properties.FirstOrDefault(Function(x) x.Name.Equals("Name"))
    If nameProperty IsNot Nothing Then eventName = nameProperty.Value.ToString().Trim(""""c)
    Return New EventId(If(id = Nothing, 0, id), If(String.IsNullOrWhiteSpace(eventName), String.Empty, eventName))
  End Function
End Class
```

#### Configuring the custom sink — DataStoreLoggerSinkExtensions

```csharp
public static class DataStoreLoggerSinkExtensions
{
    public static LoggerConfiguration DataStoreLoggerSink(
        this LoggerSinkConfiguration loggerConfiguration,
        Func<ILogDataStore> dataStoreProvider,
        Action<DataStoreLoggerConfiguration>? configuration = null,
        IFormatProvider formatProvider = null!)
        => loggerConfiguration.Sink(
            new DataStoreLoggerSink(
                dataStoreProvider,
                GetConfig(configuration),
                formatProvider));

    private static Func<DataStoreLoggerConfiguration> GetConfig(Action<DataStoreLoggerConfiguration>? configuration)
    {
        DataStoreLoggerConfiguration data = new();
        configuration?.Invoke(data);
        return () => data;
    }
}
```

```vbnet
Public Module DataStoreLoggerSinkExtensions
  <Extension>
  Public Function DataStoreLoggerSink(loggerConfiguration As LoggerSinkConfiguration, _
                    dataStoreProvider As Func(Of ILogDataStore), _
                    Optional configuration As Action(Of DataStoreLoggerConfiguration) = Nothing, _
                    Optional formatProvider As IFormatProvider = Nothing) _
                    As LoggerConfiguration
    Return loggerConfiguration.Sink(New DataStoreLoggerSink(dataStoreProvider, GetConfig(configuration), formatProvider))
  End Function

  Private Function GetConfig(configuration As Action(Of DataStoreLoggerConfiguration)) _
    As Func(Of DataStoreLoggerConfiguration)
    Dim data As New DataStoreLoggerConfiguration()
    If configuration IsNot Nothing Then configuration.Invoke(data)
    Return Function() data
  End Function
End Module
```

#### appsettings.json excerpt for Serilog

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System.Net.Http.HttpClient": "Information"
    }
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "LevelSwitches": { "controlSwitch": "Information" },
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Information" }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {EventId.Name} | {Message:lj} {NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "c:\\WIP\\LogData\\log-.txt",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "outputTemplate": "{Timestamp:G} {Message}{NewLine:1}{Exception:1}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "c:\\WIP\\LogData\\log-.json",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "formatter": "Serilog.Formatting.Json.JsonFormatter"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId" ]
  }
}
```

#### Dependency Injection wiring (Serilog)

Default colors:

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.AddLogViewer();
IServiceCollection services = builder.Services;

services.AddLogging(cfg =>
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.DataStoreLoggerSink(
            dataStoreProvider: () => _host!.Services.TryGetService<ILogDataStore>()!)
        .CreateLogger();

    cfg.ClearProviders().AddSerilog(Log.Logger);
});

_host = builder.Build();
```

Custom colors:

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.AddLogViewer();
IServiceCollection services = builder.Services;

services.AddLogging(cfg =>
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.DataStoreLoggerSink(
            () => _host!.Services.TryGetService<ILogDataStore>()!, options =>
            {
                options.Colors[LogLevel.Trace] = new() { Foreground = Color.White, Background = Color.DarkGray };
                options.Colors[LogLevel.Debug] = new() { Foreground = Color.White, Background = Color.Gray };
                options.Colors[LogLevel.Information] = new() { Foreground = Color.White, Background = Color.DodgerBlue };
                options.Colors[LogLevel.Warning] = new() { Foreground = Color.White, Background = Color.Orchid };
            })
        .CreateLogger();

    cfg.ClearProviders().AddSerilog(Log.Logger);
});

_host = builder.Build();
```

VB equivalents are available in the full capture if needed.

Manual (no DI) helper pattern:

```csharp
public static class LoggingHelper
{
    static LoggingHelper()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Initialize().Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.DataStoreLoggerSink(() => MainControlsDataStore.DataStore)
            .CreateLogger();

        Factory = LoggerFactory.Create(loggingBuilder =>
            loggingBuilder.AddSerilog(Log.Logger));
    }

    public static ILoggerFactory Factory { get; }

    public static void CloseAndFlush() => Log.CloseAndFlush();
}
```

With custom colors:

```csharp
public static class LoggingHelper
{
    static LoggingHelper()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Initialize().Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.DataStoreLoggerSink(
                () => MainControlsDataStore.DataStore,
                options =>
                {
                    options.Colors[LogLevel.Trace] = new() { Foreground = Color.White, Background = Color.DarkGray };
                    options.Colors[LogLevel.Debug] = new() { Foreground = Color.White, Background = Color.Gray };
                    options.Colors[LogLevel.Information] = new() { Foreground = Color.White, Background = Color.DodgerBlue };
                    options.Colors[LogLevel.Warning] = new() { Foreground = Color.White, Background = Color.Orchid };
                })
            .CreateLogger();

        Factory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger));
    }

    public static ILoggerFactory Factory { get; }
    public static void CloseAndFlush() => Log.CloseAndFlush();
}
```

Illustrations:

![Serilog DI](https://cloudfront.codeproject.com/articles/5357417/serilog_-_di_507.png)

![Serilog No DI](https://cloudfront.codeproject.com/articles/5357417/serilog_-_nodi_516.png)

---

### Custom NLog Target Logger Implementation

NLog targets (loggers) differ from Microsoft’s ILogger. NLog integrates with Microsoft.Extensions.Logging via its provider. We implement a custom target that writes to the shared data store.

#### Logger — DataStoreLoggerTarget class

```csharp
[Target("DataStoreLogger")]
public class DataStoreLoggerTarget : TargetWithLayout
{
    private ILogDataStore? _dataStore;
    private DataStoreLoggerConfiguration? _config;

    protected override void InitializeTarget()
    {
        IServiceProvider serviceProvider = ResolveService<IServiceProvider>();
        _dataStore = serviceProvider.GetRequiredService<ILogDataStore>();
        IOptionsMonitor<DataStoreLoggerConfiguration>? options =
            serviceProvider.GetService<IOptionsMonitor<DataStoreLoggerConfiguration>>();
        _config = options?.CurrentValue ?? new DataStoreLoggerConfiguration();
        base.InitializeTarget();
    }

    protected override void Write(LogEventInfo logEvent)
    {
        // cast NLog level to Microsoft LogLevel
        LogLevel logLevel = (LogLevel)Enum.ToObject(typeof(LogLevel), logEvent.Level.Ordinal);
        string message = RenderLogEvent(Layout, logEvent);
        EventId eventId = (EventId)logEvent.Properties["EventId"];

        _dataStore?.AddEntry(new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId.Id == 0 && (_config?.EventId.Id ?? 0) != 0 ? _config!.EventId : eventId,
            State = message,
            Exception = logEvent.Exception?.Message ?? (logLevel == LogLevel.Error ? message : ""),
            Color = _config!.Colors[logLevel],
        });
    }
}
```

```vbnet
<Target("DataStoreLogger")>
Public Class DataStoreLoggerTarget : Inherits TargetWithLayout
  Private _dataStore As ILogDataStore
  Private _config As DataStoreLoggerConfiguration

  Protected Overrides Sub InitializeTarget()
    Dim sp As IServiceProvider = ResolveService(Of IServiceProvider)()
    _dataStore = sp.GetRequiredService(Of ILogDataStore)
    Dim options = sp.GetService(Of IOptionsMonitor(Of DataStoreLoggerConfiguration))
    _config = If(options Is Nothing, New DataStoreLoggerConfiguration(), options.CurrentValue)
    MyBase.InitializeTarget()
  End Sub

  Protected Overrides Sub Write(logEvent As LogEventInfo)
    Dim logLevel As LogLevel = CType([Enum].ToObject(GetType(LogLevel), logEvent.Level.Ordinal), LogLevel)
    Dim message As String = RenderLogEvent(Layout, logEvent)
    Dim eventId As EventId = logEvent.Properties("EventId")
    If eventId.Id = 0 AndAlso _config.EventId.Id <> 0 Then eventId = _config.EventId
    _dataStore.AddEntry(New LogModel() With {
      .Timestamp = Date.UtcNow,
      .LogLevel = logLevel,
      .EventId = eventId,
      .State = message,
      .Exception = If(logEvent.Exception Is Nothing, If(logLevel = LogLevel.Error, message, ""), logEvent.Exception.Message),
      .Color = _config.Colors(logLevel) })
  End Sub
End Class
```

#### Configuring the custom target — ServicesExtension

```csharp
public static class ServicesExtension
{
    public static ILoggingBuilder AddNLogTargets(this ILoggingBuilder builder, IConfiguration config)
    {
        LogManager.Setup()
            .SetupExtensions(ext => ext.RegisterTarget<DataStoreLoggerTarget>("DataStoreLogger"));

        builder.ClearProviders()
               .SetMinimumLevel(LogLevel.Trace)
               .AddNLog(config, new NLogProviderOptions
               {
                   IgnoreEmptyEventId = false,
                   CaptureEventId = EventIdCaptureType.Legacy
               });
        return builder;
    }

    public static ILoggingBuilder AddNLogTargets(this ILoggingBuilder builder, IConfiguration config, Action<DataStoreLoggerConfiguration> configure)
    {
        builder.AddNLogTargets(config);
        builder.Services.Configure(configure);
        return builder;
    }
}
```

```vbnet
Public Module ServicesExtension
  <Extension>
  Public Function AddNLogTargets(builder As ILoggingBuilder, config As IConfiguration) As ILoggingBuilder
    LogManager.Setup().SetupExtensions(Sub(ext) ext.RegisterTarget(Of DataStoreLoggerTarget)("DataStoreLogger"))
    builder.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddNLog(config, New NLogProviderOptions With {
      .IgnoreEmptyEventId = False,
      .CaptureEventId = EventIdCaptureType.Legacy
    })
    Return builder
  End Function

  <Extension>
  Public Function AddNLogTargets(builder As ILoggingBuilder, config As IConfiguration, configure As Action(Of DataStoreLoggerConfiguration)) As ILoggingBuilder
    builder.AddNLogTargets(config)
    builder.Services.Configure(configure)
    Return builder
  End Function
End Module
```

#### appsettings.json excerpt for NLog

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System.Net.Http.HttpClient": "Information"
    }
  },
  "NLog": {
    "throwConfigExceptions": true,
    "targets": {
      "async": true,
      "logconsole": {
        "type": "Console",
        "layout": "${longdate}|${level}|${message} | ${all-event-properties} ${exception:format=tostring}"
      },
      "DataStoreLogger": {
        "type": "DataStoreLogger",
        "layout": "${message}"
      }
    },
    "rules": [
      { "logger": "*", "minLevel": "Info", "writeTo": "logconsole, DataStoreLogger" }
    ]
  }
}
```

#### Dependency Injection (NLog)

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.AddLogViewer();
builder.Logging.AddNLogTargets(builder.Configuration);
_host = builder.Build();
```

With custom colors:

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.AddLogViewer();
builder.Logging.AddNLogTargets(builder.Configuration, options =>
{
    options.Colors[LogLevel.Trace] = new() { Foreground = Color.White, Background = Color.DarkGray };
    options.Colors[LogLevel.Debug] = new() { Foreground = Color.White, Background = Color.Gray };
    options.Colors[LogLevel.Information] = new() { Foreground = Color.White, Background = Color.DodgerBlue };
    options.Colors[LogLevel.Warning] = new() { Foreground = Color.White, Background = Color.Orchid };
});
_host = builder.Build();
```

#### Manually (without DI) — NLog

```csharp
public static class ServicesExtension
{
    public static ILoggingBuilder AddNLogTargetsNoDI(this ILoggingBuilder builder, IConfiguration config)
    {
        builder.Services.AddSingleton(MainControlsDataStore.DataStore);
        builder.AddNLogTargets(config);
        return builder;
    }

    public static ILoggingBuilder AddNLogTargetsNoDI(this ILoggingBuilder builder, IConfiguration config, Action<DataStoreLoggerConfiguration> configure)
    {
        builder.AddNLogTargetsNoDI(config);
        builder.Services.Configure(configure);
        return builder;
    }
}
```

Helper factory:

```csharp
public static class LoggingHelper
{
    static LoggingHelper()
    {
        string value = AppSettings<string>.Current("Logging:LogLevel", "Default") ?? "Information";
        Enum.TryParse(value, out LogLevel logLevel);
        IConfigurationRoot configuration = new ConfigurationBuilder().Initialize().Build();
        Factory = LoggerFactory.Create(builder => builder
            .AddNLogTargetsNoDI(configuration)
            .SetMinimumLevel(logLevel));
    }

    public static ILoggerFactory Factory { get; }
}
```

With custom colors helper:

```csharp
public static class LoggingHelper
{
    static LoggingHelper()
    {
        string value = AppSettings<string>.Current("Logging:LogLevel", "Default") ?? "Information";
        Enum.TryParse(value, out LogLevel logLevel);
        IConfigurationRoot configuration = new ConfigurationBuilder().Initialize().Build();
        Factory = LoggerFactory.Create(builder => builder
            .AddNLogTargetsNoDI(configuration, options =>
            {
                options.Colors[LogLevel.Trace] = new() { Foreground = Color.White, Background = Color.DarkGray };
                options.Colors[LogLevel.Debug] = new() { Foreground = Color.White, Background = Color.Gray };
                options.Colors[LogLevel.Information] = new() { Foreground = Color.White, Background = Color.DodgerBlue };
                options.Colors[LogLevel.Warning] = new() { Foreground = Color.White, Background = Color.Orchid };
            })
            .SetMinimumLevel(logLevel));
    }

    public static ILoggerFactory Factory { get; }
}
```

Illustrations:

![NLog DI](https://cloudfront.codeproject.com/articles/5357417/nlog_di_640.png)

![NLog No DI](https://cloudfront.codeproject.com/articles/5357417/nlog_nodi_640.png)

---

### Custom Apache Log4Net Appender Logger Implementation

Log4Net supports .NET via .NET Standard 1.3, but lacks some pieces out-of-the-box: native DI for custom appenders and EventId support. We extend an existing adapter project and add missing parts.

#### Adding EventId support to the adapter

Interface and wrapper:

```csharp
public interface IEventIDLog : ILog { void Log(EventId eventId, LoggingEvent loggingEvent); }

public class EventIDLogImpl : LogImpl, IEventIDLog
{
    public EventIDLogImpl(log4net.Core.ILogger logger) : base(logger) { }
    public void Log(EventId eventId, LoggingEvent loggingEvent)
    {
        if (!(eventId.Id == 0 && string.IsNullOrWhiteSpace(eventId.Name)))
            loggingEvent.Properties[nameof(EventId)] = eventId;
        Logger.Log(loggingEvent);
    }
}
```

Update the adapter’s Log4NetLogger to use IEventIDLog and pass EventId when logging (see full capture for full diff).

#### DI for custom appenders — ServiceAppenderSkeleton

```csharp
internal interface IAppenderServiceProvider { IServiceProvider ServiceProvider { set; } }

public abstract class ServiceAppenderSkeleton : AppenderSkeleton, IAppenderServiceProvider, IDisposable
{
    private IServiceProvider? _serviceProvider;
    IServiceProvider IAppenderServiceProvider.ServiceProvider { set => _serviceProvider = value; }
    protected T? ResolveService<T>() where T : class => _serviceProvider?.GetService<T>();
    public void Dispose() => _serviceProvider = null;
}
```

Inject services from provider into appenders:

```csharp
public class Log4NetProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IServiceProvider _serviceProvider;
    public Log4NetProvider(Log4NetProviderOptions options, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // ... configure repository (omitted)
        InjectServices();
    }

    private void InjectServices()
    {
        if (Repository is null) return;
        IEnumerable<IAppenderServiceProvider> adapters = Repository.GetAppenders().OfType<IAppenderServiceProvider>();
        foreach (var adapter in adapters) adapter.ServiceProvider = _serviceProvider;
    }
}
```

#### Logger — DataStoreLoggerAppender class

```csharp
public class DataStoreLoggerAppender : ServiceAppenderSkeleton
{
    private ILogDataStore? _dataStore;
    private DataStoreLoggerConfiguration? _options;
    private IServiceProvider? _serviceProvider;

    protected override void Append(LoggingEvent loggingEvent)
    {
        if (_serviceProvider is null) Initialize();

        LogLevel logLevel = loggingEvent.Level.Value switch
        {
            int.MaxValue => LogLevel.None,
            120000 => LogLevel.Debug,
            90000 => LogLevel.Critical,
            70000 => LogLevel.Error,
            60000 => LogLevel.Warning,
            20000 => LogLevel.Trace,
            _ => LogLevel.Information
        };

        var config = _options ?? new DataStoreLoggerConfiguration();
        EventId? eventId = (EventId?)loggingEvent.LookupProperty(nameof(EventId));
        eventId = eventId is null && config.EventId.Id != 0 ? config.EventId : eventId;

        string message = loggingEvent.RenderedMessage ?? string.Empty;
        string exceptionMessage = loggingEvent.GetExceptionString();

        _dataStore!.AddEntry(new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId ?? new(),
            State = message,
            Exception = exceptionMessage,
            Color = config.Colors[logLevel],
        });
    }

    private void Initialize()
    {
        _serviceProvider = ResolveService<IServiceProvider>();
        _dataStore = _serviceProvider!.GetRequiredService<ILogDataStore>();
        _options = _serviceProvider.GetService<DataStoreLoggerConfiguration>();
    }
}
```

VB equivalent available in full capture.

#### ServicesExtension — AddLog4Net

```csharp
public static class ServicesExtension
{
    public static ILoggingBuilder AddLog4Net(this ILoggingBuilder builder, IConfiguration config)
        => builder.ClearProviders().AddLog4Net(config.GetLog4NetConfiguration());

    public static ILoggingBuilder AddLog4Net(this ILoggingBuilder builder, IConfiguration config, Action<DataStoreLoggerConfiguration> configure)
        => builder.AddLog4Net(config).Services.Configure(configure);

    public static Log4NetProviderOptions? GetLog4NetConfiguration(this IConfiguration configuration)
        => configuration.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();
}
```

#### log4net.config (XML) and appsettings overrides

```xml
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <appender name="DebugAppender" type="log4net.Appender.DebugAppender">
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>
  <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
    <threshold value="ALL" />
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>
  <appender name="DataStoreLogger" type="Log4Net.Appender.LogView.Core.DataStoreLoggerAppender">
    <threshold value="ALL" />
  </appender>
  <root>
    <Level value="ALL" />
    <appender-ref ref="DebugAppender" />
    <appender-ref ref="ConsoleAppender" />
    <appender-ref ref="DataStoreLogger" />
  </root>
  </log4net>
```

appsettings.Production.json override example:

```json
{
  "Log4NetCore": {
    "Name": "Log4NetLogViewer_Prod",
    "LoggerRepository": "LogViewerRepository",
    "OverrideCriticalLevelWith": "Critical",
    "Watch": false,
    "UseWebOrAppConfig": false,
    "PropertyOverrides": [
      {
        "XPath": "/log4net/appender[@name='ConsoleAppender']/layout/conversionPattern",
        "Attributes": { "Value": "%date [%thread] %-5level | %logger | %message%newline" }
      },
      {
        "XPath": "/log4net/appender[@name='ConsoleAppender']/threshold",
        "Attributes": { "Value": "Warn" }
      },
      {
        "XPath": "/log4net/appender[@name='DataStoreLogger']/threshold",
        "Attributes": { "Value": "Warn" }
      }
    ]
  }
}
```

#### Dependency Injection (Log4Net)

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.AddLogViewer();
builder.Logging.AddLog4Net(builder.Configuration);
_host = builder.Build();
```

Custom colors variant provided in the full capture.

#### Manually (without DI) — Log4Net

```csharp
public static class ServicesExtension
{
    public static ILoggingBuilder AddLog4NetNoDI(this ILoggingBuilder builder, IConfiguration config)
    {
        builder.Services.AddSingleton(MainControlsDataStore.DataStore);
        builder.AddLog4Net(config);
        return builder;
    }

    public static ILoggingBuilder AddLog4NetNoDI(this ILoggingBuilder builder, IConfiguration config, Action<DataStoreLoggerConfiguration> configure)
    {
        builder.AddLog4NetNoDI(config);
        builder.Services.Configure(configure);
        return builder;
    }
}
```

Helper factory:

```csharp
public static class LoggingHelper
{
    static LoggingHelper()
    {
        string value = AppSettings<string>.Current("Logging:LogLevel", "Default") ?? "Information";
        Enum.TryParse(value, out LogLevel logLevel);
        IConfigurationRoot configuration = new ConfigurationBuilder().Initialize().Build();
        Factory = LoggerFactory.Create(builder => builder
            .AddLog4NetNoDI(configuration)
            .SetMinimumLevel(logLevel));
    }
    public static ILoggerFactory Factory { get; }
}
```

Illustrations:

![Log4Net DI](https://cloudfront.codeproject.com/articles/5357417/log4net_di_640.png)

![Log4Net No DI](https://cloudfront.codeproject.com/articles/5357417/log4net_nodi_638.png)

---

### Processing Log Entries

We have our `LogDataStore` class storing all the Log Entries from all libraries and the application based on the minimal `LogLevel` retrieved from the appsettings*.json configuration file.

#### Dependency Injection

The `LogDataStore` class is registered as a singleton. It can be injected into the consumer class:

```csharp
public class MyConsumer
{
    public MyConsumer(ILogDataStore dataStore)
        => _dataStore = dataStore;

    private ILogDataStore? _dataStore;
}
```

```vbnet
Public Class MyConsumer
  Public Sub New(dataStore As ILogDataStore)
    _dataStore = dataStore
  End Sub
  Private _dataStore As ILogDataStore
End Class
```

Or request an instance manually from the service provider:

```csharp
public class MyConsumer
{
    public MyConsumer(IServiceProvider serviceProvider)
        => _dataStore = serviceProvider.GetRequiredService<LogDataStore>();

    private ILogDataStore? _dataStore;
}
```

```vbnet
Public Class MyConsumer
  Public Sub New(serviceProvider As IServiceProvider)
    _dataStore = serviceProvider.GetRequiredService(Of LogDataStore)()
  End Sub
  Private _dataStore As ILogDataStore
End Class
```

Register `MyConsumer` for DI:

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<LogDataStore>();
builder.Services.AddTransient<MyConsumer>();

_host = builder.Build();
```

```vbnet
Dim builder As HostApplicationBuilder = Host.CreateApplicationBuilder()

builder.Services.AddSingleton(Of LogDataStore)()
builder.Services.AddTransient(Of MyConsumer)()

_host = builder.Build()
```

#### Manually (without Dependency Injection)

Hold the Data Store in a singleton so it's shared between the logger (producer) and the consumer(s):

```csharp
public static class MainControlsDataStore
{
    public static ILogDataStore DataStore { get; } = new LogDataStore();
}
```

```vbnet
Public Module MainControlsDataStore
  Public ReadOnly Property DataStore As ILogDataStore = New LogDataStore()
End Module
```

Pass the `DataStore` to your consumer for IOC-friendly construction:

```csharp
public class MyConsumer
{
    public MyConsumer(ILogDataStore dataStore)
        => _dataStore = dataStore;

    private ILogDataStore? _dataStore;
}
```

```vbnet
Public Class MyConsumer
  Public Sub New(dataStore As ILogDataStore)
    _dataStore = dataStore
  End Sub
  Private _dataStore As ILogDataStore
End Class
```

Usage:

```csharp
MyConsumer instance = new MyConsumer(MainControlsDataStore.DataStore);
```

```vbnet
Dim instance As New MyConsumer(MainControlsDataStore.DataStore)
```

#### Listening for New Entries

When instantiating `MyConsumer` and referencing `LogDataStore`, either listen to the `Entries.CollectionChanged` event manually or let data binding do the work.

Manual event handling example:

```csharp
public class MyConsumer
{
    public MyConsumer(LogDataStore dataStore)
    {
        _dataStore = dataStore;
        _dataStore.Entries.CollectionChanged += OnCollectionChanged;
    }

    private ILogDataStore? _dataStore;

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?.Count > 0)
        {
            // process new items
        }

        if (e.OldItems?.Count > 0)
        {
            // remove items if needed
        }
    }
}
```

```vbnet
Public Class MyConsumer
  Public Sub New(dataStore As LogDataStore)
    _dataStore = dataStore
    AddHandler _dataStore.Entries.CollectionChanged, AddressOf OnCollectionChanged
  End Sub

  Private _dataStore As ILogDataStore

  Private Sub OnCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
    If e.NewItems IsNot Nothing AndAlso e.NewItems.Count > 0 Then
      ' process new items
    End If
    If e.OldItems IsNot Nothing AndAlso e.OldItems.Count > 0 Then
      ' remove items if needed
    End If
  End Sub
End Class
```

## LogViewerControl Implementation

The `Logger` code is in two parts:

1. Common code — `LogViewer.Core` project (shared code)
2. Application-specific control wrappers:

* WinForms — `LogViewer.WinForms`
* WPF — `LogViewer.Wpf`
* Avalonia — `LogViewer.Avalonia`

We must marshal updates to the UI thread. A `DispatcherHelper` is included for WinForms and WPF; Avalonia uses `Dispatcher.UIThread`.

### DispatcherHelper Class

WinForms implementation:

```csharp
public static class DispatcherHelper
{
    public static void Execute(Action action)
    {
        if (Application.OpenForms.Count == 0)
        {
            action.Invoke();
            return;
        }

        try
        {
            if (Application.OpenForms[0]!.InvokeRequired)
                Application.OpenForms[0]!.Invoke(action);
            else
                action.Invoke();
        }
        catch (Exception)
        {
            // ignore as app may be shutting down
        }
    }
}
```

```vbnet
Public Module DispatcherHelper
  Public Sub Execute(action As Action)
    If Application.OpenForms.Count = 0 Then
      action.Invoke()
      Return
    End If

    Try
      If Application.OpenForms(0).InvokeRequired Then
        Application.OpenForms(0).Invoke(action)
      Else
        action.Invoke()
      End If
    Catch
      ' ignore as app may be shutting down
    End Try
  End Sub
End Module
```

WPF implementation:

```csharp
public static class DispatcherHelper
{
    public static void Execute(Action action)
    {
        if (Application.Current is null || Application.Current.Dispatcher is null)
            return; // already on UI thread

        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
    }
}
```

```vbnet
Public Module DispatcherHelper
  Public Sub Execute(action As Action)
    If Application.Current Is Nothing OrElse Application.Current.Dispatcher Is Nothing Then
      Return
    End If
    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, action)
  End Sub
End Module
```

Avalonia usage example:

```csharp
await Dispatcher.UIThread.InvokeAsync(() => /* do UI work */);
```

```vbnet
Await Dispatcher.UIThread.InvokeAsync(Sub() ' do UI work ' End Sub)
```

### Common code — LoggerExtensions

```csharp
public static class LoggerExtensions
{
    public static void Emit(this ILogger logger, EventId eventId,
        LogLevel logLevel, string message, Exception? exception = null,
        params object?[] args)
    {
        if (logger is null) return;

        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(eventId, message, args);
                break;
            case LogLevel.Debug:
                logger.LogDebug(eventId, message, args);
                break;
            case LogLevel.Information:
                logger.LogInformation(eventId, message, args);
                break;
            case LogLevel.Warning:
                logger.LogWarning(eventId, exception, message, args);
                break;
            case LogLevel.Error:
                logger.LogError(eventId, exception, message, args);
                break;
            case LogLevel.Critical:
                logger.LogCritical(eventId, exception, message, args);
                break;
        }
    }

    public static void TestPattern(this ILogger logger, EventId eventId)
    {
        Exception ex = new("Test Error Message");
        logger.Emit(eventId, LogLevel.Trace, "Trace Test Pattern");
        logger.Emit(eventId, LogLevel.Debug, "Debug Test Pattern");
        logger.Emit(eventId, LogLevel.Information, "Information Test Pattern");
        logger.Emit(eventId, LogLevel.Warning, "Warning Test Pattern");
        logger.Emit(eventId, LogLevel.Error, "Error Test Pattern", ex);
        logger.Emit(eventId, LogLevel.Critical, "Critical Test Pattern", ex);
    }
}
```

```vbnet
Public Module LoggerExtensions
  <Extension>
  Public Sub Emit(logger As ILogger, eventId As EventId,
                  logLevel As LogLevel, message As String, ParamArray args As Object())
    logger.Emit(eventId, logLevel, message, Nothing, args)
  End Sub

  <Extension>
  Public Sub Emit(logger As ILogger, eventId As EventId,
                  logLevel As LogLevel, message As String, [exception] As Exception,
                  ParamArray args As Object())
    If logger Is Nothing Then Return
    Select Case logLevel
      Case LogLevel.Trace : logger.LogTrace(eventId, message, args)
      Case LogLevel.Debug : logger.LogDebug(eventId, message, args)
      Case LogLevel.Information : logger.LogInformation(eventId, message, args)
      Case LogLevel.Warning : logger.LogWarning(eventId, [exception], message, args)
      Case LogLevel.[Error] : logger.LogError(eventId, [exception], message, args)
      Case LogLevel.Critical : logger.LogCritical(eventId, [exception], message, args)
    End Select
  End Sub

  <Extension>
  Public Sub TestPattern(logger As ILogger, Optional eventId As EventId = Nothing)
    Dim ex As New Exception("Test Error Message")
    logger.Emit(eventId, LogLevel.Trace, "Trace Test Pattern")
    logger.Emit(eventId, LogLevel.Debug, "Debug Test Pattern")
    logger.Emit(eventId, LogLevel.Information, "Information Test Pattern")
    logger.Emit(eventId, LogLevel.Warning, "Warning Test Pattern")
    logger.Emit(eventId, LogLevel.[Error], "Error Test Pattern", ex)
    logger.Emit(eventId, LogLevel.Critical, "Critical Test Pattern", ex)
  End Sub
End Module
```

### ViewModel: LogViewerControlViewModel

```csharp
public class LogViewerControlViewModel : ViewModel, ILogDataStoreImpl
{
    public LogViewerControlViewModel(ILogDataStore dataStore)
        => DataStore = dataStore;

    public ILogDataStore DataStore { get; }
}
```

```vbnet
Public Class LogViewerControlViewModel
  Inherits ViewModel
  Implements ILogDataStoreImpl

  Public Sub New(store As ILogDataStore)
    DataStore = store
  End Sub

  Public ReadOnly Property DataStore As ILogDataStore _
       Implements ILogDataStoreImpl.DataStore
End Class
```

### WinForms — LogViewerControl

![WinForms LogViewer](https://cloudfront.codeproject.com/articles/5357417/logviewercontrol_-_winforms_640.png)

Code-behind:

```csharp
public partial class LogViewerControl : UserControl
{
    public LogViewerControl()
    {
        InitializeComponent();
        ListView.SetDoubleBuffered();
        Disposed += OnDispose;
    }

    public LogViewerControl(LogViewerControlViewModel vm) : this()
        => RegisterLogDataStore(vm.DataStore);

    private ILogDataStore? _dataStore;
    private static readonly SemaphoreSlim _semaphore = new(1);

    public void RegisterLogDataStore(ILogDataStore dataStore)
    {
        _dataStore = dataStore;
        AddListViewItems(_dataStore.Entries);
        _dataStore.Entries.CollectionChanged += OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?.Count > 0)
        {
            AddListViewItems(e.NewItems.Cast<LogModel>());
            ExclusiveDispatcher(() =>
            {
                if (CanAutoScroll.Checked)
                    ListView.Items[^1].EnsureVisible();
            });
        }
    }

    private void AddListViewItems(IEnumerable<LogModel> logEntries)
    {
        ExclusiveDispatcher(() =>
        {
            foreach (var item in logEntries)
            {
                var lvi = new ListViewItem
                {
                    Font = new(ListView.Font, FontStyle.Regular),
                    Text = item.Timestamp.ToString("G"),
                    ForeColor = item.Color!.Foreground,
                    BackColor = item.Color.Background
                };
                lvi.SubItems.Add(item.LogLevel.ToString());
                lvi.SubItems.Add(item.EventId.ToString());
                lvi.SubItems.Add(item.State?.ToString() ?? string.Empty);
                lvi.SubItems.Add(item.Exception ?? string.Empty);
                ListView.Items.Add(lvi);
            }
        });
    }

    private void ExclusiveDispatcher(Action action)
    {
        _semaphore.Wait();
        DispatcherHelper.Execute(action.Invoke);
        _semaphore.Release();
    }

    private void OnDispose(object? sender, EventArgs e)
    {
        Disposed -= OnDispose;
        if (_dataStore is null) return;
        _dataStore.Entries.CollectionChanged -= OnCollectionChanged;
    }
}
```

```vbnet
Public Class LogViewerControl
  Public Sub New()
    InitializeComponent()
    ListView.SetDoubleBuffered()
    AddHandler Disposed, AddressOf OnDispose
  End Sub

  Public Sub New(vm As LogViewerControlViewModel)
    Me.New()
    RegisterLogDataStore(vm.DataStore)
  End Sub

  Private _dataStore As ILogDataStore
  Private Shared ReadOnly _semaphore As New SemaphoreSlim(1)

  Public Sub RegisterLogDataStore(store As ILogDataStore)
    _dataStore = store
    AddListViewItems(_dataStore.Entries)
    AddHandler _dataStore.Entries.CollectionChanged, AddressOf OnCollectionChanged
  End Sub

  Private Sub OnCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
    If e.NewItems IsNot Nothing AndAlso e.NewItems.Count > 0 Then
      AddListViewItems(e.NewItems.Cast(Of LogModel))
      ExclusiveDispatcher(Sub()
        If CanAutoScroll.Checked Then
          ListView.Items(ListView.Items.Count - 1).EnsureVisible()
        End If
      End Sub)
    End If
  End Sub

  Private Sub AddListViewItems(entries As IEnumerable(Of LogModel))
    ExclusiveDispatcher(Sub()
      For Each item In entries
        Dim lvi As New ListViewItem With {
          .Font = New Font(ListView.Font, FontStyle.Regular),
          .Text = item.Timestamp.ToString("G"),
          .ForeColor = item.Color.Foreground,
          .BackColor = item.Color.Background
        }
        lvi.SubItems.Add(item.LogLevel.ToString())
        lvi.SubItems.Add(item.EventId.ToString())
        lvi.SubItems.Add(If(item.State Is Nothing, String.Empty, item.State.ToString()))
        lvi.SubItems.Add(If(item.Exception, String.Empty))
        ListView.Items.Add(lvi)
      Next
    End Sub)
  End Sub

  Private Sub ExclusiveDispatcher(action As Action)
    _semaphore.Wait()
    Execute(Sub() action.Invoke())
    _semaphore.Release()
  End Sub

  Private Sub OnDispose(sender As Object, e As EventArgs)
    RemoveHandler Disposed, AddressOf OnDispose
    If _dataStore Is Nothing Then Return
    RemoveHandler _dataStore.Entries.CollectionChanged, AddressOf OnCollectionChanged
  End Sub
End Class
```

Here is a GIF with default colorization in action:

![WinForms GIF](https://cloudfront.codeproject.com/articles/5357417/winform_logviewer.gif)

### WPF — LogViewerControl

![WPF LogViewer](https://cloudfront.codeproject.com/articles/5357417/logviewercontrol_-_wpf_640.png)

Code-behind:

```csharp
public partial class LogViewerControl
{
    public LogViewerControl() => InitializeComponent();

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (CanAutoScroll.IsChecked != true) return;
        if (DataContext is null) return;

        LogModel? item = (DataContext as ILogDataStoreImpl)?.DataStore.Entries.LastOrDefault();
        if (item is null) return;
        ListView.ScrollIntoView(item);
    }
}
```

```vbnet
Public Class LogViewerControl
  Public Sub New()
    InitializeComponent()
  End Sub

  Private Sub OnLayoutUpdated(sender As Object, e As EventArgs)
    If Not CanAutoScroll.IsChecked Then Return
    If DataContext Is Nothing Then Return
    Dim store As ILogDataStoreImpl = DirectCast(DataContext, ILogDataStoreImpl)
    Dim item As LogModel = store.DataStore.Entries.LastOrDefault()
    If item Is Nothing Then Return
    ListView.ScrollIntoView(item)
  End Sub
End Class
```

Expose a common interface for binding:

```csharp
public interface ILogDataStoreImpl { LogDataStore DataStore { get; } }
```

```vbnet
Public Interface ILogDataStoreImpl
  ReadOnly Property DataStore As ILogDataStore
End Interface
```

User Interface (XAML excerpt):

```xml
<ListView x:Name="ListView"
          ItemsSource="{Binding DataStore.Entries}"
          LayoutUpdated="OnLayoutUpdated">
  <ListView.Resources>
    <Style TargetType="{x:Type ListViewItem}">
      <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
      <Setter Property="BorderBrush" Value="Silver"/>
      <Setter Property="BorderThickness" Value="0,0,0,1"/>
      <Setter Property="Foreground" Value="{Binding Color.Foreground, Converter={StaticResource ColorConverter}}" />
      <Setter Property="Background" Value="{Binding Color.Background, Converter={StaticResource ColorConverter}}" />
    </Style>
  </ListView.Resources>
  <ListView.View>
    <GridView>
      <GridViewColumn Header="Time" Width="140">
        <GridViewColumn.CellTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding Timestamp}"/>
          </DataTemplate>
        </GridViewColumn.CellTemplate>
      </GridViewColumn>
      <GridViewColumn Header="Level" Width="80">
        <GridViewColumn.CellTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding LogLevel}"/>
          </DataTemplate>
        </GridViewColumn.CellTemplate>
      </GridViewColumn>
      <GridViewColumn Header="Event Id" Width="120">
        <GridViewColumn.CellTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding EventId}"/>
          </DataTemplate>
        </GridViewColumn.CellTemplate>
      </GridViewColumn>
      <GridViewColumn Header="State" Width="300">
        <GridViewColumn.CellTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding State}"/>
          </DataTemplate>
        </GridViewColumn.CellTemplate>
      </GridViewColumn>
      <GridViewColumn Header="Exception" Width="300">
        <GridViewColumn.CellTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding Exception}"/>
          </DataTemplate>
        </GridViewColumn.CellTemplate>
      </GridViewColumn>
    </GridView>
  </ListView.View>
 </ListView>
```

WPF color converter:

```csharp
public class ChangeColorTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var c = (System.Drawing.Color)value;
        return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

```vbnet
Public Class ChangeColorTypeConverter
  Implements IValueConverter
  Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    Dim c As System.Drawing.Color = DirectCast(value, System.Drawing.Color)
    Return New SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B))
  End Function
  Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
    Throw New NotImplementedException()
  End Function
End Class
```

GIF:

![WPF GIF](https://cloudfront.codeproject.com/articles/5357417/wpf_logviewer.gif)

### Avalonia — LogViewerControl

![Avalonia LogViewer](https://cloudfront.codeproject.com/articles/5357417/logviewercontrol_-_avalonia_640.png)

Code-behind:

```csharp
public partial class LogViewerControl : UserControl
{
    public LogViewerControl() => InitializeComponent();

    private ILogDataStoreImpl? vm;
    private LogModel? item;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is null) return;
        vm = (ILogDataStoreImpl)DataContext;
        vm.DataStore.Entries.CollectionChanged += OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => item = MyDataGrid.Items.Cast<LogModel>().LastOrDefault();

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (CanAutoScroll.IsChecked != true || item is null) return;
        MyDataGrid.ScrollIntoView(item, null);
        item = null;
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (vm is null) return;
        vm.DataStore.Entries.CollectionChanged -= OnCollectionChanged;
    }
}
```

```vbnet
Partial Public Class LogViewerControl
  Inherits UserControl

  Private _vm As ILogDataStoreImpl
  Private _model As LogModel

  Private Sub OnDataContextChanged(sender As Object, e As EventArgs)
    If DataContext Is Nothing Then Return
    _vm = DirectCast(DataContext, ILogDataStoreImpl)
    AddHandler _vm.DataStore.Entries.CollectionChanged, AddressOf OnCollectionChanged
  End Sub

  Private Sub OnCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
    _model = MyDataGrid.Items.Cast(Of LogModel).LastOrDefault()
  End Sub

  Private Sub OnLayoutUpdated(sender As Object, e As EventArgs)
    If CanAutoScroll.IsChecked <> True OrElse _model Is Nothing Then Return
    MyDataGrid.ScrollIntoView(_model, Nothing)
    _model = Nothing
  End Sub

  Private Sub OnDetachedFromLogicalTree(sender As Object, e As LogicalTreeAttachmentEventArgs)
    If _vm Is Nothing Then Return
    RemoveHandler _vm.DataStore.Entries.CollectionChanged, AddressOf OnCollectionChanged
  End Sub
End Class
```

User Interface (Avalonia XAML excerpt):

```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition />
    <RowDefinition Height="Auto" />
  </Grid.RowDefinitions>
  <Grid.Resources>
    <converters:ChangeColorTypeConverter x:Key="ColorConverter" />
    <converters:EventIdConverter x:Key="EventIdConverter"/>
    <SolidColorBrush x:Key="ColorBlack">Black</SolidColorBrush>
    <SolidColorBrush x:Key="ColorTransparent">Transparent</SolidColorBrush>
  </Grid.Resources>
  <Grid.Styles>
    <Style Selector="DataGridRow">
      <Setter Property="Padding" Value="0" />
      <Setter Property="Foreground" Value="{Binding Color.Foreground, Converter={StaticResource ColorConverter}, ConverterParameter={StaticResource ColorBlack}}" />
      <Setter Property="Background" Value="{Binding Color.Background, Converter={StaticResource ColorConverter}, ConverterParameter={StaticResource ColorTransparent}}" />
    </Style>
  </Grid.Styles>
  <DataGrid x:Name="MyDataGrid" Items="{Binding DataStore.Entries}" AutoGenerateColumns="False" CanUserSortColumns="False" LayoutUpdated="OnLayoutUpdated">
    <DataGrid.Columns>
      <DataGridTextColumn Header="Time" Width="150" Binding="{Binding Timestamp}"/>
      <DataGridTextColumn Header="Level" Width="90" Binding="{Binding LogLevel}" />
      <DataGridTextColumn Header="Event Id" Width="120" Binding="{Binding EventId, Converter={StaticResource EventIdConverter}}" />
      <DataGridTextColumn Header="State" Width="300" Binding="{Binding State}" />
      <DataGridTextColumn Header="Exception" Width="300" Binding="{Binding Exception}" />
    </DataGrid.Columns>
  </DataGrid>
 </Grid>
```

Converters:

```csharp
public class ChangeColorTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var c = (System.Drawing.Color)value;
        return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EventIdConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? "0" : ((EventId)value).ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new EventId(0, value?.ToString() ?? string.Empty);
}
```

```vbnet
Public Class ChangeColorTypeConverter : Implements IValueConverter
  Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    Dim c As System.Drawing.Color = DirectCast(value, System.Drawing.Color)
    Return New SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B))
  End Function
  Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
    Throw New NotImplementedException
  End Function
End Class

Public Class EventIdConverter : Implements IValueConverter
  Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    If value Is Nothing Then Return "0"
    Dim eventId As EventId = DirectCast(value, EventId)
    Return eventId.ToString()
  End Function
  Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
    Return New EventId(0, If(value Is Nothing, String.Empty, value.ToString()))
  End Function
End Class
```

GIF:

![Avalonia GIF](https://cloudfront.codeproject.com/articles/5357417/mac_avalonia_logging_vb.gif)

## Summary

We covered how logging works; how to create, register, and use a custom logger & provider with customization for **WinForms**, **WPF**, and **Avalonia** application types in both **C#** & **VB**. We looked at the internal code of .NET for working with loggers & providers. We created custom controls for **WinForms**, **WPF**, and **Avalonia** application types in both **C#** & **VB**, to consume the logs from a custom logger, using Microsoft's Default Logger and a 3rd-party SeriLog structured logger. We also covered how to use the custom loggers and the custom control for both Dependency Injection and manual wiring up. Lastly, we created the **.NET Framework** Background Service for emulating an application generating log entries.

Whilst this article was long and thorough, creating Custom Loggers and consuming the content generate is not complicated, regardless of application type and how the application is wired up, either manually or via Dependency Injection.

All source code, both **C#** and **VB**, is provided in the link at the top of this article. To use in your own project, copy all of the required libraries for the application type, add a reference to the `LogViewer` control project + the type of logger project, and then follow the guidelines for usage.

If you have any questions, please post below and I would be more than happy to answer.

## References

### Documentation, articles, etc

#### .NET (Core) 7.0 Framework

* [.NET App Settings Demystified (C# & VB) | CodeProject](https://www.codeproject.com/Articles/5354478/NET-App-Settings-Demystified-Csharp-VB)
* [Logging in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line)
* [Implement a custom logging provider in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider)
* [Dependency injection in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
* [Model-View-ViewModel (MVVM) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm#the-mvvm-pattern)
* [Data binding overview (Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/data/overview?view=netdesktop-7.0) [.NET) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-7.0)
* [Data binding overview (WPF .NET) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-7.0)
* [BackgroundService | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-7.0)
* [Background tasks with hosted services in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio)
* [Announcing .NET 6 — The Fastest .NET Yet > Microsoft.Extensions.Logging](https://devblogs.microsoft.com/dotnet/announcing-net-6/#microsoft-extensions-logging-compile-time-source-generator)
* [Compile-time logging source generation | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
* [What's new in .NET 8 Preview 6 > Expanding LoggerMessageAttribute Constructor Overloads for Enhanced Functionality | Github](https://github.com/dotnet/core/issues/8437#issuecomment-1605698272)

#### Avalonia UI

* [Avalonia UI](https://avaloniaui.net/)
* [Comparison of Avalonia with WPF and UWP | Avalonia UI](https://docs.avaloniaui.net/guides/developer-guides/comparison-of-avalonia-with-wpf-and-uwp)
* [The Missing Avalonia Templates for VB | Code Project](https://www.codeproject.com/Articles/5357284/Avalonia-for-VB)

#### Serilog

* [Serilog](https://serilog.net/)

#### NLog

* [Getting started with .NET Core 2 Console application | NLog](https://github.com/NLog/NLog/wiki/Getting-started-with-.NET-Core-2---Console-application#a-minimal-example)
* [Getting started with ASP.NET Core 6 | NLog](https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-6)
* [How to write a custom target | NLog](https://github.com/NLog/NLog/wiki/How-to-write-a-custom-target)
* [Register your custom component | NLog](https://github.com/NLog/NLog/wiki/Register-your-custom-component)
* [NLog configuration with appsettings.json | NLog](https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json)
* [NLog properties with Microsoft Extension Logging | NLog](https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-properties-with-Microsoft-Extension-Logging)
* [NLog.Extensions.Logging changes capture of EventId | NLog](https://nlog-project.org/2021/08/25/nlog-5-0-preview1-ready.html#nlogextensionslogging-changes-capture-of-eventid)

#### Log4Net

* [Apache Log4Net | Apache](https://logging.apache.org/log4net/)
* [Apache log4net Manual - Configuration | Apache](https://logging.apache.org/log4net/release/manual/configuration.html)
* [http://svn.apache.org/logging/log4net | Apache Repository](http://svn.apache.org/viewvc/logging/log4net/trunk/examples/net/2.0/Extensibility/EventIDLogApp/cs/src/)
* [How to use Log4Net with ASP.NET Core for logging | DotNetThoughts Blog](https://dotnetthoughts.net/how-to-use-log4net-with-aspnetcore-for-logging/)
* [huorswords / Microsoft.Extensions.Logging.Log4Net.AspNetCore | Github](https://github.com/huorswords/Microsoft.Extensions.Logging.Log4Net.AspNetCore)

## History

* 23rd March, 2023 - v1.00 - Initial release
* 28th March, 2023 - v1.10 - Added support for [NLOG](https://nlog-project.org/) logging platform + **WinForms**, **WPF**, and **Avalonia** sample DI & no-DI applications (x6); fixed an issue in `LogViewer.Winforms` project where possible "index out of range" exception occasionally occurs
* 29th March, 2023 - v1.20 = Added support for [Apache Log4Net](https://logging.apache.org/log4net/) logging Services + **WinForms**, **WPF**, and **Avalonia** sample DI & no-DI applications (x6); various code cleanup and optimizations
* 20th April, 2023 - v1.20a - rezipped project using Microsoft's File Explorer "Compress to Zip"
* 12th September, 2023 - v1.30 - Added [LoggerMessageAttribute (C# only)](https://www.codeproject.com/Articles/5357417/LogViewer-Control-for-WinForms-WPF-and-Avalonia-in#loggermessageattribute) section
