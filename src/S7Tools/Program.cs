using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Extensions;
using S7Tools.ViewModels;
using S7Tools.Views;
using S7Tools.Services.Interfaces;
using S7Tools.Services;
using Splat.Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Infrastructure.Logging.Core.Models;


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

        // Register global interaction handlers
        RegisterGlobalInteractionHandlers(serviceProvider);

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
        // Add logging with DataStore provider
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            
            // Add DataStore logging provider
            builder.AddDataStore(options =>
            {
                options.MaxEntries = 10000;
            }, config =>
            {
                // Configure logger settings
                config.LogLevel = LogLevel.Debug;
                config.IncludeScopes = true;
                config.CaptureProperties = true;
                config.FormatMessages = true;
                config.CaptureStackTrace = true;
                config.MaxMessageLength = 10000;
            });
        });

        // Add S7Tools services using the extension method
        services.AddS7ToolsServices(options =>
        {
            options.MaxEntries = 10000;
        });

        // Ensure DialogService is registered as singleton for proper interaction handling
        services.AddSingleton<IDialogService, DialogService>();

        // Configure Splat to use the Microsoft.Extensions.DependencyInjection container.
        // This must be done during service configuration and before the service provider is built.
        services.UseMicrosoftDependencyResolver();

        // Views
        services.AddSingleton<MainWindow>(provider => new MainWindow(provider)
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>()
        });
    }

    /// <summary>
    /// Registers global interaction handlers for dialogs and other UI interactions.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    private static void RegisterGlobalInteractionHandlers(IServiceProvider serviceProvider)
    {
        var dialogService = serviceProvider.GetService<IDialogService>();
        Console.WriteLine($"DialogService resolved: {dialogService != null}");
        
        if (dialogService != null)
        {
            Console.WriteLine("Registering interaction handlers...");
            
            // Handle confirmation dialogs
            dialogService.ShowConfirmation.RegisterHandler(interaction =>
            {
                // For now, we'll provide a simple console-based confirmation
                // In a real application, this would show a proper dialog
                Console.WriteLine($"Confirmation: {interaction.Input.Title} - {interaction.Input.Message}");
                Console.WriteLine("Defaulting to 'No' for safety in automated scenario");
                
                // For automated scenarios, we'll default to 'No' to prevent hanging
                // In a real UI application, this would show an actual dialog
                var result = false; // Default to false for safety
                
                interaction.SetOutput(result);
                return Task.CompletedTask;
            });

            // Handle error dialogs
            dialogService.ShowError.RegisterHandler(interaction =>
            {
                Console.WriteLine($"Error: {interaction.Input.Title} - {interaction.Input.Message}");
                interaction.SetOutput(Unit.Default);
                return Task.CompletedTask;
            });
            
            Console.WriteLine("Interaction handlers registered successfully");
        }
        else
        {
            Console.WriteLine("DialogService is null - handlers not registered");
        }
    }
}
