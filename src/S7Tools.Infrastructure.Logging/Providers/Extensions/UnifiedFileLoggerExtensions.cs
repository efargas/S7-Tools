using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Providers.Extensions;

public static class UnifiedFileLoggerExtensions
{
    public static ILoggingBuilder AddUnifiedFileLogger<TConfig>(this ILoggingBuilder builder, Action<TConfig> configure)
        where TConfig : class, IFileLogConfiguration, new()
    {
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var config = new TConfig();
            configure(config);
            var options = Options.Create<IFileLogConfiguration>(config);
            var pathService = serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
            return new UnifiedFileLoggerProvider(options, pathService);
        });
        return builder;
    }
}
