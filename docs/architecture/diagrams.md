---
title: "S7Tools System Architecture Diagrams"
version: "1.1.0"
created: "2025-10-16"
last-updated: "2025-11-07"
status: "current"
tags: ["architecture", "diagrams", "visualization", "mermaid", "clean-architecture", "mvvm"]
related:
  - "docs/architecture/overview.md"
  - "docs/architecture/clean-architecture.md"
  - "docs/architecture/mvvm-patterns.md"
  - "docs/patterns/system-patterns.md"
supersedes: []
---

# S7Tools System Architecture Diagrams

## Overview

This document provides visual representations of the S7Tools architecture, patterns, and component relationships. Updated to reflect the ViewModels/Views categorization (November 6-7, 2025).

---

## 1. Clean Architecture Layers

```
┌───────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                 │
│                            (S7Tools)                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                        VIEWS (Avalonia XAML - Categorized)           │ │
│  │  Base (MainWindow)                                                   │ │
│  │  Controls (PropertyDisplayItem, SidebarSection,                      │ │
│  │            SerialPortDiscoveryControl, CloseApplicationBehavior)     │ │
│  │  Dialogs (ConfirmationDialog, InputDialog)                           │ │
│  │  Jobs (JobsMain, JobsSidebar, JobWizard)                             │ │
│  │  Layout (TaskManagerShell, LoggingTest)                              │ │
│  │  Pages (Home, Connections, LogViewer, About, PlcInput)               │ │
│  │  Profiles (SerialPortProfileEditContent, SocatProfileEditContent,    │ │
│  │            PowerSupplyProfileEditContent)                            │ │
│  │  Settings (Settings, SettingsCategories, General, Appearance,        │ │
│  │            Logging, Advanced, SerialPorts, Socat, PowerSupply)       │ │
│  │  Tasks (TaskManager, TaskManagerSidebar, TaskQueue)                  │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     ↕                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                    VIEWMODELS (ReactiveUI - Categorized)             │ │
│  │  Base (ViewModelBase, TabViewModel)                                  │ │
│  │  Controls (PropertyDisplayItem, SidebarSection)                      │ │
│  │  Dialogs (ConfirmationDialog, InputDialog)                           │ │
│  │  Jobs (JobsManagement, JobWizard, JobWizardPlaceholder)              │ │
│  │  Layout (MainWindow, Navigation, NavigationItem, BottomPanel,        │ │
│  │          SettingsManagement, TaskManagerShell)                       │ │
│  │  Pages (Home, Connections, LogViewer, About, PlcInput)               │ │
│  │  Profiles (SerialPort, Socat, PowerSupply profiles)                  │ │
│  │  Settings (Settings, General, Appearance, Logging, Advanced,         │ │
│  │            SerialPorts, Socat, PowerSupply, SerialPortScanner)       │ │
│  │  Tasks (TaskManager, TaskQueue, Task)                                │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     ↕                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                    APPLICATION SERVICES                              │ │
│  │  UI Services:                Profile Services:                       │ │
│  │  • DialogService             • SerialPortProfileService              │ │
│  │  • ThemeService              • SocatProfileService                   │ │
│  │  • LayoutService             • PowerSupplyProfileService             │ │
│  │  • ActivityBarService        (all inherit StandardProfileManager<T>) │ │
│  │  • UIThreadService                                                   │ │
│  │  • LocalizationService       Support Services:                       │ │
│  │  • FileDialogService         • LogExportService                      │ │
│  │  • ClipboardService          • SettingsService                       │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ depends on
                                     ↓
┌───────────────────────────────────────────────────────────────────────────┐
│                          DOMAIN LAYER                                      │
│                        (S7Tools.Core)                                      │
│                     ❌ ZERO EXTERNAL DEPENDENCIES                          │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                  INTERFACES & CONTRACTS                              │ │
│  │  Service Interfaces:         Profile Contracts:                      │ │
│  │  • IUIThreadService          • IProfileBase                          │ │
│  │  • IDialogService            • IProfileManager<T>                    │ │
│  │  • ISerialPortService        • ISerialPortProfileService             │ │
│  │  • ISocatService             • ISocatProfileService                  │ │
│  │  • IPowerSupplyService       • IPowerSupplyProfileService            │ │
│  │  • ILayoutService                                                    │ │
│  │  • IThemeService             Command Pattern:                        │ │
│  │  • ISettingsService          • ICommand<TOptions, TResult>           │ │
│  │                              • ICommandHandler<TCommand, TResult>    │ │
│  │                              • ICommandDispatcher                    │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     ↕                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                       DOMAIN MODELS                                  │ │
│  │  Profile Models:             Configuration Models:                   │ │
│  │  • SerialPortProfile         • SocatConfiguration                    │ │
│  │  • SocatProfile              • ModbusTcpConfiguration                │ │
│  │  • PowerSupplyProfile        • PowerSupplyConfiguration              │ │
│  │                                                                       │ │
│  │  Value Objects:              Common Models:                          │ │
│  │  • PlcAddress                • Result<T>                             │ │
│  │  • TagValue                  • ValidationResult                      │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                     ↕                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                  COMMANDS & VALIDATION                               │ │
│  │  Commands:                   Validation:                             │ │
│  │  • ExportLogsCommand         • IValidationService                    │ │
│  │  • ImportLogsCommand         • IProfileValidator                     │ │
│  │  • FilterLogsCommand                                                 │ │
│  │  • ClearLogsCommand                                                  │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────┘
                                     ↑
                                     │ depends on
                                     │
┌───────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                  │
│                (S7Tools.Infrastructure.Logging)                            │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                  CUSTOM LOGGING PROVIDER                             │ │
│  │  • DataStoreLoggerProvider   → ILoggerProvider (MEL)                │ │
│  │  • DataStoreLogger           → ILogger (MEL)                         │ │
│  │  • LogDataStore              → ILogDataStore                         │ │
│  │                                                                       │ │
│  │  Features:                                                           │ │
│  │  ✓ In-memory circular buffer (bounded to 10,000 entries)            │ │
│  │  ✓ Thread-safe with ConcurrentQueue                                 │ │
│  │  ✓ Real-time UI integration                                         │ │
│  │  ✓ Structured logging support                                       │ │
│  │  ✓ Observable changes for reactive UI                               │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────┐
│                     CROSS-CUTTING CONCERNS                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────────┐    │
│  │ Dependency       │  │ Logging          │  │ Configuration       │    │
│  │ Injection        │  │ (MEL + Custom)   │  │ (JSON + Services)   │    │
│  │                  │  │                  │  │                     │    │
│  │ Microsoft.       │  │ ILogger<T>       │  │ appsettings.json    │    │
│  │ Extensions.DI    │  │ Structured logs  │  │ SettingsService     │    │
│  └──────────────────┘  └──────────────────┘  └─────────────────────┘    │
│                                                                            │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────────┐    │
│  │ Thread Safety    │  │ Error Handling   │  │ Memory Management   │    │
│  │                  │  │                  │  │                     │    │
│  │ SemaphoreSlim    │  │ Try-Finally      │  │ IDisposable         │    │
│  │ ConcurrentQueue  │  │ Structured logs  │  │ Circular buffers    │    │
│  │ IUIThreadService │  │ Domain rules     │  │ GC.SuppressFinalize │    │
│  └──────────────────┘  └──────────────────┘  └─────────────────────┘    │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## 2. MVVM Pattern Flow

```
USER INTERACTION → VIEW → VIEWMODEL → SERVICE → REPOSITORY → DATA
                    ↓       ↓          ↓         ↓            ↓
                  XAML   ReactiveUI  DI-injected Thread-safe  JSON
                Binding  RaiseAnd    Async/Await Semaphore   Files
                         SetIf
                         Changed

NOTIFICATION FLOW:
DATA → REPOSITORY → SERVICE → VIEWMODEL → VIEW → USER
  ↓                              ↑          ↑
JSON File                  INotifyProperty  Data
Change                     Changed          Binding

Example Flow (Create Profile):

1. User clicks "Create" button in View
   ↓
2. Button Command binding triggers ViewModel.CreateCommand
   ↓
3. ViewModel validates and calls ProfileService.CreateAsync()
   ↓
4. ProfileService (StandardProfileManager<T>) acquires semaphore
   ↓
5. Service validates business rules (unique name, default profile logic)
   ↓
6. Service assigns ID, sets timestamps, saves to JSON file
   ↓
7. Service releases semaphore and returns new profile
   ↓
8. ViewModel updates Profiles ObservableCollection
   ↓
9. RaiseAndSetIfChanged triggers property notification
   ↓
10. View's DataGrid updates via data binding
   ↓
11. User sees new profile in the list
```

---

## 3. Profile Management Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    UNIFIED PROFILE MANAGEMENT                        │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │              IProfileBase (Entity Interface)                   │  │
│  │                                                                 │  │
│  │  + int Id                    ← Unique identifier               │  │
│  │  + string Name               ← Display name                    │  │
│  │  + string Description        ← User description                │  │
│  │  + DateTime CreatedAt        ← Audit trail                     │  │
│  │  + DateTime ModifiedAt       ← Audit trail                     │  │
│  │  + bool IsDefault            ← Default profile flag            │  │
│  │  + bool IsReadOnly           ← System profile protection       │  │
│  │                                                                 │  │
│  │  + bool CanModify()          ← Business rule                   │  │
│  │  + bool CanDelete()          ← Business rule                   │  │
│  │  + string GetSummary()       ← Display representation          │  │
│  │  + IProfileBase Clone()      ← Duplication support             │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↑ implements                            │
│                              │                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐         │
│  │SerialPort    │  │Socat         │  │PowerSupply        │         │
│  │Profile       │  │Profile       │  │Profile            │         │
│  └──────────────┘  └──────────────┘  └───────────────────┘         │
│                              │                                       │
│                              ↓ managed by                            │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │        IProfileManager<T> (Repository Interface)               │  │
│  │                                                                 │  │
│  │  CRUD Operations:                                              │  │
│  │  • Task<T> CreateAsync(T profile)                             │  │
│  │  • Task<T> UpdateAsync(T profile)                             │  │
│  │  • Task<bool> DeleteAsync(int id)                             │  │
│  │  • Task<IReadOnlyList<T>> GetAllAsync()                       │  │
│  │  • Task<T?> GetByIdAsync(int id)                              │  │
│  │                                                                 │  │
│  │  Domain Operations:                                            │  │
│  │  • Task<T> DuplicateAsync(int sourceId, string newName)       │  │
│  │  • Task<T> SetDefaultAsync(int id)                            │  │
│  │  • Task<bool> ExistsAsync(int id)                             │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↑ implements                            │
│                              │                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │    StandardProfileManager<T> (Base Implementation)             │  │
│  │                                                                 │  │
│  │  Thread Safety:                                                │  │
│  │  • SemaphoreSlim _semaphore = new(1, 1)                       │  │
│  │  • Internal method pattern to prevent deadlocks                │  │
│  │                                                                 │  │
│  │  Business Rules:                                               │  │
│  │  • Unique name enforcement (auto-suffix: Name, Name (2), ...)│  │
│  │  • Gap-filling ID assignment (fills deleted IDs)               │  │
│  │  • Single default profile enforcement                          │  │
│  │  • Read-only profile protection                                │  │
│  │  • Audit trail maintenance (CreatedAt/ModifiedAt)              │  │
│  │                                                                 │  │
│  │  Persistence:                                                  │  │
│  │  • JSON file storage (~/.config/S7Tools/profiles/)            │  │
│  │  • Automatic directory creation                                │  │
│  │  • Error recovery (corrupt file → create new)                  │  │
│  │  • Lazy loading (load on first access)                         │  │
│  │                                                                 │  │
│  │  Abstract Methods (subclass must implement):                   │  │
│  │  • protected abstract T CreateSystemDefault()                  │  │
│  │  • protected abstract string ProfileTypeName { get; }          │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              ↑ inherits                              │
│                              │                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐         │
│  │SerialPort    │  │Socat         │  │PowerSupply        │         │
│  │ProfileService│  │ProfileService│  │ProfileService     │         │
│  │              │  │              │  │                   │         │
│  │+ Create      │  │+ Create      │  │+ Create           │         │
│  │  SystemDefault│  │  SystemDefault│  │  SystemDefault    │         │
│  └──────────────┘  └──────────────┘  └───────────────────┘         │
└─────────────────────────────────────────────────────────────────────┘

BENEFITS OF THIS ARCHITECTURE:
✓ Zero code duplication (all CRUD in base class)
✓ Consistent behavior across all profile types
✓ Thread-safe by design (semaphore in base class)
✓ Business rules enforced uniformly
✓ Easy to add new profile types (inherit and implement 2 methods)
✓ Testable (interface-based design)
```

---

## 4. Threading and Synchronization Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    THREADING ARCHITECTURE                            │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                        UI THREAD                               │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Avalonia Dispatcher.UIThread                            │  │  │
│  │  │                                                           │  │  │
│  │  │  Responsibilities:                                        │  │  │
│  │  │  • UI rendering and updates                              │  │  │
│  │  │  • Event handling (button clicks, etc.)                  │  │  │
│  │  │  • Data binding notifications                            │  │  │
│  │  │  • XAML element manipulation                             │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                           ↕                                    │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  IUIThreadService (Abstraction)                          │  │  │
│  │  │                                                           │  │  │
│  │  │  • Task InvokeAsync(Action action)                       │  │  │
│  │  │  • Task<T> InvokeAsync<T>(Func<T> function)              │  │  │
│  │  │  • bool IsOnUIThread { get; }                            │  │  │
│  │  │                                                           │  │  │
│  │  │  Implementation: AvaloniaUIThreadService                 │  │  │
│  │  │  • Checks if already on UI thread                        │  │  │
│  │  │  • Marshals if needed via Dispatcher                     │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                   BACKGROUND THREADS                           │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  ThreadPool (via Task.Run)                               │  │  │
│  │  │                                                           │  │  │
│  │  │  Responsibilities:                                        │  │  │
│  │  │  • I/O operations (file, network)                        │  │  │
│  │  │  • Heavy computations                                    │  │  │
│  │  │  • Async service operations                              │  │  │
│  │  │  • Profile persistence                                   │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                           ↕                                    │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  SemaphoreSlim (Synchronization)                         │  │  │
│  │  │                                                           │  │  │
│  │  │  Usage Pattern:                                          │  │  │
│  │  │  • Protects shared profile collections                   │  │  │
│  │  │  • Ensures atomic operations                             │  │  │
│  │  │  • Internal method pattern prevents deadlocks            │  │  │
│  │  │                                                           │  │  │
│  │  │  Public API:                                             │  │  │
│  │  │    await _semaphore.WaitAsync(ct);                       │  │  │
│  │  │    try { return await InternalAsync(); }                 │  │  │
│  │  │    finally { _semaphore.Release(); }                     │  │  │
│  │  │                                                           │  │  │
│  │  │  Internal Helper:                                        │  │  │
│  │  │    // NO semaphore - assumes already held                │  │  │
│  │  │    private async Task InternalAsync() { ... }            │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │             THREAD-SAFE COLLECTIONS                            │  │
│  │  • ConcurrentQueue<LogModel> (log data store)                 │  │
│  │  • Protected List<T> with SemaphoreSlim (profile managers)    │  │
│  │  • ObservableCollection<T> (UI bindings - UI thread only)     │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                 ASYNC/AWAIT PATTERNS                           │  │
│  │                                                                 │  │
│  │  Library Code (Services, Repositories):                        │  │
│  │    await operation.ConfigureAwait(false)                       │  │
│  │    • Doesn't capture synchronization context                   │  │
│  │    • Better performance                                        │  │
│  │    • Prevents deadlocks                                        │  │
│  │                                                                 │  │
│  │  UI Code (ViewModels with UI updates):                         │  │
│  │    await _uiThreadService.InvokeAsync(() => UpdateUI())        │  │
│  │    • Explicit UI thread marshaling                             │  │
│  │    • Clear intent                                              │  │
│  │                                                                 │  │
│  │  Reactive Subscriptions:                                       │  │
│  │    this.WhenAnyValue(x => x.Prop)                              │  │
│  │        .ObserveOn(RxApp.MainThreadScheduler)                   │  │
│  │        .Subscribe(...)                                         │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

CRITICAL PATTERNS DOCUMENTED IN systemPatterns.md:

1. Internal Method Pattern (prevents semaphore deadlocks)
   ✓ Public methods acquire semaphore
   ✓ Internal helpers assume semaphore held
   ✓ No nested semaphore acquisition

2. Debug Logging for Async Operations
   ✓ Log "🔒 Waiting for semaphore..."
   ✓ Log "🔓 Semaphore acquired"
   ✓ Log "🔓 Releasing semaphore..."
   ✓ Helps track async flow timeline

3. ConfigureAwait(false) in Library Code
   ✓ All service layer uses ConfigureAwait(false)
   ✓ Prevents SynchronizationContext capture
   ✓ Better performance and safety
```

---

## 5. Logging Infrastructure Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      LOGGING INFRASTRUCTURE                              │
│                                                                           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              Microsoft.Extensions.Logging (MEL)                     │ │
│  │                      ILoggerFactory                                 │ │
│  │                           │                                         │ │
│  │                           ├─→ Console Provider                      │ │
│  │                           ├─→ Debug Provider                        │ │
│  │                           └─→ DataStore Provider (Custom) ✨        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                  ↓                                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              DataStoreLoggerProvider (Custom)                       │ │
│  │                                                                      │ │
│  │  • Implements ILoggerProvider (MEL interface)                       │ │
│  │  • Creates DataStoreLogger instances per category                   │ │
│  │  • Manages lifecycle and disposal                                   │ │
│  │  • Configurable via DataStoreLoggerConfiguration                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                  ↓                                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                   DataStoreLogger (Custom)                          │ │
│  │                                                                      │ │
│  │  • Implements ILogger (MEL interface)                               │ │
│  │  • Formats log entries with category, level, message                │ │
│  │  • Supports structured logging (scopes, properties)                 │ │
│  │  • Writes to ILogDataStore                                          │ │
│  │  • Thread-safe via internal locking                                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                  ↓                                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              ILogDataStore → LogDataStore                           │ │
│  │                                                                      │ │
│  │  Storage:                                                           │ │
│  │    • ConcurrentQueue<LogModel> _entries                            │ │
│  │    • Thread-safe lock-free queue                                    │ │
│  │    • Bounded size (MaxEntries = 10,000 default)                     │ │
│  │    • Circular buffer behavior                                       │ │
│  │                                                                      │ │
│  │  Operations:                                                        │ │
│  │    • AddEntry(LogModel) - adds and maintains size                   │ │
│  │    • GetEntries() - returns snapshot                                │ │
│  │    • Clear() - removes all entries                                  │ │
│  │    • Observable<LogModel> Entries - real-time stream                │ │
│  │                                                                      │ │
│  │  Features:                                                          │ │
│  │    ✓ Automatic old entry removal (FIFO)                             │ │
│  │    ✓ No unbounded memory growth                                     │ │
│  │    ✓ High performance (concurrent queue)                            │ │
│  │    ✓ Observable for reactive UI updates                             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                  ↓                                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                    LOG VIEWER (UI Integration)                      │ │
│  │                                                                      │ │
│  │  LogViewerViewModel:                                                │ │
│  │    • Subscribes to ILogDataStore.Entries observable                 │ │
│  │    • Receives real-time log updates                                 │ │
│  │    • Marshals to UI thread via IUIThreadService                     │ │
│  │    • Filters by level, category, search text                        │ │
│  │    • Exports logs to file (JSON, CSV, TXT)                          │ │
│  │                                                                      │ │
│  │  LogViewerView:                                                     │ │
│  │    • DataGrid with virtualization                                   │ │
│  │    • Color-coded log levels                                         │ │
│  │    • Real-time auto-scroll option                                   │ │
│  │    • Column visibility toggles                                      │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘

USAGE PATTERN:

// 1. Service registration
services.AddS7ToolsLogging(options =>
{
    options.MaxEntries = 10000;  // Circular buffer size
    options.MinLevel = LogLevel.Information;
});

// 2. Service usage (constructor injection)
public class SomeService
{
    private readonly ILogger<SomeService> _logger;

    public SomeService(ILogger<SomeService> logger)
    {
        _logger = logger;
    }

    public async Task DoWorkAsync()
    {
        _logger.LogInformation("🔧 Starting work");

        try
        {
            var result = await PerformAsync();
            _logger.LogInformation("✓ Work completed: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Work failed");
            throw;
        }
    }
}

// 3. UI viewing
The LogViewer automatically displays all logs in real-time
Users can filter, search, and export logs

BENEFITS:
✓ Real-time UI integration
✓ Bounded memory (circular buffer)
✓ Thread-safe (ConcurrentQueue)
✓ Structured logging support
✓ Observable for reactive updates
✓ Multiple output formats (JSON, CSV, TXT)
✓ Integrates with standard MEL
```

---

## 6. Service Registration and Dependency Injection

```
┌───────────────────────────────────────────────────────────────────┐
│                 DEPENDENCY INJECTION ARCHITECTURE                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    Program.cs (Entry Point)                  │  │
│  │                                                               │  │
│  │  var builder = new ServiceCollection();                      │  │
│  │                                                               │  │
│  │  // ✓ Modular registration via extension methods             │  │
│  │  builder.AddS7ToolsServices(options =>                       │  │
│  │  {                                                            │  │
│  │      options.MaxEntries = 10000;                             │  │
│  │  });                                                          │  │
│  │                                                               │  │
│  │  var services = builder.BuildServiceProvider();              │  │
│  │                                                               │  │
│  │  // ✓ Initialize services that need async setup              │  │
│  │  await services.InitializeS7ToolsServicesAsync();            │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │        ServiceCollectionExtensions (Central Registry)        │  │
│  │                                                               │  │
│  │  AddS7ToolsServices()                                        │  │
│  │    ├─→ AddS7ToolsFoundationServices()                       │  │
│  │    │     ├─ IUIThreadService → AvaloniaUIThreadService      │  │
│  │    │     ├─ ILayoutService → LayoutService                  │  │
│  │    │     ├─ IThemeService → ThemeService                    │  │
│  │    │     ├─ IDialogService → DialogService                  │  │
│  │    │     ├─ ISerialPortProfileService → ...                 │  │
│  │    │     ├─ ISocatProfileService → ...                      │  │
│  │    │     └─ IPowerSupplyProfileService → ...                │  │
│  │    │                                                          │  │
│  │    ├─→ AddS7ToolsAdvancedServices()                         │  │
│  │    │     ├─ ICommandDispatcher → CommandDispatcher          │  │
│  │    │     ├─ IViewModelFactory → EnhancedViewModelFactory    │  │
│  │    │     ├─ IResourceManager → ResourceManager              │  │
│  │    │     └─ IValidationService → ValidationService          │  │
│  │    │                                                          │  │
│  │    ├─→ AddS7ToolsLogging(options)                           │  │
│  │    │     ├─ ILoggerFactory → Microsoft.Extensions.Logging   │  │
│  │    │     ├─ ILogDataStore → LogDataStore                    │  │
│  │    │     └─ DataStoreLoggerProvider                         │  │
│  │    │                                                          │  │
│  │    └─→ AddS7ToolsViewModels()                               │  │
│  │          ├─ MainWindowViewModel                              │  │
│  │          ├─ NavigationViewModel                              │  │
│  │          ├─ LogViewerViewModel                               │  │
│  │          └─ Profile ViewModels (Serial, Socat, Power)        │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │              Service Lifetimes & Best Practices              │  │
│  │                                                               │  │
│  │  Singleton (single instance for app lifetime):               │  │
│  │    ✓ UI services (Layout, Theme, ActivityBar)               │  │
│  │    ✓ Profile services (shared state)                        │  │
│  │    ✓ Logging infrastructure                                 │  │
│  │    ✓ Configuration services                                 │  │
│  │    ✓ Main ViewModels (navigation state)                     │  │
│  │                                                               │  │
│  │  Transient (new instance per request):                       │  │
│  │    ✓ Dialog services (short-lived)                          │  │
│  │    ✓ Dialog ViewModels                                      │  │
│  │    ✓ Profile dialog ViewModels                              │  │
│  │    ✓ Export services                                        │  │
│  │                                                               │  │
│  │  Scoped (not used in desktop app):                           │  │
│  │    ✗ Not applicable (no request scope)                       │  │
│  │                                                               │  │
│  │  Registration Pattern:                                       │  │
│  │    services.TryAddSingleton<IService, Implementation>();    │  │
│  │              ↑                                                │  │
│  │              TryAdd prevents duplicate registration           │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                Service Initialization                         │  │
│  │                                                               │  │
│  │  InitializeS7ToolsServicesAsync(IServiceProvider):           │  │
│  │    • Loads layout configuration                              │  │
│  │    • Loads theme configuration                               │  │
│  │    • Initializes profile storage (background)                │  │
│  │    • Creates default profiles if needed                      │  │
│  │    • Handles errors gracefully                               │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                   Service Consumption                         │  │
│  │                                                               │  │
│  │  // Constructor injection (preferred)                        │  │
│  │  public class MainWindowViewModel                            │  │
│  │  {                                                            │  │
│  │      private readonly IDialogService _dialogService;         │  │
│  │      private readonly ILogger _logger;                       │  │
│  │                                                               │  │
│  │      public MainWindowViewModel(                             │  │
│  │          IDialogService dialogService,                       │  │
│  │          ILogger<MainWindowViewModel> logger)                │  │
│  │      {                                                        │  │
│  │          _dialogService = dialogService;                     │  │
│  │          _logger = logger;                                   │  │
│  │      }                                                        │  │
│  │  }                                                            │  │
│  │                                                               │  │
│  │  // Service locator (for special cases only)                 │  │
│  │  var service = serviceProvider.GetRequiredService<IService>();│  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

BENEFITS:
✓ Central registration (ServiceCollectionExtensions only)
✓ Modular configuration (foundation, advanced, logging, ViewModels)
✓ No registration in Program.cs (follows guideline)
✓ Proper lifetimes (Singleton/Transient)
✓ TryAdd pattern prevents duplicates
✓ Constructor injection throughout
✓ Interface-based (testability and flexibility)
```

---

## Summary

This architecture demonstrates:

1. **Clean Architecture** - Proper dependency flow, domain isolation
2. **MVVM Pattern** - Clear separation with ReactiveUI
3. **DDD Principles** - Rich domain models, business rules, repositories
4. **Thread Safety** - SemaphoreSlim, concurrent collections, IUIThreadService
5. **Custom Logging** - MEL integration with real-time UI
6. **Dependency Injection** - Central registration, proper lifetimes
7. **Unified Profile Management** - Zero duplication via base class
8. **Async/Await Best Practices** - ConfigureAwait, cancellation, internal methods

**Result:** A production-ready, maintainable, and testable codebase with excellent separation of concerns and adherence to .NET best practices.

## Related Documentation

- [Index](../INDEX.md)
- [_Index](_index.md)
- [Clean Architecture](clean-architecture.md)
- [Mvvm Patterns](mvvm-patterns.md)
- [Overview](overview.md)
- [System Patterns](../patterns/system-patterns.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
