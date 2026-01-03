using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

/// <summary>
/// Extension methods for setting up the task file logger in an <see cref="ILoggingBuilder" />.
/// </summary>
public static class TaskFileLoggerExtensions
{
    /// <summary>
    /// Adds a task file logger named 'TaskFile' to the factory.
    /// </summary>
    public static ILoggingBuilder AddTaskFileLogger(this ILoggingBuilder builder, Action<IServiceProvider, TaskFileLoggerConfiguration> configure)
    {
        builder.Services.AddSingleton<ILoggerProvider, TaskFileLoggerProvider>(serviceProvider =>
        {
            var options = new S7Tools.Infrastructure.Logging.Core.Configuration.TaskFileLoggerConfiguration();
            configure(serviceProvider, options);
            var pathService = serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
            return new TaskFileLoggerProvider(Options.Create(options), pathService);
        });
        return builder;
    }
}
