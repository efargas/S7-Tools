using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Extensions;
using S7Tools.ViewModels;
using S7Tools.Views;
using S7Tools.Services.Interfaces;
using S7Tools.Services;
using Splat.Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using S7Tools.Infrastructure.Logging.Providers.Extensions;
using S7Tools.Infrastructure.Logging.Core.Models;


namespace S7Tools;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        IconProvider.Current.Register<FontAwesomeIconProvider>();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
        => AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();


    private static void ConfigureServices(IServiceCollection services)
    {
        // Add logging with DataStore provider
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            
            // Add DataStore logging provider
            builder.AddDataStore(options =>
            {
                options.MaxEntries = 10000;
            }, config =>
            {
                // Configure logger settings
                config.LogLevel = LogLevel.Debug;
                config.IncludeScopes = true;
                config.CaptureProperties = true;
                config.FormatMessages = true;
                config.CaptureStackTrace = true;
                config.MaxMessageLength = 10000;
            });
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
