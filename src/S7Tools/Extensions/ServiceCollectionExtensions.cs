using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Commands;
using S7Tools.Core.Factories;
using S7Tools.Core.Logging;
using S7Tools.Core.Resources;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
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
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

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
            var logger = provider.GetRequiredService<ILogger<AvaloniaFileDialogService>>();
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
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Add Command Pattern Services
        services.TryAddSingleton<ICommandDispatcher, CommandDispatcher>();

        // Add Enhanced Factory Services
        services.TryAddSingleton<EnhancedViewModelFactory>();
        services.TryAddSingleton<IViewModelFactory>(provider => provider.GetRequiredService<EnhancedViewModelFactory>());

        // Add Resource Pattern Services (InMemoryResourceManager para pruebas/desarrollo)
        services.TryAddSingleton<IResourceManager, S7Tools.Core.Resources.InMemoryResourceManager>();
        // Si se requiere el ResourceManager decorador, cambiar aquí

        // Add Validation Services
        services.TryAddSingleton<IValidationService, S7Tools.Core.Validation.ValidationService>();

        // Add Structured Logging Services
        services.TryAddSingleton<IStructuredLoggerFactory, StructuredLoggerFactory>();
        services.TryAddTransient(typeof(IStructuredLogger), provider =>
        {
            var factory = provider.GetRequiredService<IStructuredLoggerFactory>();
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
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Add DataStore logging services
        services.AddDataStoreLogging(configureDataStore);

        return services;
    }

    /// <summary>
    /// Adds S7Tools ViewModels to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add ViewModels to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS7ToolsViewModels(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

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
        services.TryAddTransient<SettingsViewModel>(provider => new SettingsViewModel(provider));
        services.TryAddTransient<AboutViewModel>();
        services.TryAddTransient<ConfirmationDialogViewModel>();

        // Add Serial Port ViewModels
        services.TryAddTransient<SerialPortsSettingsViewModel>();
        services.TryAddTransient<SerialPortProfileViewModel>();
        services.TryAddTransient<SerialPortScannerViewModel>();

        // Add Socat ViewModels (Servers Settings - socat configuration)
        services.TryAddTransient<SocatSettingsViewModel>();
        services.TryAddTransient<SocatProfileViewModel>();

        // Add Power Supply ViewModels (Power Supply Control - Modbus TCP)
        services.TryAddTransient<PowerSupplySettingsViewModel>();
        services.TryAddTransient<PowerSupplyProfileViewModel>();

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
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Add foundation services
        services.AddS7ToolsFoundationServices();

        // Add advanced design pattern services
        services.AddS7ToolsAdvancedServices();

        // Add logging services
        services.AddS7ToolsLogging(configureDataStore);

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
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureServices == null)
        {
            throw new ArgumentNullException(nameof(configureServices));
        }

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
    /// </summary>
    /// <param name="serviceProvider">The service provider to initialize services from.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Initialize Layout Service
        var layoutService = serviceProvider.GetService<ILayoutService>();
        if (layoutService != null)
        {
            await layoutService.LoadLayoutAsync().ConfigureAwait(false);
        }

        // Initialize Theme Service
        var themeService = serviceProvider.GetService<IThemeService>();
        if (themeService != null)
        {
            await themeService.LoadThemeConfigurationAsync().ConfigureAwait(false);
        }

        // Initialize Localization Service (if needed)
        var localizationService = serviceProvider.GetService<ILocalizationService>();
        if (localizationService != null)
        {
            // Localization service doesn't require async initialization currently
            // but this is where you would add it if needed
        }

        // Initialize Profile Services using unified IProfileManager<T> pattern
        // All profile services implement the same interface and can be initialized consistently
        try
        {
            // Initialize Serial Port Profiles
            var serialProfileService = serviceProvider.GetService<ISerialPortProfileService>();
            if (serialProfileService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await serialProfileService.GetAllAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var logger = serviceProvider.GetService<ILogger<ISerialPortProfileService>>();
                        logger?.LogError(ex, "Failed to initialize serial port profiles during application startup");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("S7Tools.Startup");
            logger?.LogError(ex, "Failed to retrieve ISerialPortProfileService during service initialization");
        }

        // Initialize Socat Profile storage in background to ensure profiles folder/file are created.
        try
        {
            var socatProfileService = serviceProvider.GetService<ISocatProfileService>();
            if (socatProfileService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await socatProfileService.GetAllAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var logger = serviceProvider.GetService<ILogger<ISocatProfileService>>();
                        logger?.LogError(ex, "Failed to initialize socat profile storage during application startup");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("S7Tools.Startup");
            logger?.LogError(ex, "Failed to retrieve ISocatProfileService during service initialization");
        }

        // Initialize Power Supply Profile storage in background to ensure profiles folder/file are created.
        try
        {
            var powerSupplyProfileService = serviceProvider.GetService<IPowerSupplyProfileService>();
            if (powerSupplyProfileService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await powerSupplyProfileService.GetAllAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var logger = serviceProvider.GetService<ILogger<IPowerSupplyProfileService>>();
                        logger?.LogError(ex, "Failed to initialize power supply profile storage during application startup");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("S7Tools.Startup");
            logger?.LogError(ex, "Failed to retrieve IPowerSupplyProfileService during service initialization");
        }
    }

    /// <summary>
    /// Shuts down S7Tools services gracefully.
    /// </summary>
    /// <param name="serviceProvider">The service provider to shut down services from.</param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    public static async Task ShutdownS7ToolsServicesAsync(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Save Layout Service configuration
        var layoutService = serviceProvider.GetService<ILayoutService>();
        if (layoutService != null)
        {
            await layoutService.SaveLayoutAsync().ConfigureAwait(false);
        }

        // Save Theme Service configuration
        var themeService = serviceProvider.GetService<IThemeService>();
        if (themeService != null)
        {
            await themeService.SaveThemeConfigurationAsync().ConfigureAwait(false);
        }

        // Dispose logging services
        var logDataStore = serviceProvider.GetService<S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore>();
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
