using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services
{
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger<ApplicationSettingsService> _logger;
        private readonly IWritableOptions<AppSettings> _options;
        private readonly IConfigurationRoot? _configurationRoot;
        private readonly IUIThreadService? _uiThreadService;

        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public ApplicationSettingsService(
            ILogger<ApplicationSettingsService> logger,
            IWritableOptions<AppSettings> options,
            IConfiguration? configuration = null,
            IUIThreadService? uiThreadService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _configurationRoot = configuration as IConfigurationRoot;
            _uiThreadService = uiThreadService;
            _logger.LogInformation("ApplicationSettingsService initialized as strongly-typed proxy");
        }

        public AppSettings Current => _options.CurrentValue;

        public async Task LoadSettingsAsync()
        {
            if (_configurationRoot is null)
            {
                _logger.LogWarning("Cannot reload application settings because configuration root is not available.");
                return;
            }

            _logger.LogInformation("Reloading application settings from current configuration source.");
            try
            {
                await Task.Run(() => _configurationRoot.Reload()).ConfigureAwait(false);
                RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload application settings from configuration source.");
                throw;
            }
        }

        public async Task UpdateSettingsAsync(Action<AppSettings> updateAction)
        {
            await _options.UpdateAsync(settings =>
            {
                updateAction(settings);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = true });
        }

        public async Task ResetAllSettingsAsync()
        {
            await _options.UpdateAsync(s =>
            {
                var def = new AppSettings();
                s.Logging = def.Logging;
                s.Ui = def.Ui;
                s.Paths = def.Paths;
                s.Profiles = def.Profiles;
                s.PowerSupply = def.PowerSupply;
                s.MemoryDump = def.MemoryDump;
                s.MemoryRegion = def.MemoryRegion;
                s.Export = def.Export;
                s.Plc = def.Plc;
                s.Jobs = def.Jobs;
                s.Tasks = def.Tasks;
                s.Serial = def.Serial;
                s.Network = def.Network;
                s.Socat = def.Socat;
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = false });
        }

        public Task RestoreDefaultsAsync() => ResetAllSettingsAsync();

        private void RaiseSettingsChanged(SettingsChangedEventArgs args)
        {
            var handler = SettingsChanged;
            if (handler is null) return;

            if (_uiThreadService is not null)
            {
                _uiThreadService.PostToUIThread(() => handler(this, args));
            }
            else
            {
                handler(this, args);
            }
        }
    }
}
