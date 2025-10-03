using ReactiveUI;

namespace S7Tools.ViewModels;

public class ConnectionsViewModel : ViewModelBase
{
    public string Greeting => "Manage your connections here.";

    private object? _detailContent;
    public object? DetailContent
    {
        get => _detailContent;
        set => this.RaiseAndSetIfChanged(ref _detailContent, value);
    }

    public ConnectionsViewModel()
    {
        DetailContent = new AboutViewModel();
    }
}