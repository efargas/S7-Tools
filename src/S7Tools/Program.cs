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

    // Diagnostic initialization: ensure important services are initialized early so
    // we can validate profile storage and stty integration during startup.
        // If started with --diag flag, run initialization synchronously and print diagnostics, then exit.
        if (args != null && args.Length > 0 && args.Contains("--diag"))
        {
            var logger = serviceProvider.GetService<ILogger<Program>>();
            try
            {
                // Run initialization synchronously for diagnostics so we can inspect storage state.
                serviceProvider.InitializeS7ToolsServicesAsync().GetAwaiter().GetResult();

                var profileService = serviceProvider.GetService<S7Tools.Core.Services.Interfaces.ISerialPortProfileService>()
                                     ?? serviceProvider.GetService<S7Tools.Services.SerialPortProfileService>();

                if (profileService != null)
                {
                    try
                    {
                        var storageInfo = profileService.GetStorageInfoAsync().GetAwaiter().GetResult();
                        var json = System.Text.Json.JsonSerializer.Serialize(storageInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        logger?.LogInformation("[S7Tools] SerialPortProfileService storage info:\n{StorageInfo}", json);
                        Console.WriteLine("[S7Tools] SerialPortProfileService storage info:\n" + json); // Keep console for --diag flag
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[S7Tools] Failed to get profile storage info");
                        Console.WriteLine($"[S7Tools] Failed to get profile storage info: {ex}"); // Keep console for --diag flag
                    }
                }

                logger?.LogInformation("[S7Tools] Diagnostics complete. Exiting due to --diag flag");
                Console.WriteLine("[S7Tools] Diagnostics complete. Exiting due to --diag flag."); // Keep console for --diag flag
                return;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[S7Tools] Startup diagnostics failed");
                Console.WriteLine($"[S7Tools] Startup diagnostics failed: {ex}"); // Keep console for --diag flag
                // fall through to start the UI so user can still run the app
            }
        }

        // Normal startup: initialize background services asynchronously without blocking the UI thread.
        // This avoids blocking startup hangs while still allowing services to initialize in the background.
        _ = Task.Run(async () =>
        {
            var logger = serviceProvider.GetService<ILogger<Program>>();
            try
            {
                await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);
                logger?.LogInformation("[S7Tools] Background service initialization completed successfully");
            }
            catch (Exception ex)
            {
                // Log to both logger and console for critical startup failures
                logger?.LogError(ex, "[S7Tools] Background service initialization failed");
                Console.WriteLine($"[S7Tools] Background service initialization failed: {ex}");
            }
        });

        IconProvider.Current.Register<FontAwesomeIconProvider>();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args ?? Array.Empty<string>());
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
}
