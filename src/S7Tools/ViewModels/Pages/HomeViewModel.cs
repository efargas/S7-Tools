using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for the Home/Explorer view.
/// </summary>
public class HomeViewModel : ViewModelBase, IDockableViewModel
{
    // IDockableViewModel implementation
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "Home";

    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Home";

    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => false; // Home should typically stay open

    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private readonly IViewModelFactory _viewModelFactory;
    private readonly IActivityBarService _activityBarService;
    private readonly ILogExportService _logExportService;
    private readonly ILogDataStore? _logDataStore;

    /// <summary>
    /// Gets the greeting message.
    /// </summary>
    public string Greeting => "Welcome to Home!";

    private object? _detailContent;
    /// <summary>
    /// Gets or sets the detail content displayed in the main area.
    /// </summary>
    public object? DetailContent
    {
        get => _detailContent;
        set => this.RaiseAndSetIfChanged(ref _detailContent, value);
    }

    /// <summary>
    /// Gets the command to create a new job.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewJobCommand { get; }

    /// <summary>
    /// Gets the command to open the live console.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LiveConsoleCommand { get; }

    /// <summary>
    /// Gets the command to export logs.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportLogsCommand { get; }

    /// <summary>
    /// Gets the command to check system health.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CheckHealthCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The ViewModel factory.</param>
    /// <param name="activityBarService">The activity bar service.</param>
    /// <param name="logExportService">The log export service.</param>
    /// <param name="logDataStore">The log data store (optional).</param>
    public HomeViewModel(
        IViewModelFactory viewModelFactory,
        IActivityBarService activityBarService,
        ILogExportService logExportService,
        ILogDataStore? logDataStore = null)
    {
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _activityBarService = activityBarService ?? throw new ArgumentNullException(nameof(activityBarService));
        _logExportService = logExportService ?? throw new ArgumentNullException(nameof(logExportService));
        _logDataStore = logDataStore;

        DetailContent = _viewModelFactory.Create<AboutViewModel>();

        NewJobCommand = ReactiveCommand.Create(() => { _activityBarService.SelectItem("jobs"); });
        LiveConsoleCommand = ReactiveCommand.Create(() => { _activityBarService.SelectItem("logviewer"); });

        ExportLogsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            IEnumerable<LogModel> logs = _logDataStore?.Entries ?? Array.Empty<LogModel>();
            await _logExportService.ExportLogsAsync(logs, ExportFormat.Text, "logs_export.txt");
        });

        CheckHealthCommand = ReactiveCommand.Create(() =>
        {
            // Simple placeholder for health check
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class for design-time.
    /// </summary>
    public HomeViewModel()
    {
        // Minimal design-time setup to satisfy non-nullable requirements
        _viewModelFactory = null!;
        _activityBarService = null!;
        _logExportService = null!;

        NewJobCommand = ReactiveCommand.Create(() => { });
        LiveConsoleCommand = ReactiveCommand.Create(() => { });
        ExportLogsCommand = ReactiveCommand.CreateFromTask(() => Task.CompletedTask);
        CheckHealthCommand = ReactiveCommand.Create(() => { });
    }
}

/// <summary>
/// Design-time implementation of IViewModelFactory for XAML designer support.
/// </summary>
internal class DesignTimeViewModelFactory : IViewModelFactory
{
    public T Create<T>() where T : ViewModelBase
    {
        if (typeof(T) == typeof(AboutViewModel))
        {
            return (T)(object)new AboutViewModel();
        }

        throw new NotSupportedException($"Design-time factory does not support type {typeof(T).Name}");
    }

    /// <summary>
    /// Executes the Create operation.
    /// </summary>
    public ViewModelBase Create(Type viewModelType)
    {
        if (viewModelType == typeof(AboutViewModel))
        {
            return new AboutViewModel();
        }

        throw new NotSupportedException($"Design-time factory does not support type {viewModelType.Name}");
    }
}
