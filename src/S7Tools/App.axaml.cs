using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.ViewModels;
using S7Tools.Views;
using S7Tools.Services.Interfaces;
using S7Tools.Models;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace S7Tools;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Get required services
                var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                
                logger.LogDebug("Registering dialog interaction handlers");
                
                // Register interaction handlers on the UI thread with proper window context
                RegisterInteractionHandlers(dialogService, logger);
                
                // Create and set main window
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                
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
                    
                    // Create and show confirmation dialog
                    var dialog = new ConfirmationDialog
                    {
                        DataContext = new ConfirmationDialogViewModel(interaction.Input.Title, interaction.Input.Message)
                    };
                    
                    // Get the main window as parent
                    var mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                    
                    if (mainWindow != null)
                    {
                        var result = await dialog.ShowDialog<bool>(mainWindow);
                        interaction.SetOutput(result);
                        logger.LogDebug("Confirmation dialog result: {Result}", result);
                    }
                    else
                    {
                        logger.LogWarning("Main window not available for dialog, defaulting to false");
                        interaction.SetOutput(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing confirmation dialog");
                    interaction.SetOutput(false);
                }
            });

            // Handle error dialogs
            dialogService.ShowError.RegisterHandler(async interaction =>
            {
                try
                {
                    logger.LogDebug("Showing error dialog: {Title} - {Message}", 
                        interaction.Input.Title, interaction.Input.Message);
                    
                    // Create and show error dialog
                    var dialog = new ConfirmationDialog
                    {
                        DataContext = new ConfirmationDialogViewModel(interaction.Input.Title, interaction.Input.Message, false)
                    };
                    
                    // Get the main window as parent
                    var mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                    
                    if (mainWindow != null)
                    {
                        await dialog.ShowDialog<bool>(mainWindow);
                    }
                    else
                    {
                        logger.LogWarning("Main window not available for error dialog");
                    }
                    
                    interaction.SetOutput(Unit.Default);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error showing error dialog");
                    interaction.SetOutput(Unit.Default);
                }
            });
            
            logger.LogInformation("Dialog interaction handlers registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register dialog interaction handlers");
        }
    }
}
