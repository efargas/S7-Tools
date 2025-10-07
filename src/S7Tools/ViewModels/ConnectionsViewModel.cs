using ReactiveUI;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Connections view.
/// </summary>
public class ConnectionsViewModel : ViewModelBase
{
    private readonly IViewModelFactory _viewModelFactory;
    
    /// <summary>
    /// Gets the greeting message.
    /// </summary>
    public string Greeting => "Manage your connections here.";

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
    /// Initializes a new instance of the <see cref="ConnectionsViewModel"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The ViewModel factory for creating child ViewModels.</param>
    public ConnectionsViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        DetailContent = _viewModelFactory.Create<AboutViewModel>();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsViewModel"/> class for design-time.
    /// </summary>
    public ConnectionsViewModel() : this(new DesignTimeViewModelFactory())
    {
    }
}