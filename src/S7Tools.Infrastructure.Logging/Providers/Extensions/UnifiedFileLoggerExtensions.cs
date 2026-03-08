using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;
using S7Tools.Infrastructure.Logging.Core.Configuration;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

/// <summary>
/// Extension methods for registering the Unified File Logger.
/// </summary>
public static class UnifiedFileLoggerExtensions
{
    /// <summary>
    /// Adds the Unified File Logger to the logging builder.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration class.</typeparam>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configure">Delegate to configure the logger settings.</param>
    /// <returns>The modified logging builder.</returns>
    public static ILoggingBuilder AddUnifiedFileLogger<TConfiguration>(
        this ILoggingBuilder builder,
        Action<TConfiguration> configure)
        where TConfiguration : class, IFileLogConfiguration, new()
    {
        builder.Services.AddSingleton<ILoggerProvider, UnifiedLoggerProvider>();
        builder.Services.Configure(configure);
        return builder;
    }
}
