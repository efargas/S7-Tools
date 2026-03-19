using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for appearance settings configuration.
/// </summary>
public class AppearanceSettingsViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settingsService;
    private string _theme;

    /// <summary>
    /// Initializes a new instance of the AppearanceSettingsViewModel class.
    /// </summary>
    public AppearanceSettingsViewModel(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;
        _theme = _settingsService.Current.Ui.Theme;

        RestoreDefaultsCommand = ReactiveCommand.Create(RestoreDefaults);

        // When Theme property changes, auto-save to configuration
        this.WhenAnyValue(x => x.Theme)
            .Subscribe(newTheme =>
            {
                if (newTheme != _settingsService.Current.Ui.Theme)
                {
                    _ = _settingsService.UpdateSettingsAsync(settings => settings.Ui.Theme = newTheme);
                }
            });
    }

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public AppearanceSettingsViewModel()
    {
        _settingsService = null!; // Dummy for designer
        _theme = "Dark";
        RestoreDefaultsCommand = ReactiveCommand.Create(() => {});
    }

    /// <summary>
    /// Gets or sets the application Theme variant.
    /// Can be 'Dark', 'Light', or 'System'.
    /// </summary>
    public string Theme
    {
        get => _theme;
        set => this.RaiseAndSetIfChanged(ref _theme, value);
    }

    /// <summary>
    /// Command to restore the default theme (System).
    /// </summary>
    public ReactiveCommand<Unit, Unit> RestoreDefaultsCommand { get; }

    private void RestoreDefaults()
    {
        Theme = "System";
    }
}
