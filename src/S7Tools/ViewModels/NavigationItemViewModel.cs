using System;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace S7Tools.ViewModels;

public class NavigationItemViewModel : ReactiveObject
{
    public string Header { get; }
    public IconSource IconSource { get; }
    public Type ContentViewModelType { get; }

    public NavigationItemViewModel(string header, IconSource iconSource, Type contentViewModelType)
    {
        Header = header;
        IconSource = iconSource;
        ContentViewModelType = contentViewModelType;
    }
}