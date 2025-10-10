using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

/// <summary>
/// Extension methods for configuring DataStore logging services.
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds DataStore logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional configuration action for DataStore options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataStoreLogging(
        this IServiceCollection services,
        Action<LogDataStoreOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LogDataStoreOptions>(options => { });
        }

        // Register the data store as singleton
        services.TryAddSingleton<ILogDataStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LogDataStoreOptions>>().Value;
            return new LogDataStore(options);
        });

        // Register logger configuration
        services.TryAddSingleton<DataStoreLoggerConfiguration>();

        // Register the logger provider
        services.TryAddSingleton<DataStoreLoggerProvider>(serviceProvider =>
        {
            var dataStore = serviceProvider.GetRequiredService<ILogDataStore>();
            var configuration = serviceProvider.GetRequiredService<DataStoreLoggerConfiguration>();
            return new DataStoreLoggerProvider(dataStore, configuration);
        });

        return services;
    }

    /// <summary>
    /// Adds DataStore logging services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureDataStore">Configuration action for DataStore options.</param>
    /// <param name="configureLogger">Configuration action for logger configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataStoreLogging(
        this IServiceCollection services,
        Action<LogDataStoreOptions> configureDataStore,
        Action<DataStoreLoggerConfiguration> configureLogger)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureDataStore == null)
        {
            throw new ArgumentNullException(nameof(configureDataStore));
        }

        if (configureLogger == null)
        {
            throw new ArgumentNullException(nameof(configureLogger));
        }

        // Configure DataStore options
        services.Configure(configureDataStore);

        // Register the data store as singleton
        services.TryAddSingleton<ILogDataStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LogDataStoreOptions>>().Value;
            return new LogDataStore(options);
        });

        // Register and configure logger configuration
        services.TryAddSingleton<DataStoreLoggerConfiguration>(serviceProvider =>
        {
            var configuration = new DataStoreLoggerConfiguration();
            configureLogger(configuration);
            return configuration;
        });

        // Register the logger provider
        services.TryAddSingleton<DataStoreLoggerProvider>(serviceProvider =>
        {
            var dataStore = serviceProvider.GetRequiredService<ILogDataStore>();
            var configuration = serviceProvider.GetRequiredService<DataStoreLoggerConfiguration>();
            return new DataStoreLoggerProvider(dataStore, configuration);
        });

        return services;
    }

    /// <summary>
    /// Adds DataStore logging to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder to add the provider to.</param>
    /// <param name="configure">Optional configuration action for logger configuration.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDataStore(
        this ILoggingBuilder builder,
        Action<DataStoreLoggerConfiguration>? configure = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Ensure DataStore services are registered
        builder.Services.AddDataStoreLogging();

        // Configure logger if needed
        if (configure != null)
        {
            builder.Services.Configure<DataStoreLoggerConfiguration>(config => configure(config));
        }

        // Add the logger provider to the logging system
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DataStoreLoggerProvider>(
            serviceProvider => serviceProvider.GetRequiredService<DataStoreLoggerProvider>()));

        return builder;
    }

    /// <summary>
    /// Adds DataStore logging to the logging builder with custom options.
    /// </summary>
    /// <param name="builder">The logging builder to add the provider to.</param>
    /// <param name="configureDataStore">Configuration action for DataStore options.</param>
    /// <param name="configureLogger">Configuration action for logger configuration.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDataStore(
        this ILoggingBuilder builder,
        Action<LogDataStoreOptions> configureDataStore,
        Action<DataStoreLoggerConfiguration> configureLogger)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Register DataStore services with configuration
        builder.Services.AddDataStoreLogging(configureDataStore, configureLogger);

        // Add the logger provider to the logging system
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DataStoreLoggerProvider>(
            serviceProvider => serviceProvider.GetRequiredService<DataStoreLoggerProvider>()));

        return builder;
    }

    /// <summary>
    /// Gets the DataStore logger provider from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to get the provider from.</param>
    /// <returns>The DataStore logger provider instance.</returns>
    public static DataStoreLoggerProvider GetDataStoreLoggerProvider(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        return serviceProvider.GetRequiredService<DataStoreLoggerProvider>();
    }

    /// <summary>
    /// Gets the log data store from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to get the data store from.</param>
    /// <returns>The log data store instance.</returns>
    public static ILogDataStore GetLogDataStore(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        return serviceProvider.GetRequiredService<ILogDataStore>();
    }
}
