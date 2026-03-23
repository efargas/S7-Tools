using S7Tools.ViewModels.Base;
using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Resources;

namespace S7Tools.ViewModels.Layout;

/// <summary>
/// Represents the SplashScreenViewModel.
/// </summary>
public class SplashScreenViewModel(IServiceProvider serviceProvider, ILogger<SplashScreenViewModel> logger) : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SplashScreenViewModel> _logger = logger;
    private string _statusText = "Initializing...";
    private double _progress;

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    /// <summary>
    /// Executes the InitializeAsync operation.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting application initialization sequence...");

            // Step 1: Path Service
            StatusText = "Initializing file system...";
            Progress = 10;
            var pathService = _serviceProvider.GetRequiredService<IPathService>();
            await pathService.InitializeAsync();
            _logger.LogInformation("Path service initialized.");

            // Step 2: Resource Manager
            StatusText = "Loading resources...";
            Progress = 30;
            var resourceService = _serviceProvider.GetRequiredService<IResourceManagerService>();
            var resourceResult = await resourceService.InitializeResourcesAsync();
            if (!resourceResult.Success)
            {
                _logger.LogWarning("Resource initialization had errors.");
            }
            _logger.LogInformation("Resources initialized.");

            // Step 3: Settings
            StatusText = "Loading settings...";
            Progress = 60;
            var settingsService = _serviceProvider.GetRequiredService<IApplicationSettingsService>();
            await settingsService.LoadSettingsAsync();
            _logger.LogInformation("Settings loaded.");

            // Step 4: Profile Services (Async background init in App.axaml.cs was effectively doing this, 
            // but we can do some critical parts here if needed, or leave the rest for background)
            // For now, we mirror the synchronous block's responsibility.

            StatusText = "Starting services...";
            Progress = 80;
            // UnifiedLogger/FileLogSink is auto-initialized by DI when requested found in App.axaml.cs logic
            // We can ensure it's ready by requesting it if we strictly mirrored the old logic, 
            // but DI handles it.

            StatusText = "Ready!";
            Progress = 100;
            await Task.Delay(200); // Visual comfort
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialization failed");
            StatusText = $"Error: {ex.Message}";
            // In a real app, we might show a retry button or exit. 
            // For now, rethrow or let the View handle it? 
            // We'll throw so App.axaml.cs knows we failed.
            throw;
        }
    }
}