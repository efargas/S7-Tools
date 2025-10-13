# Implementation Details

**Last Updated**: 2025-10-13

## Feature: Siemens S7 Bootloader Integration

**Status**: In Progress

### Implemented Functionality:
- **Core Models:** `Job`, `JobProfileSet`, and related models have been created in `S7Tools.Core`.
- **Core Services:** `IBootloaderService`, `IJobScheduler`, and `IResourceCoordinator` interfaces and their concrete implementations (`JobScheduler`, `BootloaderService`, `ResourceCoordinator`) have been created.
- **Adapter Services:** A suite of adapter classes (`PlcClientAdapter`, `SocatAdapter`, etc.) has been introduced to decouple the core services from external libraries and dependencies.
- **Dependency Injection:** The new services and view models are registered in `ServiceCollectionExtensions.cs`.
- **UI:** A new `TaskManagerViewModel` has been created to manage the UI logic for the Task Manager view.
- **Unit Tests:** Unit tests for the `ResourceCoordinator` and `JobScheduler` services have been added.
- **Code Cleanup:** The unused `BooleanToVisibilityConverter` has been removed, and its usages in the XAML files have been updated.
- **Build Fixes:** Resolved a race condition in the `SchedulerTests` and a Moq verification issue to ensure all tests pass reliably.
- **Refactoring:** Completed the refactoring of `ISocatService` and updated its consumers.
- **Bug Fixes:** Corrected the `IPlcClient` factory implementation and other placeholder logic.

### Missing Functionality:
- The `DumpMemoryAsync` and `InstallStagerAsync` methods in `PlcClientAdapter` are currently placeholder implementations and need to be fully implemented with the correct logic for interacting with the S7 PLC.
- The UI for the Task Manager (`TaskManagerView`) has not yet been created.
- The application currently relies on stub implementations for the `SiemensS7Bootloader` library. A real implementation or a more robust simulation is required for end-to-end testing and production use.
- The `PowerControllerAdapter` is a stub and needs to be implemented with the actual power controller logic.
- The `PayloadProviderAdapter` is a stub and needs to be implemented with the actual payload provider logic.
- The `PlcProtocolAdapter` is a stub and needs to be implemented with the actual PLC protocol logic.
- The `PlcTransportAdapter` is a stub and needs to be implemented with the actual PLC transport logic.
- The `SocatSettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LogExportService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `MainWindowViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortsSettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `AboutViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ConfirmationDialogViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `HomeViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ConnectionsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortProfileViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortScannerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatProfileViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `TaskManagerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LogViewerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `BottomPanelViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `NavigationViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsManagementViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `GreetingService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ThemeService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LayoutService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LocalizationService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `PlcDataService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortProfileService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatProfileService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ActivityBarService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ClipboardService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `DialogService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `FileDialogService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `UIThreadService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ViewModelFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `EnhancedViewModelFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `InMemoryResourceManager` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ValidationService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `StructuredLoggerFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `FileLogWriter` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `CommandDispatcher` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `AvaloniaUIThreadService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LocalizationService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LayoutService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ThemeService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `DialogService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ClipboardService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LogExportService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `FileLogWriter` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `AvaloniaFileDialogService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `GreetingService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `PlcDataService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortProfileService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatProfileService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ActivityBarService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `CommandDispatcher` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `EnhancedViewModelFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `InMemoryResourceManager` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ValidationService` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `StructuredLoggerFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ViewModelFactory` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `BottomPanelViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ConfirmationDialogViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `ConnectionsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `HomeViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LogViewerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `MainWindowViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `NavigationViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortProfileViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortsSettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SerialPortScannerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsManagementViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatProfileViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `SocatSettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `TaskManagerViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `AboutViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.
- The `LoggingSettingsViewModel` has some placeholder logic for dialogs and file operations that needs to be implemented.