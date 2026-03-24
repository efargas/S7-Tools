using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Extensions;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Layout;
using S7Tools.Views;
using S7Tools.Views.Layout;
using Splat.Microsoft.Extensions.DependencyInjection;
using Serilog;


namespace S7Tools;

sealed class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Mitigate GLX/OpenGL renderer blacklist (e.g., SVGA3D) by forcing software rendering on Linux
        // This avoids Avalonia.OpenGL.OpenGlException during startup on some VMs/drivers
        try
        {
            if (OperatingSystem.IsLinux())
            {
                // Force Avalonia to prefer software (Skia) rendering
                Environment.SetEnvironmentVariable("AVALONIA_RENDERING_MODE", "Software");
                // Hint Mesa to avoid hardware GL paths in constrained environments
                Environment.SetEnvironmentVariable("LIBGL_ALWAYS_SOFTWARE", "1");
            }
        }
        catch
        {
            // Non-fatal; continue with defaults
        }

        // Diagnostic initialization: ensure important services are initialized early so
        // we can validate profile storage and stty integration during startup.
        // If started with --diag flag, run initialization asynchronously and print diagnostics, then exit.
        if (args != null && args.Length > 0 && args.Contains("--diag"))
        {
            ILogger<Program>? logger = serviceProvider.GetService<ILogger<Program>>();
            try
            {
                // Run initialization asynchronously for diagnostics
                await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);

                ISerialPortProfileService? profileService = serviceProvider.GetService<S7Tools.Core.Interfaces.Services.ISerialPortProfileService>();

                if (profileService != null)
                {
                    try
                    {
                        IEnumerable<SerialPortProfile> profiles = await profileService.GetAllAsync().ConfigureAwait(false);
                        logger?.LogInformation("[S7Tools] SerialPortProfileService loaded {ProfileCount} profiles", profiles.Count());
                        Console.WriteLine($"[S7Tools] SerialPortProfileService loaded {profiles.Count()} profiles"); // Keep console for --diag flag
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[S7Tools] Failed to get profile info");
                        Console.WriteLine($"[S7Tools] Failed to get profile info: {ex}"); // Keep console for --diag flag
                    }
                }

                // Initialize SocatProfileService and ensure default profile exists
                ISocatProfileService? socatProfileService = serviceProvider.GetService<S7Tools.Core.Interfaces.Services.ISocatProfileService>();

                if (socatProfileService != null)
                {
                    try
                    {
                        IEnumerable<SocatProfile> profiles = await socatProfileService.GetAllAsync().ConfigureAwait(false);
                        logger?.LogInformation("[S7Tools] SocatProfileService loaded {ProfileCount} profiles", profiles.Count());
                        Console.WriteLine($"[S7Tools] SocatProfileService loaded {profiles.Count()} profiles"); // Keep console for --diag flag
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[S7Tools] Failed to initialize socat profile storage");
                        Console.WriteLine($"[S7Tools] Failed to initialize socat profile storage: {ex}"); // Keep console for --diag flag
                    }
                }

                // Initialize PowerSupplyProfileService and ensure default profile exists
                IPowerSupplyProfileService? powerSupplyProfileService = serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IPowerSupplyProfileService>();

                if (powerSupplyProfileService != null)
                {
                    try
                    {
                        IEnumerable<PowerSupplyProfile> profiles = await powerSupplyProfileService.GetAllAsync().ConfigureAwait(false);
                        logger?.LogInformation("[S7Tools] PowerSupplyProfileService loaded {ProfileCount} profiles", profiles.Count());
                        Console.WriteLine($"[S7Tools] PowerSupplyProfileService loaded {profiles.Count()} profiles"); // Keep console for --diag flag
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[S7Tools] Failed to initialize power supply profile storage");
                        Console.WriteLine($"[S7Tools] Failed to initialize power supply profile storage: {ex}"); // Keep console for --diag flag
                    }
                }

                // Initialize JobManager and ensure default job profiles exist
                IJobManager? jobManager = serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IJobManager>();

                if (jobManager != null)
                {
                    try
                    {
                        IEnumerable<JobProfile> profiles = await jobManager.GetAllAsync().ConfigureAwait(false);
                        logger?.LogInformation("[S7Tools] JobManager loaded {ProfileCount} profiles", profiles.Count());
                        Console.WriteLine($"[S7Tools] JobManager loaded {profiles.Count()} profiles"); // Keep console for --diag flag
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[S7Tools] Failed to initialize job manager storage");
                        Console.WriteLine($"[S7Tools] Failed to initialize job manager storage: {ex}"); // Keep console for --diag flag
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

        // Profile services are initialized asynchronously in App.OnFrameworkInitializationCompleted()
        // after foundational services (PathService, ResourceManager, ApplicationSettings) complete.
        // This ensures proper dependency order while allowing profile services to load in parallel.

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
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine(basePath, "Resources", "AppSettings", "UserSettings.json"), optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration);

        // Bind strongly typed options
        var appSection = configuration.GetSection("App");
        services.AddOptions<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings>()
            .Bind(appSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register WritableOptions factory
        services.AddTransient<S7Tools.Core.Interfaces.Services.IWritableOptions<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings>>(provider =>
            new S7Tools.Services.WritableOptions<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings>(
                basePath,
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings>>(),
                "App",
                Path.Combine("Resources", "AppSettings", "UserSettings.json")));

        // Register Global LogDataStore Sink for UI
        var logDataStore = new S7Tools.Infrastructure.Logging.Core.Storage.LogDataStore(new LogDataStoreOptions { MaxEntries = 10000 });
        services.AddSingleton<S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore>(logDataStore);
        services.AddSingleton<S7Tools.Core.Interfaces.Services.ITaskLogDataStore>(logDataStore);

        // Configure Serilog Global Pipeline
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            // Global DataStore sink for UI
            .WriteTo.Sink(logDataStore)
            
            // Application File Sink (No TaskId)
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(e => e.Properties.ContainsKey("TaskId"))
                .WriteTo.File("Logs/Application/app-.log", rollingInterval: RollingInterval.Day))
            
            // Task-specific File Sinks via Map (Has TaskId)
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("TaskId"))
                .WriteTo.Map("TaskId", (taskId, wt) => 
                {
                    // Within a task, map by Scope to create main.log, process.log, etc.
                    wt.Map("LogScope", "Main", (scope, subWt) => 
                        subWt.File($"Logs/Tasks/{taskId}/{scope}.log", rollingInterval: RollingInterval.Day));
                }, sinkMapCountLimit: 50))
                
            .CreateLogger();

        // Add logging using Serilog
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
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
