using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Commands;
using S7Tools.Core.Factories;
using S7Tools.Core.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Resources;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.Services.Jobs;
using S7Tools.Services.Bootloader;
using S7Tools.Services.Tasking;
using S7Tools.ViewModels;

namespace S7Tools.Extensions;

/// <summary>
/// Extension methods for configuring S7Tools services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all S7Tools foundation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsFoundationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add UI Thread Service
        services.TryAddSingleton<IUIThreadService, AvaloniaUIThreadService>();

        // Add Localization Service
        services.TryAddSingleton<ILocalizationService, LocalizationService>();

        // Add Layout Service
        services.TryAddSingleton<ILayoutService, LayoutService>();

        // Add Activity Bar Service
        services.TryAddSingleton<IActivityBarService, ActivityBarService>();

        // Add Theme Service
        services.TryAddSingleton<IThemeService, ThemeService>();

        // Add Settings Service
        services.TryAddSingleton<ISettingsService, SettingsService>();

        // Add Dialog Service
        services.TryAddTransient<IDialogService, DialogService>();

        // Add Profile Edit Dialog Service
        services.TryAddTransient<IProfileEditDialogService, ProfileEditDialogService>();

        // Add Unified Profile Dialog Service (delegates to ProfileEditDialogService)
        services.TryAddTransient<IUnifiedProfileDialogService, UnifiedProfileDialogService>();

        // Add Clipboard Service
        services.TryAddTransient<IClipboardService, ClipboardService>();

        // Add Log Export Service
        services.TryAddTransient<ILogExportService, LogExportService>();

        // Add File Log Writer - will subscribe to in-memory log data store and persist to disk when enabled
        services.TryAddSingleton<FileLogWriter>();

        // Add File Dialog Service
        services.TryAddTransient<IFileDialogService>(provider =>
        {
            ILogger<AvaloniaFileDialogService> logger = provider.GetRequiredService<ILogger<AvaloniaFileDialogService>>();
            return new AvaloniaFileDialogService(logger, () =>
            {
                // Get the main window from the application
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }
                return null;
            });
        });

        // Add Greeting Service
        services.TryAddSingleton<IGreetingService, GreetingService>();

        // Add PLC Services
        services.TryAddSingleton<ITagRepository, PlcDataService>();
        services.TryAddSingleton<IS7ConnectionProvider, PlcDataService>();

        // Add Profile Services using Unified IProfileManager<T> Pattern
        // All profile services implement IProfileManager<T> through StandardProfileManager<T> base class
        // This ensures consistent CRUD operations, validation, and business rule enforcement

        // Serial Port Profile Service (Communication - Serial profiles)
        services.TryAddSingleton<ISerialPortProfileService, SerialPortProfileService>();
        services.TryAddSingleton<ISerialPortService, SerialPortService>();

        // Socat Profile Service (Servers Settings - socat configuration)
        services.TryAddSingleton<ISocatProfileService, SocatProfileService>();
        services.TryAddSingleton<ISocatService, SocatService>();

        // Power Supply Profile Service (Power Supply Control - Modbus TCP)
        services.TryAddSingleton<IPowerSupplyProfileService, PowerSupplyProfileService>();
        services.TryAddSingleton<IPowerSupplyService, PowerSupplyService>();

        return services;
    }

    /// <summary>
    /// Adds advanced design pattern services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsAdvancedServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add Command Pattern Services
        services.TryAddSingleton<ICommandDispatcher, CommandDispatcher>();

        // Add Enhanced Factory Services
        services.TryAddSingleton<EnhancedViewModelFactory>();
        services.TryAddSingleton<IViewModelFactory>(provider => provider.GetRequiredService<EnhancedViewModelFactory>());

        // Add Resource Pattern Services - use production S7ToolsResourceManager so UIStrings reads ResX by default
        // Renamed from ResourceManager to S7ToolsResourceManager to avoid collision with System.Resources.ResourceManager
        services.TryAddSingleton<IResourceManager, S7Tools.Resources.S7ToolsResourceManager>();
        // If a decorator is required in the future, change registration here

        // Add Validation Services
        services.TryAddSingleton<IValidationService, S7Tools.Core.Validation.ValidationService>();

        // Add Structured Logging Services
        services.TryAddSingleton<IStructuredLoggerFactory, StructuredLoggerFactory>();
        services.TryAddTransient(typeof(IStructuredLogger), provider =>
        {
            IStructuredLoggerFactory factory = provider.GetRequiredService<IStructuredLoggerFactory>();
            return factory.CreateLogger("S7Tools.Application");
        });

        return services;
    }

    /// <summary>
    /// Adds S7Tools logging infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureDataStore">Optional configuration action for the log data store.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsLogging(
        this IServiceCollection services,
        Action<LogDataStoreOptions>? configureDataStore = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add DataStore logging services
        services.AddDataStoreLogging(configureDataStore);

        return services;
    }

    /// <summary>
    /// Adds S7Tools task manager and jobs services to the service collection.
    /// This includes job management, task scheduling, and bootloader orchestration services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsTaskManagerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add JobManagerOptions configuration
        services.Configure<S7Tools.Core.Models.Jobs.JobManagerOptions>(options =>
        {
            // Persist job profiles in the committed resources folder
            options.ProfilesPath = "src/resources/JobProfiles/profiles.json";
        });

        // Add Job Management Services using options pattern
        services.TryAddSingleton<IJobManager, JobManager>();

        // Add Task Scheduling Services
        services.TryAddSingleton<ITaskScheduler, EnhancedTaskScheduler>();

        // Add Resource Coordination Services
        services.TryAddSingleton<IResourceCoordinator, ResourceCoordinator>();

        // Add Bootloader Services
        services.TryAddSingleton<IBootloaderService, Services.Bootloader.BootloaderService>();
        services.TryAddSingleton<IEnhancedBootloaderService, Services.Bootloader.EnhancedBootloaderService>();

        // Add Payload Services
        services.TryAddSingleton<IPayloadProvider, Services.Bootloader.FilePayloadProvider>();

        // Register PLC Client stub
        services.TryAddTransient<IPlcClient, PlcClientStub>();

        // Add PLC Client Factory
        services.TryAddTransient<Func<JobProfileSet, IPlcClient>>(provider =>
        {
            return profiles =>
            {
                // This would be implemented based on the profile configuration
                // For now, return a stub implementation that logs calls
                return provider.GetRequiredService<IPlcClient>();
            };
        });

        return services;
    }

    /// <summary>
    /// Adds S7Tools ViewModels to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add ViewModels to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsViewModels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add ViewModel Factory
        services.TryAddSingleton<IViewModelFactory, ViewModelFactory>();

        // Add Main ViewModels
        services.TryAddSingleton<MainWindowViewModel>(provider => new MainWindowViewModel(
            provider.GetRequiredService<NavigationViewModel>(),
            provider.GetRequiredService<BottomPanelViewModel>(),
            provider.GetRequiredService<SettingsManagementViewModel>(),
            provider.GetRequiredService<IDialogService>(),
            provider.GetRequiredService<IClipboardService>(),
            provider.GetRequiredService<ISettingsService>(),
            provider.GetService<IFileDialogService>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MainWindowViewModel>>()));

        // Add Specialized ViewModels for MainWindow decomposition
        services.TryAddSingleton<NavigationViewModel>();
        services.TryAddSingleton<BottomPanelViewModel>();
        services.TryAddSingleton<SettingsManagementViewModel>(provider => new SettingsManagementViewModel(
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SettingsManagementViewModel>>(),
            provider.GetRequiredService<ISettingsService>(),
            provider.GetService<IFileDialogService>()));

        // Add Feature ViewModels
        services.TryAddTransient<LogViewerViewModel>();
        services.TryAddTransient<HomeViewModel>();
        services.TryAddTransient<ConnectionsViewModel>();
        services.TryAddSingleton<SettingsViewModel>(provider => new SettingsViewModel(provider));
        services.TryAddTransient<AboutViewModel>();
        services.TryAddTransient<ConfirmationDialogViewModel>();

        // Add Profile Management ViewModels as Singletons to persist state across navigation
        services.TryAddSingleton<SerialPortsSettingsViewModel>();
        services.TryAddTransient<SerialPortProfileViewModel>();
        services.TryAddTransient<SerialPortScannerViewModel>();

        // Add Socat ViewModels (Servers Settings - socat configuration)
        services.TryAddSingleton<SocatSettingsViewModel>();
        services.TryAddTransient<SocatProfileViewModel>();

        // Add Power Supply ViewModels (Power Supply Control - Modbus TCP)
        services.TryAddSingleton<PowerSupplySettingsViewModel>();
        services.TryAddTransient<PowerSupplyProfileViewModel>();

        // Add Task Management ViewModels (Task Manager and Jobs Management)
        services.TryAddSingleton<TaskManagerViewModel>();
        services.TryAddSingleton<JobsManagementViewModel>();

        return services;
    }

    /// <summary>
    /// Adds all S7Tools services including foundation services and logging.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureDataStore">Optional configuration action for the log data store.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsServices(
        this IServiceCollection services,
        Action<LogDataStoreOptions>? configureDataStore = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add foundation services
        services.AddS7ToolsFoundationServices();

        // Add advanced design pattern services
        services.AddS7ToolsAdvancedServices();

        // Add logging services
        services.AddS7ToolsLogging(configureDataStore);

        // Add task manager and jobs services
        services.AddS7ToolsTaskManagerServices();

        // Add ViewModels
        services.AddS7ToolsViewModels();

        return services;
    }

    /// <summary>
    /// Adds S7Tools services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureServices">Action to configure individual services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsServices(
        this IServiceCollection services,
        Action<S7ToolsServiceConfiguration> configureServices)
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentNullException.ThrowIfNull(configureServices);

        var configuration = new S7ToolsServiceConfiguration();
        configureServices(configuration);

        // Add foundation services based on configuration
        if (configuration.IncludeUIThreadService)
        {
            services.TryAddSingleton<IUIThreadService, AvaloniaUIThreadService>();
        }

        if (configuration.IncludeLocalizationService)
        {
            services.TryAddSingleton<ILocalizationService, LocalizationService>();
        }

        if (configuration.IncludeLayoutService)
        {
            services.TryAddSingleton<ILayoutService, LayoutService>();
        }

        if (configuration.IncludeActivityBarService)
        {
            services.TryAddSingleton<IActivityBarService, ActivityBarService>();
        }

        if (configuration.IncludeThemeService)
        {
            services.TryAddSingleton<IThemeService, ThemeService>();
        }

        // Add logging services if configured
        if (configuration.IncludeLoggingServices)
        {
            services.AddDataStoreLogging(configuration.DataStoreConfiguration);
        }

        return services;
    }

    /// <summary>
    /// Initializes S7Tools services that require initialization after the service provider is built.
    /// Uses parallel initialization for profile services to improve startup performance.
    /// </summary>
    /// <param name="serviceProvider">The service provider to initialize services from.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var startTime = System.Diagnostics.Stopwatch.StartNew();
        ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        ILogger? startupLogger = loggerFactory?.CreateLogger("S7Tools.Startup");

        startupLogger?.LogInformation("Starting S7Tools service initialization...");

        // Initialize Layout Service
        ILayoutService? layoutService = serviceProvider.GetService<ILayoutService>();
        if (layoutService != null)
        {
            await layoutService.LoadLayoutAsync().ConfigureAwait(false);
            startupLogger?.LogDebug("Layout service initialized");
        }

        // Initialize Theme Service
        IThemeService? themeService = serviceProvider.GetService<IThemeService>();
        if (themeService != null)
        {
            await themeService.LoadThemeConfigurationAsync().ConfigureAwait(false);
            startupLogger?.LogDebug("Theme service initialized");
        }

        // Initialize Localization Service (if needed)
        ILocalizationService? localizationService = serviceProvider.GetService<ILocalizationService>();
        if (localizationService != null)
        {
            // Localization service doesn't require async initialization currently
            // but this is where you would add it if needed
            startupLogger?.LogDebug("Localization service ready");
        }

        // Initialize Profile Services in parallel using unified IProfileManager<T> pattern
        // All profile services implement the same interface and can be initialized consistently
        // This parallel approach improves startup time by loading profiles concurrently
        var profileInitTasks = new List<Task>();

        // Initialize Serial Port Profiles
        ISerialPortProfileService? serialProfileService = serviceProvider.GetService<ISerialPortProfileService>();
        if (serialProfileService != null)
        {
            profileInitTasks.Add(InitializeProfileServiceAsync(
                serialProfileService,
                "Serial Port",
                serviceProvider.GetService<ILogger<ISerialPortProfileService>>(),
                startupLogger));
        }

        // Initialize Socat Profiles
        ISocatProfileService? socatProfileService = serviceProvider.GetService<ISocatProfileService>();
        if (socatProfileService != null)
        {
            profileInitTasks.Add(InitializeProfileServiceAsync(
                socatProfileService,
                "Socat",
                serviceProvider.GetService<ILogger<ISocatProfileService>>(),
                startupLogger));
        }

        // Initialize Power Supply Profiles
        IPowerSupplyProfileService? powerSupplyProfileService = serviceProvider.GetService<IPowerSupplyProfileService>();
        if (powerSupplyProfileService != null)
        {
            profileInitTasks.Add(InitializeProfileServiceAsync(
                powerSupplyProfileService,
                "Power Supply",
                serviceProvider.GetService<ILogger<IPowerSupplyProfileService>>(),
                startupLogger));
        }

        // Wait for all profile services to initialize in parallel
        if (profileInitTasks.Count > 0)
        {
            await Task.WhenAll(profileInitTasks).ConfigureAwait(false);
        }

        startTime.Stop();
        startupLogger?.LogInformation(
            "S7Tools service initialization completed in {ElapsedMs}ms ({ProfileCount} profile services initialized in parallel)",
            startTime.ElapsedMilliseconds,
            profileInitTasks.Count);
    }

    /// <summary>
    /// Initializes a profile service asynchronously with comprehensive error handling and logging.
    /// This helper method is used for parallel profile service initialization.
    /// </summary>
    /// <typeparam name="T">The profile type that implements IProfileBase.</typeparam>
    /// <param name="profileService">The profile service to initialize.</param>
    /// <param name="serviceName">The friendly name of the service for logging.</param>
    /// <param name="serviceLogger">The logger specific to the service.</param>
    /// <param name="startupLogger">The startup logger for overall initialization tracking.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    private static async Task InitializeProfileServiceAsync<T>(
        Core.Services.Interfaces.IProfileManager<T> profileService,
        string serviceName,
        ILogger? serviceLogger,
        ILogger? startupLogger) where T : class, Core.Services.Interfaces.IProfileBase
    {
        var serviceStartTime = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            startupLogger?.LogDebug("Initializing {ServiceName} profile service...", serviceName);

            // Load profiles to ensure storage is initialized
            await profileService.GetAllAsync().ConfigureAwait(false);

            serviceStartTime.Stop();
            startupLogger?.LogInformation(
                "{ServiceName} profile service initialized successfully in {ElapsedMs}ms",
                serviceName,
                serviceStartTime.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            serviceStartTime.Stop();
            serviceLogger?.LogError(ex,
                "Failed to initialize {ServiceName} profile service during application startup (after {ElapsedMs}ms)",
                serviceName,
                serviceStartTime.ElapsedMilliseconds);

            startupLogger?.LogWarning(
                "{ServiceName} profile service initialization failed but application will continue",
                serviceName);

            // Don't rethrow - allow application to start even if profile initialization fails
            // The service will attempt to load profiles again when accessed
        }
    }

    /// <summary>
    /// Shuts down S7Tools services gracefully.
    /// </summary>
    /// <param name="serviceProvider">The service provider to shut down services from.</param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    public static async Task ShutdownS7ToolsServicesAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Save Layout Service configuration
        ILayoutService? layoutService = serviceProvider.GetService<ILayoutService>();
        if (layoutService != null)
        {
            await layoutService.SaveLayoutAsync().ConfigureAwait(false);
        }

        // Save Theme Service configuration
        IThemeService? themeService = serviceProvider.GetService<IThemeService>();
        if (themeService != null)
        {
            await themeService.SaveThemeConfigurationAsync().ConfigureAwait(false);
        }

        // Dispose logging services
        ILogDataStore? logDataStore = serviceProvider.GetService<S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore>();
        logDataStore?.Dispose();
    }
}

/// <summary>
/// Configuration options for S7Tools services.
/// </summary>
public sealed class S7ToolsServiceConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to include the UI thread service.
    /// Default is true.
    /// </summary>
    public bool IncludeUIThreadService { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the localization service.
    /// Default is true.
    /// </summary>
    public bool IncludeLocalizationService { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the layout service.
    /// Default is true.
    /// </summary>
    public bool IncludeLayoutService { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the activity bar service.
    /// Default is true.
    /// </summary>
    public bool IncludeActivityBarService { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the theme service.
    /// Default is true.
    /// </summary>
    public bool IncludeThemeService { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include logging services.
    /// Default is true.
    /// </summary>
    public bool IncludeLoggingServices { get; set; } = true;

    /// <summary>
    /// Gets or sets the configuration action for the log data store.
    /// </summary>
    public Action<LogDataStoreOptions>? DataStoreConfiguration { get; set; }
}
