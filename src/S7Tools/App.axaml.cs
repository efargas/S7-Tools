using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Resources;
using S7Tools.Extensions;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Dialogs;
using S7Tools.Views.Dialogs;
using S7Tools.Views.Layout;
using Avalonia.Styling;

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
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();

                // Show Splash Screen
                logger.LogInformation("🚀 Launching Splash Screen...");

                // Resolve correct logger for ViewModel
                var splashLogger = _serviceProvider.GetRequiredService<ILogger<ViewModels.Layout.SplashScreenViewModel>>();
                var splashViewModel = new ViewModels.Layout.SplashScreenViewModel(_serviceProvider, splashLogger);

                var splashScreen = new Views.Layout.SplashScreenWindow
                {
                    DataContext = splashViewModel
                };

                desktop.MainWindow = splashScreen;
                splashScreen.Show();

                // Run initialization in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 1. Critical Base Initialization (previously synchronous)
                        await splashViewModel.InitializeAsync();

                        // 2. Profile Services Initialization
                        logger.LogInformation("🚀 Starting async profile services initialization...");
                        await _serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);

                        // 3. Start Schedulers
                        await StartSchedulersAsync(logger);

                        // 4. Set Initial Theme and Subscribe to Changes
                        var settingsService = _serviceProvider.GetRequiredService<IApplicationSettingsService>();
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyThemeVariant(settingsService.GetSetting("ui.theme", "System")));
                        
                        settingsService.SettingsChanged += (s, e) =>
                        {
                            if (e.Key.Equals("ui.theme", StringComparison.OrdinalIgnoreCase))
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyThemeVariant(e.NewValue?.ToString() ?? "System"));
                            }
                        };

                        // 5. Switch to Main Window on UI Thread
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            logger.LogInformation("✅ Initialization complete. Switching to Main Window.");

                            // Create Main Window
                            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                            desktop.MainWindow = mainWindow;
                            mainWindow.Show();

                            // Close Splash Screen
                            splashScreen.Close();

                            // 5. Post-Startup: Register Interaction Handlers (requires MainWindow to be active context)
                            // We need the IDialogService here.
                            var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
                            RegisterInteractionHandlers(dialogService, logger);
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "❌ Critical application startup failure");
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            // Show fatal error on splash screen if possible, or message box
                            splashViewModel.StatusText = "CRITICAL ERROR: " + ex.Message;
                            // Keep splash screen open to show error
                        });
                    }
                });

                // Application exit handled - settings are saved automatically by ApplicationSettingsService
                desktop.Exit += (s, e) =>
                {
                    ILogger<App>? exitLogger = _serviceProvider.GetService<ILogger<App>>();
                    exitLogger?.LogInformation("Application exiting");
                };
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
    /// Applies the specified theme string to the Avalonia Application.
    /// </summary>
    private void ApplyThemeVariant(string theme)
    {
        RequestedThemeVariant = theme.ToLowerInvariant() switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default // System default
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
    /// Helper method to show dialogs with proper error handling and active window context.
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
            var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

            // Get the active window as owner if possible, otherwise fall back to MainWindow
            Window? owner = desktop?.Windows.FirstOrDefault(w => w.IsActive) ?? desktop?.MainWindow;

            if (owner == null)
            {
                logger.LogWarning("No owner window available for {DialogType}", dialogType);
                return DialogResult<T>.Failure("No owner window available");
            }

            Window dialog = dialogFactory();
            T? result = await dialog.ShowDialog<T>(owner);
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



    private async Task StartSchedulersAsync(ILogger logger)
    {
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
