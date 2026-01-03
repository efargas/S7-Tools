using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

/// <summary>
/// Extension methods for setting up the file logger in an <see cref="ILoggingBuilder" />.
/// </summary>
public static class FileLoggerExtensions
{
    /// <summary>
    /// Adds a file logger named 'File' to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="configure">A delegate to configure the <see cref="FileLoggerConfiguration"/>.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerConfiguration> configure)
    {
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileLoggerConfiguration>>();
            var pathService = serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
            return new FileLoggerProvider(config, pathService);
        });
        builder.Services.Configure(configure);
        return builder;
    }
}
