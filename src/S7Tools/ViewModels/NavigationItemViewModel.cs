using System;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace S7Tools.ViewModels;

public class NavigationItemViewModel : ReactiveObject
{
    public string Header { get; }
    public string Icon { get; }
    public Type ContentViewModelType { get; }

    public NavigationItemViewModel(string header, string icon, Type contentViewModelType)
    {
        Header = header;
        Icon = icon;
        ContentViewModelType = contentViewModelType;
    }
}