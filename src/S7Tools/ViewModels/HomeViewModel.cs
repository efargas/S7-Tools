using ReactiveUI;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Home/Explorer view.
/// </summary>
public class HomeViewModel : ViewModelBase
{
    private readonly IViewModelFactory _viewModelFactory;

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
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The ViewModel factory for creating child ViewModels.</param>
    public HomeViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        DetailContent = _viewModelFactory.Create<AboutViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class for design-time.
    /// </summary>
    public HomeViewModel() : this(new DesignTimeViewModelFactory())
    {
        // Design-time factory used above; no external type reference needed
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

    public ViewModelBase Create(Type viewModelType)
    {
        if (viewModelType == typeof(AboutViewModel))
        {
            return new AboutViewModel();
        }

        throw new NotSupportedException($"Design-time factory does not support type {viewModelType.Name}");
    }
}
