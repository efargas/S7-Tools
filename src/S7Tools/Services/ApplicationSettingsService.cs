using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;

namespace S7Tools.Services
{
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger<ApplicationSettingsService> _logger;
        private readonly IWritableOptions<AppSettings> _options;

        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public ApplicationSettingsService(ILogger<ApplicationSettingsService> logger, IWritableOptions<AppSettings> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger.LogInformation("ApplicationSettingsService initialized as strongly-typed proxy");
        }

        public AppSettings Current => _options.CurrentValue;

        public Task LoadSettingsAsync()
        {
            _logger.LogInformation("Reloading application settings from current configuration source.");
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { IsUserSetting = false });
            return Task.CompletedTask;
        }

        public Task UpdateSettingsAsync(Action<AppSettings> updateAction)
        {
            _options.Update(settings =>
            {
                updateAction(settings);
            });
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { IsUserSetting = true });
            return Task.CompletedTask;
        }

        public Task ResetAllSettingsAsync()
        {
            _options.Update(s =>
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
            });
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { IsUserSetting = false });
            return Task.CompletedTask;
        }

        public Task RestoreDefaultsAsync() => ResetAllSettingsAsync();
    }
}
