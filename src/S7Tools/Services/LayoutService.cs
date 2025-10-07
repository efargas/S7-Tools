using S7Tools.Services.Interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace S7Tools.Services;

/// <summary>
/// Service for managing the application layout state and configuration.
/// </summary>
public sealed class LayoutService : ILayoutService
{
    private const string LayoutConfigFileName = "layout.json";
    private const double DefaultSidebarWidth = 300;
    private const double DefaultBottomPanelHeight = 200;
    private const double DefaultActivityBarWidth = 48;
    private const double DefaultMinSidebarWidth = 200;
    private const double DefaultMaxSidebarWidth = 600;
    private const double DefaultMinBottomPanelHeight = 100;
    private const double DefaultMaxBottomPanelHeight = 400;

    private bool _isSidebarVisible = true;
    private bool _isBottomPanelVisible = true;
    private bool _isActivityBarVisible = true;
    private bool _isStatusBarVisible = true;
    private bool _isMenuBarVisible = true;
    private double _sidebarWidth = DefaultSidebarWidth;
    private double _bottomPanelHeight = DefaultBottomPanelHeight;
    private double _activityBarWidth = DefaultActivityBarWidth;
    private double _minSidebarWidth = DefaultMinSidebarWidth;
    private double _maxSidebarWidth = DefaultMaxSidebarWidth;
    private double _minBottomPanelHeight = DefaultMinBottomPanelHeight;
    private double _maxBottomPanelHeight = DefaultMaxBottomPanelHeight;
    private bool _isSidebarCollapsed = false;
    private bool _isBottomPanelCollapsed = false;

    private double _previousSidebarWidth = DefaultSidebarWidth;
    private double _previousBottomPanelHeight = DefaultBottomPanelHeight;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

    /// <inheritdoc />
    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => SetProperty(ref _isSidebarVisible, value);
    }

    /// <inheritdoc />
    public bool IsBottomPanelVisible
    {
        get => _isBottomPanelVisible;
        set => SetProperty(ref _isBottomPanelVisible, value);
    }

    /// <inheritdoc />
    public bool IsActivityBarVisible
    {
        get => _isActivityBarVisible;
        set => SetProperty(ref _isActivityBarVisible, value);
    }

    /// <inheritdoc />
    public bool IsStatusBarVisible
    {
        get => _isStatusBarVisible;
        set => SetProperty(ref _isStatusBarVisible, value);
    }

    /// <inheritdoc />
    public bool IsMenuBarVisible
    {
        get => _isMenuBarVisible;
        set => SetProperty(ref _isMenuBarVisible, value);
    }

    /// <inheritdoc />
    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => SetProperty(ref _sidebarWidth, Math.Max(_minSidebarWidth, Math.Min(_maxSidebarWidth, value)));
    }

    /// <inheritdoc />
    public double BottomPanelHeight
    {
        get => _bottomPanelHeight;
        set => SetProperty(ref _bottomPanelHeight, Math.Max(_minBottomPanelHeight, Math.Min(_maxBottomPanelHeight, value)));
    }

    /// <inheritdoc />
    public double ActivityBarWidth
    {
        get => _activityBarWidth;
        set => SetProperty(ref _activityBarWidth, Math.Max(32, Math.Min(100, value)));
    }

    /// <inheritdoc />
    public double MinSidebarWidth
    {
        get => _minSidebarWidth;
        set => SetProperty(ref _minSidebarWidth, Math.Max(100, value));
    }

    /// <inheritdoc />
    public double MaxSidebarWidth
    {
        get => _maxSidebarWidth;
        set => SetProperty(ref _maxSidebarWidth, Math.Max(_minSidebarWidth, value));
    }

    /// <inheritdoc />
    public double MinBottomPanelHeight
    {
        get => _minBottomPanelHeight;
        set => SetProperty(ref _minBottomPanelHeight, Math.Max(50, value));
    }

    /// <inheritdoc />
    public double MaxBottomPanelHeight
    {
        get => _maxBottomPanelHeight;
        set => SetProperty(ref _maxBottomPanelHeight, Math.Max(_minBottomPanelHeight, value));
    }

    /// <inheritdoc />
    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    /// <inheritdoc />
    public bool IsBottomPanelCollapsed
    {
        get => _isBottomPanelCollapsed;
        set => SetProperty(ref _isBottomPanelCollapsed, value);
    }

    /// <inheritdoc />
    public void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    /// <inheritdoc />
    public void ToggleBottomPanel()
    {
        IsBottomPanelVisible = !IsBottomPanelVisible;
    }

    /// <inheritdoc />
    public void ToggleActivityBar()
    {
        IsActivityBarVisible = !IsActivityBarVisible;
    }

    /// <inheritdoc />
    public void ToggleStatusBar()
    {
        IsStatusBarVisible = !IsStatusBarVisible;
    }

    /// <inheritdoc />
    public void ToggleMenuBar()
    {
        IsMenuBarVisible = !IsMenuBarVisible;
    }

    /// <inheritdoc />
    public void CollapseSidebar()
    {
        if (!IsSidebarCollapsed)
        {
            _previousSidebarWidth = SidebarWidth;
            SidebarWidth = MinSidebarWidth;
            IsSidebarCollapsed = true;
        }
    }

    /// <inheritdoc />
    public void ExpandSidebar()
    {
        if (IsSidebarCollapsed)
        {
            SidebarWidth = _previousSidebarWidth;
            IsSidebarCollapsed = false;
        }
    }

    /// <inheritdoc />
    public void CollapseBottomPanel()
    {
        if (!IsBottomPanelCollapsed)
        {
            _previousBottomPanelHeight = BottomPanelHeight;
            BottomPanelHeight = MinBottomPanelHeight;
            IsBottomPanelCollapsed = true;
        }
    }

    /// <inheritdoc />
    public void ExpandBottomPanel()
    {
        if (IsBottomPanelCollapsed)
        {
            BottomPanelHeight = _previousBottomPanelHeight;
            IsBottomPanelCollapsed = false;
        }
    }

    /// <inheritdoc />
    public void ResetLayout()
    {
        IsSidebarVisible = true;
        IsBottomPanelVisible = true;
        IsActivityBarVisible = true;
        IsStatusBarVisible = true;
        IsMenuBarVisible = true;
        SidebarWidth = DefaultSidebarWidth;
        BottomPanelHeight = DefaultBottomPanelHeight;
        ActivityBarWidth = DefaultActivityBarWidth;
        IsSidebarCollapsed = false;
        IsBottomPanelCollapsed = false;
        _previousSidebarWidth = DefaultSidebarWidth;
        _previousBottomPanelHeight = DefaultBottomPanelHeight;
    }

    /// <inheritdoc />
    public async Task SaveLayoutAsync()
    {
        try
        {
            var configuration = GetCurrentConfiguration();
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var configPath = GetConfigFilePath();
            await File.WriteAllTextAsync(configPath, json).ConfigureAwait(false);
        }
        catch
        {
            // Silently ignore save errors to prevent disrupting the application
        }
    }

    /// <inheritdoc />
    public async Task LoadLayoutAsync()
    {
        try
        {
            var configPath = GetConfigFilePath();
            if (!File.Exists(configPath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var configuration = JsonSerializer.Deserialize<LayoutConfiguration>(json);
            
            if (configuration != null)
            {
                ApplyConfiguration(configuration);
            }
        }
        catch
        {
            // Silently ignore load errors and use default layout
        }
    }

    /// <inheritdoc />
    public LayoutConfiguration GetCurrentConfiguration()
    {
        return new LayoutConfiguration
        {
            IsSidebarVisible = IsSidebarVisible,
            IsBottomPanelVisible = IsBottomPanelVisible,
            IsActivityBarVisible = IsActivityBarVisible,
            IsStatusBarVisible = IsStatusBarVisible,
            IsMenuBarVisible = IsMenuBarVisible,
            SidebarWidth = SidebarWidth,
            BottomPanelHeight = BottomPanelHeight,
            ActivityBarWidth = ActivityBarWidth,
            IsSidebarCollapsed = IsSidebarCollapsed,
            IsBottomPanelCollapsed = IsBottomPanelCollapsed
        };
    }

    /// <inheritdoc />
    public void ApplyConfiguration(LayoutConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        IsSidebarVisible = configuration.IsSidebarVisible;
        IsBottomPanelVisible = configuration.IsBottomPanelVisible;
        IsActivityBarVisible = configuration.IsActivityBarVisible;
        IsStatusBarVisible = configuration.IsStatusBarVisible;
        IsMenuBarVisible = configuration.IsMenuBarVisible;
        SidebarWidth = configuration.SidebarWidth;
        BottomPanelHeight = configuration.BottomPanelHeight;
        ActivityBarWidth = configuration.ActivityBarWidth;
        IsSidebarCollapsed = configuration.IsSidebarCollapsed;
        IsBottomPanelCollapsed = configuration.IsBottomPanelCollapsed;
    }

    private void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        var oldValue = field;
        field = value;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        if (propertyName != null)
        {
            LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(propertyName, oldValue, value));
        }
    }

    private static string GetConfigFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "S7Tools");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        return Path.Combine(appFolder, LayoutConfigFileName);
    }
}