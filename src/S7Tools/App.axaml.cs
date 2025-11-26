using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Resources;
using S7Tools.Extensions;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Dialogs;
using S7Tools.Views;
using S7Tools.Views.Dialogs;
using S7Tools.Views.Layout;

namespace S7Tools;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes the application.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Initialize ResourceManager for UIStrings
        try
        {
            IResourceManager resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
            UIStrings.ResourceManager = resourceManager;

            ILogger<App>? logger = _serviceProvider.GetService<ILogger<App>>();
            logger?.LogDebug("UIStrings ResourceManager initialized successfully");
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            ILogger<App>? logger = _serviceProvider.GetService<ILogger<App>>();
            logger?.LogError(ex, "Failed to initialize UIStrings ResourceManager");
        }
    }

    /// <summary>
    /// Called when the Avalonia framework initialization is completed.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Get required services
                IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();

                // CRITICAL: Initialize path services SYNCHRONOUSLY to ensure proper resource structure
                // This must happen before any other services try to access files/folders
                try
                {
                    logger.LogInformation("🔄 Starting synchronous path and settings initialization...");
                    InitializePathAndSettingsSync(logger);
                    logger.LogInformation("✅ Path and settings initialization completed successfully");

                    // Now that foundational services are ready, initialize profile services asynchronously in parallel
                    logger.LogInformation("🚀 Starting async profile services initialization in background...");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);
                            logger.LogInformation("✅ Profile services initialization completed successfully");

                            // Start the JobScheduler after profile services are initialized
                            try
                            {
                                logger.LogInformation("🚀 Starting JobScheduler...");
                                Core.Services.Interfaces.IJobScheduler? jobScheduler = _serviceProvider.GetService<Core.Services.Interfaces.IJobScheduler>();
                                if (jobScheduler != null)
                                {
                                    await jobScheduler.StartAsync(System.Threading.CancellationToken.None).ConfigureAwait(false);
                                    logger.LogInformation("✅ JobScheduler started successfully");
                                }
                                else
                                {
                                    logger.LogWarning("⚠️ JobScheduler service not found in DI container");
                                }
                            }
                            catch (Exception schedulerEx)
                            {
                                logger.LogError(schedulerEx, "❌ Failed to start JobScheduler");
                            }

                            // Start the TaskScheduler (EnhancedTaskScheduler) for task execution
                            try
                            {
                                logger.LogInformation("🚀 Starting TaskScheduler...");
                                Core.Services.Interfaces.ITaskScheduler? taskScheduler = _serviceProvider.GetService<Core.Services.Interfaces.ITaskScheduler>();
                                if (taskScheduler != null)
                                {
                                    await taskScheduler.StartAsync(System.Threading.CancellationToken.None).ConfigureAwait(false);
                                    logger.LogInformation("✅ TaskScheduler started successfully");
                                }
                                else
                                {
                                    logger.LogWarning("⚠️ TaskScheduler service not found in DI container");
                                }
                            }
                            catch (Exception taskSchedulerEx)
                            {
                                logger.LogError(taskSchedulerEx, "❌ Failed to start TaskScheduler");
                            }
                        }
                        catch (Exception profileEx)
                        {
                            logger.LogError(profileEx, "❌ Profile services initialization failed");
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ CRITICAL: Path and settings initialization failed - application may not function correctly");
                    // Continue anyway to allow user to see error in UI
                }

                logger.LogDebug("Registering dialog interaction handlers");

                // Register interaction handlers on the UI thread with proper window context
                RegisterInteractionHandlers(dialogService, logger);

                // Create and set main window
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                // Application exit handled - settings are saved automatically by ApplicationSettingsService
                desktop.Exit += (s, e) =>
                {
                    ILogger<App>? exitLogger = _serviceProvider.GetService<ILogger<App>>();
                    exitLogger?.LogInformation("Application exiting");
                };

                logger.LogInformation("Application initialization completed successfully");
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                ILogger<App>? logger = _serviceProvider.GetService<ILogger<App>>();
                logger?.LogError(ex, "Error during application initialization");

                // Fallback: create main window without dialog handlers
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            }
        }

        base.OnFrameworkInitializationCompleted();

        // Global exception handler
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            ILogger<App>? logger = _serviceProvider.GetService<ILogger<App>>();
            logger?.LogError(e.ExceptionObject as Exception, "Unhandled application exception");
        };
    }

    /// <summary>
    /// Registers interaction handlers for dialogs with proper UI thread context.
    /// </summary>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="logger">The logger instance.</param>
    private void RegisterInteractionHandlers(IDialogService dialogService, ILogger logger)
    {
        try
        {
            // Handle confirmation dialogs
            dialogService.ShowConfirmation.RegisterHandler(async interaction =>
            {
                try
                {
                    logger.LogDebug("Showing confirmation dialog: {Title} - {Message}",
                        interaction.Input.Title, interaction.Input.Message);

                    DialogResult<bool> result = await ShowDialogAsync<bool>(
                        () => new ConfirmationDialog
                        {
                            DataContext = new ConfirmationDialogViewModel(interaction.Input.Title, interaction.Input.Message)
                        },
                        logger,
                        "confirmation dialog");

                    if (result.IsSuccess)
                    {
                        interaction.SetOutput(result.Value);
                        logger.LogDebug("Confirmation dialog result: {Result}", result.Value);
                    }
                    else
                    {
                        logger.LogWarning("Confirmation dialog failed or returned unexpected result, defaulting to false");
                        interaction.SetOutput(false);

                        // Notify user of critical dialog failure
                        await ShowCriticalErrorNotificationAsync(
                            "Dialog Error",
                            "Failed to show confirmation dialog. The operation has been cancelled.",
                            logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing confirmation dialog");
                    interaction.SetOutput(false);

                    // Notify user of critical dialog failure
                    await ShowCriticalErrorNotificationAsync(
                        "Critical Dialog Error",
                        $"A critical error occurred while showing a dialog: {ex.Message}",
                        logger);
                }
            });

            // Handle error dialogs
            dialogService.ShowError.RegisterHandler(async interaction =>
            {
                try
                {
                    logger.LogDebug("Showing error dialog: {Title} - {Message}",
                        interaction.Input.Title, interaction.Input.Message);

                    DialogResult<bool> result = await ShowDialogAsync<bool>(
                        () => new ConfirmationDialog
                        {
                            DataContext = new ConfirmationDialogViewModel(interaction.Input.Title, interaction.Input.Message, false)
                        },
                        logger,
                        "error dialog");

                    if (result.IsSuccess)
                    {
                        logger.LogDebug("Error dialog shown successfully");
                    }
                    else
                    {
                        logger.LogWarning("Error dialog failed to show");
                        // For error dialogs, we still complete the interaction even if it fails
                        // since the error was already logged
                    }

                    interaction.SetOutput(Unit.Default);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing error dialog");
                    interaction.SetOutput(Unit.Default);

                    // For error dialogs, we avoid recursive error notifications
                    // Just log the failure
                    logger.LogCritical("Failed to show error dialog - this indicates a severe UI issue");
                }
            });

            // Handle input dialogs
            dialogService.ShowInput.RegisterHandler(async interaction =>
            {
                try
                {
                    logger.LogDebug("Showing input dialog: {Title} - {Message}",
                        interaction.Input.Title, interaction.Input.Message);

                    // Create the view model first
                    var viewModel = new InputDialogViewModel(interaction.Input);

                    DialogResult<InputResult?> result = await ShowDialogAsync<InputResult?>(
                        () => new InputDialog
                        {
                            DataContext = viewModel
                        },
                        logger,
                        "input dialog");

                    if (result.IsSuccess && result.Value != null)
                    {
                        interaction.SetOutput(result.Value);
                        logger.LogDebug("Input dialog result: Cancelled={IsCancelled}, Value={Value}",
                            result.Value.IsCancelled, result.Value.Value ?? string.Empty);
                    }
                    else if (result.IsSuccess && result.Value == null)
                    {
                        // If dialog returned null, use the ViewModel's result
                        InputResult viewModelResult = viewModel.Result;
                        interaction.SetOutput(viewModelResult);
                        logger.LogDebug("Input dialog returned null, using ViewModel result: Cancelled={IsCancelled}",
                            viewModelResult.IsCancelled);
                    }
                    else
                    {
                        logger.LogWarning("Input dialog failed, returning cancelled result");
                        interaction.SetOutput(InputResult.Cancelled());

                        // Notify user of critical dialog failure
                        await ShowCriticalErrorNotificationAsync(
                            "Input Dialog Error",
                            "Failed to show input dialog. The operation has been cancelled.",
                            logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing input dialog");
                    interaction.SetOutput(InputResult.Cancelled());

                    // Notify user of critical dialog failure
                    await ShowCriticalErrorNotificationAsync(
                        "Critical Input Dialog Error",
                        $"A critical error occurred while showing an input dialog: {ex.Message}",
                        logger);
                }
            });

            // Handle job selection dialogs
            dialogService.ShowJobSelection.RegisterHandler(async interaction =>
            {
                try
                {
                    logger.LogDebug("Showing job selection dialog");

                    // Get job manager to fetch available jobs
                    var jobManager = _serviceProvider.GetService<S7Tools.Core.Services.Interfaces.IJobManager>();
                    if (jobManager == null)
                    {
                        logger.LogError("IJobManager service not available for job selection dialog");
                        interaction.SetOutput(null);
                        return;
                    }

                    // Fetch available jobs
                    var jobs = await jobManager.GetAllAsync();
                    var jobList = new System.Collections.ObjectModel.ObservableCollection<Core.Models.Jobs.JobProfile>(jobs);

                    // Create the view model
                    var viewModel = new JobSelectionDialogViewModel(jobList);

                    DialogResult<Core.Models.Jobs.JobProfile?> result = await ShowDialogAsync<Core.Models.Jobs.JobProfile?>(
                        () => new JobSelectionDialog
                        {
                            DataContext = viewModel
                        },
                        logger,
                        "job selection dialog");

                    if (result.IsSuccess)
                    {
                        interaction.SetOutput(result.Value);
                        logger.LogDebug("Job selection dialog result: {JobName}",
                            result.Value?.Name ?? "Cancelled");
                    }
                    else
                    {
                        logger.LogWarning("Job selection dialog failed, returning null");
                        interaction.SetOutput(null);

                        // Notify user of critical dialog failure
                        await ShowCriticalErrorNotificationAsync(
                            "Job Selection Dialog Error",
                            "Failed to show job selection dialog. The operation has been cancelled.",
                            logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing job selection dialog");
                    interaction.SetOutput(null);

                    // Notify user of critical dialog failure
                    await ShowCriticalErrorNotificationAsync(
                        "Critical Job Selection Dialog Error",
                        $"A critical error occurred while showing the job selection dialog: {ex.Message}",
                        logger);
                }
            });

            logger.LogInformation("Dialog interaction handlers registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register dialog interaction handlers");
        }
    }

    /// <summary>
    /// Helper method to show dialogs with proper error handling and main window context.
    /// </summary>
    /// <typeparam name="T">The dialog result type.</typeparam>
    /// <param name="dialogFactory">Factory function to create the dialog.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="dialogType">Type of dialog for logging purposes.</param>
    /// <returns>A result indicating success/failure and the dialog result if successful.</returns>
    private async Task<DialogResult<T>> ShowDialogAsync<T>(Func<Window> dialogFactory, ILogger logger, string dialogType)
    {
        try
        {
            Window? mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow == null)
            {
                logger.LogWarning("Main window not available for {DialogType}", dialogType);
                return DialogResult<T>.Failure("Main window not available");
            }

            Window dialog = dialogFactory();
            T? result = await dialog.ShowDialog<T>(mainWindow);
            return DialogResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error showing {DialogType}", dialogType);
            return DialogResult<T>.Failure($"Dialog error: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a critical error notification to the user using system notification or fallback message.
    /// </summary>
    /// <param name="title">Error title.</param>
    /// <param name="message">Error message.</param>
    /// <param name="logger">Logger instance.</param>
    private Task ShowCriticalErrorNotificationAsync(string title, string message, ILogger logger)
    {
        try
        {
            logger.LogError("Critical UI Error: {Title} - {Message}", title, message);

            // Try to show a simple message box as fallback
            Window? mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var errorDialog = new ConfirmationDialog
                {
                    DataContext = new ConfirmationDialogViewModel(title, message, false)
                };

                // Fire and forget - don't await to avoid potential recursion
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await errorDialog.ShowDialog<bool>(mainWindow);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to show critical error dialog");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show critical error notification");
        }

        // No awaited work in this method; return a completed task.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes path services and settings synchronously to ensure proper startup order.
    ///
    /// ARCHITECTURAL DECISION: Synchronous Initialization Pattern
    /// =========================================================
    ///
    /// This method INTENTIONALLY blocks the UI thread during initialization. This is a deliberate
    /// architectural decision based on strict service dependency requirements:
    ///
    /// Service Dependency Chain:
    /// 1. IPathService - Creates all required directories (Resources/, Profiles/, Logs/, etc.)
    /// 2. IResourceManagerService - Creates default resource files (requires directories from #1)
    /// 3. IApplicationSettingsService - Loads settings from files (requires resources from #2)
    /// 4. FileLogWriter - Monitors DataStore for file logging (requires paths from #1)
    ///
    /// Why Synchronous?
    /// ----------------
    /// - Profile managers, job services, and UI components depend on paths existing BEFORE they initialize
    /// - Settings must be loaded BEFORE any service tries to read configuration
    /// - Resource files must exist BEFORE any service tries to access them
    /// - If we initialize asynchronously, race conditions occur where services fail because paths/files don't exist yet
    ///
    /// Why Not Task.Run()?
    /// -------------------
    /// - Task.Run() would still block initialization, just on a thread pool thread
    /// - The UI window cannot be shown until these services are ready
    /// - Moving to Task.Run() adds complexity without solving the fundamental requirement:
    ///   "These services MUST be ready before the application can function"
    ///
    /// Performance Impact:
    /// -------------------
    /// - Typical initialization time: 50-200ms (file I/O + JSON deserialization)
    /// - User sees no window during this time (acceptable for startup)
    /// - Profile services initialize asynchronously in background after this completes
    ///
    /// Alternative Considered: Splash Screen
    /// --------------------------------------
    /// A splash screen with async initialization was considered, but rejected because:
    /// - Adds complexity for minimal benefit (initialization is fast)
    /// - Still requires blocking before showing main window
    /// - Doesn't solve the fundamental dependency chain
    ///
    /// Future Optimization:
    /// --------------------
    /// If startup time becomes problematic (>500ms), consider:
    /// - Lazy loading of non-critical resources
    /// - Splash screen with progress indicator
    /// - Parallel initialization of independent services (requires careful dependency analysis)
    ///
    /// Related Patterns:
    /// -----------------
    /// - See systemPatterns.md: "Internal Method Pattern" for proper async handling after initialization
    /// - See SEMAPHORE_DEADLOCK_FIXES_COMPLETE.md for threading best practices
    /// </summary>
    /// <param name="logger">Logger instance for tracking initialization</param>
    private void InitializePathAndSettingsSync(ILogger logger)
    {
        logger.LogInformation("🔄 Initializing path services and application settings synchronously");

        try
        {
            // STEP 1: Initialize path service and create folder structure
            logger.LogDebug("Step 1: Initializing path service");
            S7Tools.Core.Interfaces.Services.IPathService? pathService = _serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IPathService>();
            if (pathService != null)
            {
                // Initialize paths synchronously (this creates folder structure)
                Task<PathConfiguration> pathTask = pathService.InitializeAsync();
                PathConfiguration pathConfig = pathTask.GetAwaiter().GetResult(); // Force synchronous execution
                logger.LogInformation("✅ Path service initialized - Base directory: {BaseDirectory}", pathConfig.BaseDirectory);
            }
            else
            {
                logger.LogError("❌ IPathService not found in service provider");
                return;
            }

            // STEP 2: Initialize resource manager to create missing files
            logger.LogDebug("Step 2: Initializing resource manager");
            S7Tools.Core.Interfaces.Services.IResourceManagerService? resourceService = _serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IResourceManagerService>();
            if (resourceService != null)
            {
                Task<ResourceInitializationResult> resourceTask = resourceService.InitializeResourcesAsync();
                ResourceInitializationResult result = resourceTask.GetAwaiter().GetResult(); // Force synchronous execution
                if (result.Success)
                {
                    logger.LogInformation("✅ Resource manager initialized - Created {ResourceCount} resources", result.CreatedResources.Count);
                }
                else
                {
                    logger.LogWarning("⚠️ Resource manager completed with {ErrorCount} errors", result.Errors.Count);
                    foreach (string error in result.Errors)
                    {
                        logger.LogWarning("Resource error: {Error}", error);
                    }
                }
            }
            else
            {
                logger.LogError("❌ IResourceManagerService not found in service provider");
            }

            // STEP 3: Initialize application settings service and load configuration
            logger.LogDebug("Step 3: Loading application settings");
            S7Tools.Core.Interfaces.Services.IApplicationSettingsService? settingsService = _serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
            if (settingsService != null)
            {
                Task<Core.Models.Configuration.ApplicationSettings> settingsTask = settingsService.LoadSettingsAsync();
                Core.Models.Configuration.ApplicationSettings settings = settingsTask.GetAwaiter().GetResult(); // Force synchronous execution
                logger.LogInformation("✅ Application settings loaded - {EffectiveCount} effective settings, {UserCount} user overrides",
                    settings.EffectiveSettings.Count, settings.UserSettings.Count);
            }
            else
            {
                logger.LogError("❌ IApplicationSettingsService not found in service provider");
            }

            // STEP 4: Initialize file logging service to start monitoring logs
            logger.LogDebug("Step 4: Initializing file logging service");
            Services.FileLogWriter? fileLogWriter = _serviceProvider.GetService<Services.FileLogWriter>();
            if (fileLogWriter != null)
            {
                logger.LogInformation("✅ File logging service initialized and monitoring DataStore");
            }
            else
            {
                logger.LogWarning("⚠️ FileLogWriter not found - file logging will not be available");
            }

            logger.LogInformation("🎉 Synchronous initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "💥 Critical failure during synchronous initialization");
            throw; // Re-throw to let caller handle
        }
    }

    /// <summary>
    /// Helper class to encapsulate dialog operation results.
    /// </summary>
    /// <typeparam name="T">The type of the dialog result.</typeparam>
    private class DialogResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }

        private DialogResult(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static DialogResult<T> Success(T value) => new(true, value, null);
        public static DialogResult<T> Failure(string error) => new(false, default, error);
    }
}
