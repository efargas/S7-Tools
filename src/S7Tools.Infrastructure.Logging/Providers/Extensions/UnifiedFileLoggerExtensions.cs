using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

public static class UnifiedFileLoggerExtensions
{
    public static ILoggingBuilder AddUnifiedFileLogger<TConfiguration>(this ILoggingBuilder builder, Action<TConfiguration> configure)
        where TConfiguration : class
    {
        builder.Services.AddSingleton<ILoggerProvider, UnifiedLoggerProvider>();
        builder.Services.Configure(configure);
        return builder;
    }
}
