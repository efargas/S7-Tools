using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.Views;
using Splat.Microsoft.Extensions.DependencyInjection;


namespace S7Tools;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

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
        // Services
        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITagRepository, PlcDataService>();
        services.AddSingleton<IS7ConnectionProvider, PlcDataService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Configure Splat to use the Microsoft.Extensions.DependencyInjection container.
        // This must be done during service configuration and before the service provider is built.
        services.UseMicrosoftDependencyResolver();


        // Views
        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>()
        });
    }
}
