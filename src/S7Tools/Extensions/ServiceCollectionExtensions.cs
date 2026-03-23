using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Core.Commands;
using S7Tools.Core.Factories;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Resources;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Infrastructure.Logging.Sinks;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Bootloader;
using S7Tools.Services.Interfaces;
using S7Tools.Services.Jobs;
using S7Tools.Services.Tasking;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Dialogs;
using S7Tools.ViewModels.Hex;
using S7Tools.ViewModels.Jobs;
using S7Tools.ViewModels.Layout;
using S7Tools.ViewModels.Pages;
using S7Tools.ViewModels.Profiles;
using S7Tools.ViewModels.Settings;
using S7Tools.ViewModels.Tasks;

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

        // Add Time Provider - Critical for testability and consistency
        services.TryAddSingleton<ITimeProvider, S7Tools.Services.Time.TimeProvider>();

        // Add Shell Command Executor
        services.TryAddSingleton<S7Tools.Core.Services.Shell.IShellCommandExecutor, S7Tools.Services.Shell.ShellCommandExecutor>();

        // Add UI Thread Service
        services.TryAddSingleton<IUIThreadService, AvaloniaUIThreadService>();

        // Add UI Refresh Service
        services.TryAddSingleton<IUIRefreshService, UIRefreshService>();

        // Add Localization Service
        services.TryAddSingleton<ILocalizationService, LocalizationService>();

        // Add Layout Service
        services.TryAddSingleton<ILayoutService, LayoutService>();

        // Add Docking Service
        services.TryAddSingleton<DockingService>();

        // Add Activity Bar Service
        services.TryAddSingleton<IActivityBarService, ActivityBarService>();

        // Add Theme Service
        services.TryAddSingleton<IThemeService, ThemeService>();

        // Add Dialog Service
        services.TryAddTransient<IDialogService, DialogService>();

        // Add Unified Profile Dialog Service (delegates to ProfileEditDialogService)
        services.TryAddTransient<IUnifiedProfileDialogService, UnifiedProfileDialogService>();

        // Add Profile Details Service for job information display
        services.TryAddSingleton<IProfileDetailsService, ProfileDetailsService>();

        // Add Error Display Service for consistent error messaging
        services.TryAddSingleton<IErrorDisplayService, ErrorDisplayService>();

        // Add Job Info Display ViewModel for job details panel
        services.TryAddTransient<ViewModels.Jobs.JobInfoDisplayViewModel>();

        // Add Clipboard Service
        services.TryAddTransient<IClipboardService, ClipboardService>();

        // Add Log Export Service
        services.TryAddTransient<ILogExportService, LogExportService>();

        // Register FileLogSink as ILogSink for UnifiedLoggerProvider
        // It starts automatically in its constructor
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILogSink, FileLogSink>());

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
        // Register as IProfileManager<SerialPortProfile> for generic dependency injection
        services.TryAddSingleton<IProfileManager<Core.Models.SerialPortProfile>>(provider =>
            provider.GetRequiredService<ISerialPortProfileService>());

        // Register SerialPort specialized services (Phase 1 refactoring)
        services.TryAddSingleton<Services.SerialPort.SerialPortDiscoveryService>();
        services.TryAddSingleton<Services.SerialPort.SerialPortConfigurationService>();
        services.TryAddSingleton<Services.SerialPort.SerialPortMonitoringService>();

        // Register SerialPort facade service (orchestrates specialized services)
        services.TryAddSingleton<ISerialPortService, SerialPortService>();

        // Socat Profile Service (Servers Settings - socat configuration)
        services.TryAddSingleton<ISocatProfileService, SocatProfileService>();
        // Register as IProfileManager<SocatProfile> for generic dependency injection
        services.TryAddSingleton<IProfileManager<Core.Models.SocatProfile>>(provider =>
            provider.GetRequiredService<ISocatProfileService>());

        // Register Socat specialized services (Phase 2 refactoring)
        services.TryAddSingleton<Services.Socat.SocatCommandBuilder>();
        services.TryAddSingleton<Services.Socat.SocatProcessManager>();
        services.TryAddSingleton<Services.Socat.SocatPortManager>();
        services.TryAddSingleton<Services.Socat.SocatConfigurationService>();

        // Register Socat facade service (orchestrates specialized services)
        services.TryAddSingleton<ISocatService>(provider =>
            new SocatService(
                provider.GetRequiredService<ILogger<SocatService>>(),
                provider.GetRequiredService<IApplicationSettingsService>(),
                provider.GetRequiredService<Services.Socat.SocatCommandBuilder>(),
                provider.GetRequiredService<Services.Socat.SocatProcessManager>(),
                provider.GetRequiredService<Services.Socat.SocatPortManager>(),
                provider.GetRequiredService<Services.Socat.SocatConfigurationService>(),
                provider.GetRequiredService<ISerialPortService>(),
                provider.GetRequiredService<ITimeProvider>()
            )
        );

        // Add Power Supply Profile Service (Power Supply Control - Modbus TCP)
        services.TryAddSingleton<IPowerSupplyProfileService, PowerSupplyProfileService>();
        // Register as IProfileManager<PowerSupplyProfile> for generic dependency injection
        services.TryAddSingleton<IProfileManager<Core.Models.PowerSupplyProfile>>(provider =>
            provider.GetRequiredService<IPowerSupplyProfileService>());
        services.TryAddSingleton<IPowerSupplyService, PowerSupplyService>();

        // Add Memory Region Profile Service (Memory region profiling and mapping)
        services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();
        // Register as IProfileManager<MemoryMappingProfile> for generic dependency injection
        services.TryAddSingleton<IProfileManager<Core.Models.MemoryMappingProfile>>(provider =>
            provider.GetRequiredService<IMemoryRegionProfileService>());

        // Add Payload Set Profile Service (Bootloader payload management)
        services.TryAddSingleton<IPayloadSetProfileService, PayloadSetProfileService>();
        // Register as IProfileManager<PayloadSetProfile> for generic dependency injection
        services.TryAddSingleton<IProfileManager<Core.Models.Jobs.PayloadSetProfile>>(provider =>
            provider.GetRequiredService<IPayloadSetProfileService>());

        return services;
    }

    /// <summary>
    /// Adds S7Tools path management services to the service collection.
    /// These services handle dynamic path resolution, resource initialization, and settings management.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsPathManagement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add path resolution service for dynamic path management
        services.TryAddSingleton<S7Tools.Core.Interfaces.Services.IPathService, PathService>();

        // Add resource manager service for resource initialization and validation
        services.TryAddSingleton<S7Tools.Core.Interfaces.Services.IResourceManagerService, ResourceManagerService>();

        // Add application settings service for layered configuration management
        services.TryAddSingleton<S7Tools.Core.Interfaces.Services.IApplicationSettingsService, ApplicationSettingsService>();

        // Add path diagnostics service for troubleshooting and monitoring
        services.TryAddSingleton<S7Tools.Core.Interfaces.Services.IPathDiagnosticsService, PathDiagnosticsService>();

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

        services.TryAddSingleton<ICentralizedTaskLogService, CentralizedTaskLogService>();
        services.TryAddSingleton<ITaskLogDataStoreFactory, TaskLogDataStoreFactory>();
        services.Configure<S7Tools.Infrastructure.Logging.Core.Configuration.TaskLogDataStoreOptions>(options => options.MaxEntries = 2000);

        services.AddLogging(builder => builder.AddUnifiedFileLogger<S7Tools.Infrastructure.Logging.Core.Configuration.CombinedFileLoggerConfiguration>(options =>
        {
            // Use absolute paths by resolving PathService from DI
            // The logger provider will resolve these paths when it's created
            var serviceProvider = builder.Services.BuildServiceProvider();
            var pathService = serviceProvider.GetService<S7Tools.Core.Interfaces.Services.IPathService>();

            if (pathService != null)
            {
                // Use absolute paths from PathService with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                options.DefaultLogPath = System.IO.Path.Combine(pathService.MainLogsDirectory, $"s7tools_{timestamp}.log");
                options.TaskMainLogPath = "task-main.log";  // These are relative to task directory
                options.TaskProcessLogPath = "task-process.log";
                options.TaskProtocolLogPath = "task-protocol.log";
            }
            else
            {
                // Fallback to relative paths if PathService not available yet
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                options.DefaultLogPath = $"s7tools_{timestamp}.log";
                options.TaskMainLogPath = "task-main.log";
                options.TaskProcessLogPath = "task-process.log";
                options.TaskProtocolLogPath = "task-protocol.log";
            }
        }));

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

        // Add Job Management Services using factory pattern to resolve path dynamically
        services.TryAddSingleton<IJobManager>(serviceProvider =>
        {
            IPathService pathService = serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
            ILogger<JobManager> logger = serviceProvider.GetRequiredService<ILogger<JobManager>>();
            IResourceCoordinator resourceCoordinator = serviceProvider.GetRequiredService<IResourceCoordinator>();
            ISerialPortProfileService serialProfileService = serviceProvider.GetRequiredService<ISerialPortProfileService>();
            ISocatProfileService socatProfileService = serviceProvider.GetRequiredService<ISocatProfileService>();
            IPowerSupplyProfileService powerSupplyProfileService = serviceProvider.GetRequiredService<IPowerSupplyProfileService>();
            IMemoryRegionProfileService memoryRegionProfileService = serviceProvider.GetRequiredService<IMemoryRegionProfileService>();
            ITimeProvider timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();


            // Create options with dynamically resolved path
            IOptions<JobManagerOptions> options = Microsoft.Extensions.Options.Options.Create(new S7Tools.Core.Models.Jobs.JobManagerOptions
            {
                ProfilesPath = pathService.JobsPath
            });

            return new JobManager(options, logger, resourceCoordinator, serialProfileService, socatProfileService, powerSupplyProfileService, memoryRegionProfileService, timeProvider);
        });

        // Add JobProfileSetFactory for creating JobProfileSet from profile IDs
        services.TryAddSingleton<Services.Jobs.IJobProfileSetFactory, Services.Jobs.JobProfileSetFactory>();

        // Add Task Scheduling Services
        services.TryAddSingleton<ITaskScheduler, EnhancedTaskScheduler>();
        services.TryAddSingleton<IJobScheduler, Services.Tasking.JobScheduler>();

        // Add Task Logging Services
        services.TryAddSingleton<ITaskLoggerFactory, Services.Logging.TaskLoggerFactory>();

        // Add Resource Coordination Services
        services.TryAddSingleton<IResourceCoordinator, ResourceCoordinator>();

        // Add Bootloader Services
        // Add Bootloader Services
        services.TryAddSingleton<IBootloaderService, Services.Bootloader.BootloaderService>();

        // Add Payload Services
        services.TryAddSingleton<IPayloadProvider, Services.Adapters.FilePayloadProvider>();

        // Add High-Performance Memory Dump Services
        services.TryAddSingleton<S7Tools.Services.Adapters.Plc.DumperService>();
        services.TryAddSingleton<Services.MemoryDumpOrchestrator>();

        // Add PLC Adapters
        services.TryAddTransient<IPlcTransport, Services.Adapters.PlcTransportAdapter>();
        services.TryAddTransient<IPlcProtocol, Services.Adapters.PlcProtocolAdapter>();
        services.TryAddTransient<IPlcClient, Services.Adapters.PlcClientAdapter>();

        // Add PLC Client Factory
        services.TryAddTransient<Func<JobProfileSet, IPlcClient>>(provider => profiles =>
        {
            IPlcClient client = provider.GetRequiredService<IPlcClient>();
            // Usage: Access TcpHost from nested Configuration object, default to 127.0.0.1 if empty/null
            string host = string.IsNullOrEmpty(profiles.Socat.Configuration?.TcpHost)
                ? "127.0.0.1"
                : profiles.Socat.Configuration.TcpHost;

            client.Configure(host, profiles.Socat.Port);
            return client;
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

        // Add Main ViewModels
        services.TryAddSingleton<MainWindowViewModel>(provider => new MainWindowViewModel(
            provider.GetRequiredService<NavigationViewModel>(),
            provider.GetRequiredService<SettingsManagementViewModel>(),
            provider.GetRequiredService<IDialogService>(),
            provider.GetRequiredService<IClipboardService>(),
            provider.GetRequiredService<IApplicationSettingsService>(),
            provider.GetService<IFileDialogService>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MainWindowViewModel>>(),
            provider));

        // Add Specialized ViewModels for MainWindow decomposition
        services.TryAddSingleton<NavigationViewModel>();
        services.TryAddSingleton<SettingsManagementViewModel>(provider => new SettingsManagementViewModel(
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SettingsManagementViewModel>>(),
            provider.GetRequiredService<IApplicationSettingsService>(),
            provider.GetService<IFileDialogService>()));

        // Add Feature ViewModels
        services.TryAddTransient<LogViewerViewModel>();
        services.TryAddTransient<LoggingTestViewModel>();
        services.TryAddTransient<HomeViewModel>();
        services.TryAddTransient<ConnectionsViewModel>();
        services.TryAddSingleton<SettingsViewModel>(provider => new SettingsViewModel(provider));
        services.TryAddSingleton<ViewModels.Profiles.ProfilesViewModel>(provider => new ViewModels.Profiles.ProfilesViewModel(provider));
        services.TryAddTransient<AboutViewModel>();
        services.TryAddTransient<ConfirmationDialogViewModel>();

        // Add Memory Dump Viewer ViewModel
        services.TryAddTransient<StreamedMemoryDumpViewModel>();
        services.TryAddSingleton<FileMemoryDumpViewModel>();
        services.TryAddTransient<FileMemoryDumpDocumentViewModel>();
        services.TryAddSingleton<MemoryDumpViewerViewModel>();

        // Hex Viewer
        services.TryAddTransient<HexViewerViewModel>();
        services.TryAddTransient<DataInspectorViewModel>(); // Actually this is usually created by HexViewerViewModel, but transient is fine if injected


        // Add Profile Management ViewModels as Singletons to persist state across navigation
        services.TryAddSingleton<SerialPortsSettingsViewModel>();
        services.TryAddTransient<SerialPortProfileViewModel>();
        services.TryAddTransient<SerialPortProfilesViewModel>();

        // Add reusable Control ViewModels
        services.TryAddTransient<ViewModels.Controls.SerialPortDiscoveryViewModel>();

        // Add Socat ViewModels (Servers Settings - socat configuration)
        services.TryAddSingleton<SocatSettingsViewModel>();
        services.TryAddTransient<SocatProfileViewModel>();
        services.TryAddTransient<SocatProfilesViewModel>();

        // Add Power Supply ViewModels (Power Supply Control - Modbus TCP)
        services.TryAddTransient<PowerSupplyProfileViewModel>();
        services.TryAddTransient<PowerSupplyProfilesViewModel>();

        // Add Memory Region Profiles ViewModels
        services.TryAddTransient<MemoryRegionProfilesViewModel>();

        // Add Task Management ViewModels (Task Manager and Jobs Management)
        // Task Viewmodels
        services.AddTransient<TaskStatisticsViewModel>();
        services.AddTransient<TaskCommandManager>(); // Registered for DI interaction
        services.AddSingleton<TaskManagerViewModel>();
        services.AddTransient<TaskDetailsViewModel>();
        services.TryAddSingleton<ScheduledTasksViewModel>();
        services.TryAddSingleton<HistoryTasksViewModel>();
        services.TryAddSingleton<TaskCreatorViewModel>();
        services.TryAddSingleton<TaskManagerShellViewModel>();
        services.TryAddSingleton<JobsManagementViewModel>();
        services.TryAddSingleton<ActiveTasksViewModel>();
        services.TryAddTransient<JobWizardViewModel>();
        services.TryAddTransient<JobWizardMemoryRegionStepViewModel>();

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

        // Add path management services
        services.AddS7ToolsPathManagement();

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
        List<Task> profileInitTasks = [];

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

        // Initialize Job Manager
        IJobManager? jobManager = serviceProvider.GetService<IJobManager>();
        if (jobManager != null)
        {
            profileInitTasks.Add(InitializeProfileServiceAsync(
                jobManager,
                "Job Manager",
                serviceProvider.GetService<ILogger<IJobManager>>(),
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

        ILogger<App>? logger = serviceProvider.GetService<ILogger<App>>();
        logger?.LogInformation("Starting application shutdown sequence");

        try
        {
            // Save Layout Service configuration
            ILayoutService? layoutService = serviceProvider.GetService<ILayoutService>();
            if (layoutService != null)
            {
                logger?.LogDebug("Saving layout configuration");
                await layoutService.SaveLayoutAsync().ConfigureAwait(false);
            }

            // Save Theme Service configuration
            IThemeService? themeService = serviceProvider.GetService<IThemeService>();
            if (themeService != null)
            {
                logger?.LogDebug("Saving theme configuration");
                await themeService.SaveThemeConfigurationAsync().ConfigureAwait(false);
            }

            // Dispose all IDisposable services with error handling
            await DisposeServicesAsync(serviceProvider, logger).ConfigureAwait(false);

            logger?.LogInformation("Application shutdown sequence completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during application shutdown sequence");
            throw; // Re-throw to ensure calling code knows about shutdown failures
        }
    }

    /// <summary>
    /// Disposes all IDisposable services in the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing services to dispose.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    private static async Task DisposeServicesAsync(IServiceProvider serviceProvider, ILogger? logger)
    {
        // List of service types to dispose in specific order (critical services first)
        Type[] serviceTypes =
        [
            // Critical infrastructure services
            typeof(ITaskScheduler),
            typeof(IS7ConnectionProvider),
            typeof(IPowerSupplyService),
            typeof(ISocatService),
            typeof(ISerialPortService),

            // Profile management services
            typeof(ISerialPortProfileService),
            typeof(ISocatProfileService),
            typeof(IPowerSupplyProfileService),

            // UI and logging services
            typeof(IUIRefreshService),
            typeof(S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore)
        ];

        foreach (Type serviceType in serviceTypes)
        {
            try
            {
                object? service = serviceProvider.GetService(serviceType);
                if (service is IDisposable disposable)
                {
                    logger?.LogDebug("Disposing service: {ServiceType}", serviceType.Name);
                    disposable.Dispose();
                }
                else if (service is IAsyncDisposable asyncDisposable)
                {
                    logger?.LogDebug("Disposing async service: {ServiceType}", serviceType.Name);
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error disposing service {ServiceType}", serviceType.Name);
                // Continue with other services even if one fails
            }
        }

        // Dispose logging infrastructure last
        try
        {
            S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore? logDataStore =
                serviceProvider.GetService<S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore>();
            if (logDataStore is IDisposable disposableLogStore)
            {
                logger?.LogDebug("Disposing log data store");
                disposableLogStore.Dispose();
            }
        }
        catch (Exception ex)
        {
            // Use fallback logging since our logger might be disposed
            System.Console.WriteLine($"Warning: Error disposing log data store: {ex.Message}");
        }
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
