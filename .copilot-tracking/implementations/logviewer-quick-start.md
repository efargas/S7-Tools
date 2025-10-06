# LogViewer Implementation Quick Start Guide

**For Implementation Agents**: This is your quick reference for implementing the LogViewer system in S7Tools.

## 🚀 Quick Implementation Checklist

### Prerequisites ✅
- [ ] Read `logviewer-implementation-plan.md` for full details
- [ ] Read `logviewer-agent-instructions.md` for specific instructions
- [ ] Understand existing S7Tools architecture (DO NOT BREAK)
- [ ] Reference implementation at `.github/agents/workspace/referent-projects/LogViewerControl/`

### Phase 1: Infrastructure (Days 1-3) 🏗️

#### 1.1 Create New Project
```bash
# In src/ directory
dotnet new classlib -n S7Tools.Infrastructure.Logging -f net8.0
cd S7Tools.Infrastructure.Logging

# Add required packages
dotnet add package Avalonia --version 11.3.6
dotnet add package Avalonia.ReactiveUI --version 11.3.6
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
dotnet add package Microsoft.Extensions.Options --version 8.0.0
dotnet add package ReactiveUI --version 20.1.1
dotnet add package System.Drawing.Common --version 8.0.0
```

#### 1.2 Create Core Models (Required Files)
- `Core/Models/LogModel.cs` - Log entry data model
- `Core/Models/LogEntryColor.cs` - Color configuration
- `Core/Models/LogDataStoreOptions.cs` - Storage options
- `Core/Storage/ILogDataStore.cs` - Storage interface
- `Core/Storage/LogDataStore.cs` - Thread-safe storage
- `Core/Configuration/DataStoreLoggerConfiguration.cs` - Logger config

#### 1.3 Create Logger Provider (Required Files)
- `Providers/Microsoft/DataStoreLogger.cs` - ILogger implementation
- `Providers/Microsoft/DataStoreLoggerProvider.cs` - ILoggerProvider implementation
- `Providers/Extensions/LoggingServiceCollectionExtensions.cs` - DI extensions

### Phase 2: UI Components (Days 4-7) 🎨

#### 2.1 Create Converters (Required Files)
- `Converters/LogLevelToColorConverter.cs`
- `Converters/LogLevelToIconConverter.cs`
- `Converters/EventIdConverter.cs`

#### 2.2 Create LogViewer Control (Required Files)
- `Controls/LogViewerControl.axaml` - Main control XAML
- `Controls/LogViewerControl.axaml.cs` - Control code-behind
- `Controls/ViewModels/LogViewerControlViewModel.cs` - Control ViewModel

### Phase 3: Integration (Days 8-10) 🔗

#### 3.1 Add Configuration (Required Files)
- `src/S7Tools/appsettings.json` - Application configuration

#### 3.2 Create Views (Required Files)
- `src/S7Tools/Views/Logging/LoggingView.axaml`
- `src/S7Tools/Views/Logging/LoggingView.axaml.cs`
- `src/S7Tools/ViewModels/Logging/LoggingViewModel.cs`

#### 3.3 Update Existing Files (CAREFUL - ADDITIVE ONLY)
- `src/S7Tools/S7Tools.csproj` - Add project reference
- `src/S7Tools/Program.cs` - Add logging services
- `src/S7Tools/ViewModels/MainWindowViewModel.cs` - Add navigation item

## 🔧 Critical Implementation Patterns

### Thread-Safe Storage Pattern
```csharp
public class LogDataStore : ILogDataStore
{
    private static readonly SemaphoreSlim _semaphore = new(1);
    
    public void AddEntry(LogModel logModel)
    {
        _semaphore.Wait();
        try
        {
            if (Entries.Count >= _maxEntries)
                Entries.RemoveAt(0); // Circular buffer
            Entries.Add(logModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### ReactiveUI ViewModel Pattern
```csharp
public class LogViewerControlViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }
}
```

### Service Registration Pattern
```csharp
// In Program.cs - ADD ONLY, don't modify existing
private static void ConfigureServices(IServiceCollection services)
{
    // [EXISTING SERVICES - DO NOT MODIFY]
    
    // [NEW] Add configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // [NEW] Add logging infrastructure
    services.AddLoggingInfrastructure(configuration);
    services.AddSingleton<LogViewerControlViewModel>();
    services.AddSingleton<LoggingViewModel>();

    // [NEW] Configure logging
    services.AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddConsole();
        builder.AddDebug();
        builder.AddDataStoreLogger();
    });
}
```

## 🚫 Critical DON'Ts

### Never Modify These Files
- `src/S7Tools/Services/` - Existing service implementations
- `src/S7Tools/Views/MainWindow.axaml` - Main window structure
- `src/S7Tools.Core/` - Core business logic
- Existing ViewModels (except to ADD navigation item)

### Never Change These Patterns
- Existing dependency injection lifetimes
- Current ReactiveUI patterns
- Avalonia project configuration
- Existing service interfaces

## 📋 Testing Checklist

### Functional Tests
- [ ] LogViewer displays entries in real-time
- [ ] Color coding works for all log levels
- [ ] Auto-scroll functionality works
- [ ] Search and filtering work
- [ ] Clear logs functionality works
- [ ] Navigation to logging view works
- [ ] **ALL EXISTING FUNCTIONALITY STILL WORKS**

### Performance Tests
- [ ] UI responsive with 1000+ entries
- [ ] Memory usage stable
- [ ] No memory leaks
- [ ] Startup time impact < 100ms

## 🆘 Emergency Rollback

If something breaks:
1. Comment out logging service registration in `Program.cs`
2. Remove LogViewer navigation item from `MainWindowViewModel`
3. Remove project references if needed

## 📚 Key Reference Files

**Primary Reference**: `.github/agents/workspace/referent-projects/LogViewerControl/CSharp/Applications/AvaloniaLoggingDI/`

**Key Files to Study**:
- `App.axaml.cs` - DI configuration patterns
- `Program.cs` - Service registration
- `appsettings.json` - Configuration structure
- `LogViewerControl.axaml` - UI implementation

## 🎯 Success Criteria

- ✅ Real-time log display with color coding
- ✅ Search and filtering capabilities
- ✅ Integration with existing navigation
- ✅ **No existing functionality broken**
- ✅ Performance requirements met
- ✅ Comprehensive documentation

## 📞 Need Help?

1. Check `logviewer-implementation-plan.md` for detailed instructions
2. Review `dotnet-logviewer-memory.instructions.md` for patterns
3. Study reference implementation in `.github/agents/workspace/referent-projects/`
4. Follow .NET best practices in `.github/prompts/dotnet-best-practices.prompt.md`

---

**Remember**: This is an ADDITIVE implementation. The existing S7Tools application must continue to work exactly as before!