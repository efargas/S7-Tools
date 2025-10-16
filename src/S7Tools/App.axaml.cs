using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Resources;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.Views;

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
            var resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
            UIStrings.ResourceManager = resourceManager;

            var logger = _serviceProvider.GetService<ILogger<App>>();
            logger?.LogDebug("UIStrings ResourceManager initialized successfully");
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            var logger = _serviceProvider.GetService<ILogger<App>>();
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
                var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();

                // Load application settings at startup (creates defaults if missing) without blocking UI thread
                try
                {
                    var settingsService = _serviceProvider.GetService<ISettingsService>();
                    _ = settingsService?.LoadSettingsAsync();
                    logger.LogInformation("Application settings loading scheduled at startup");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to schedule settings load at startup");
                }

                logger.LogDebug("Registering dialog interaction handlers");

                // Register interaction handlers on the UI thread with proper window context
                RegisterInteractionHandlers(dialogService, logger);

                // Create and set main window
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                // Save settings on application exit (non-blocking)
                desktop.Exit += async (s, e) =>
                {
                    try
                    {
                        var settingsService = _serviceProvider.GetService<ISettingsService>();
                        if (settingsService != null)
                        {
                            // Use ConfigureAwait(false) to avoid deadlocks
                            await settingsService.SaveSettingsAsync().ConfigureAwait(false);
                        }
                        var exitLogger = _serviceProvider.GetService<ILogger<App>>();
                        exitLogger?.LogInformation("Application settings saved on exit");
                    }
                    catch (Exception ex)
                    {
                        var exitLogger = _serviceProvider.GetService<ILogger<App>>();
                        exitLogger?.LogError(ex, "Failed to save application settings on exit");
                    }
                };

                logger.LogInformation("Application initialization completed successfully");
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                var logger = _serviceProvider.GetService<ILogger<App>>();
                logger?.LogError(ex, "Error during application initialization");

                // Fallback: create main window without dialog handlers
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            }
        }

        base.OnFrameworkInitializationCompleted();

        // Global exception handler
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var logger = _serviceProvider.GetService<ILogger<App>>();
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

                    var result = await ShowDialogAsync<bool>(
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

                    var result = await ShowDialogAsync<bool>(
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

                    var result = await ShowDialogAsync<InputResult?>(
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
                        var viewModelResult = viewModel.Result;
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
            var mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow == null)
            {
                logger.LogWarning("Main window not available for {DialogType}", dialogType);
                return DialogResult<T>.Failure("Main window not available");
            }

            var dialog = dialogFactory();
            var result = await dialog.ShowDialog<T>(mainWindow);
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
    private async Task ShowCriticalErrorNotificationAsync(string title, string message, ILogger logger)
    {
        try
        {
            logger.LogError("Critical UI Error: {Title} - {Message}", title, message);

            // Try to show a simple message box as fallback
            var mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
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
