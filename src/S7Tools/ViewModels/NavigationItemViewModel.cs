using ReactiveUI;

namespace S7Tools.ViewModels;

public class NavigationItemViewModel : ReactiveObject
{
    private string _icon = string.Empty;
    public string Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }
}