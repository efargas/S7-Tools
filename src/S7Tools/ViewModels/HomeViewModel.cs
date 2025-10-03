using ReactiveUI;

namespace S7Tools.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Home!";

    private object? _detailContent;
    public object? DetailContent
    {
        get => _detailContent;
        set => this.RaiseAndSetIfChanged(ref _detailContent, value);
    }

    public HomeViewModel()
    {
        DetailContent = new AboutViewModel();
    }
}