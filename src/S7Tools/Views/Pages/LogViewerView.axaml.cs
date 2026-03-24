using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using S7Tools.ViewModels.Pages;

namespace S7Tools.Views.Pages;

public partial class LogViewerView : UserControl
{
    private bool _autoScroll = true;

    public LogViewerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        LogScrollViewer.ScrollChanged += OnScrollChanged;

        if (DataContext is LogViewerViewModel vm)
        {
            if (vm.FilteredLogEntries is INotifyCollectionChanged c)
                c.CollectionChanged += OnCollectionChanged;

            vm.PropertyChanged += OnViewModelPropertyChanged;

            Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Render);
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        LogScrollViewer.ScrollChanged -= OnScrollChanged;

        if (DataContext is LogViewerViewModel vm)
        {
            if (vm.FilteredLogEntries is INotifyCollectionChanged c)
                c.CollectionChanged -= OnCollectionChanged;

            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var sv = LogScrollViewer;
        double scrollable = sv.Extent.Height - sv.Viewport.Height;
        bool atBottom = scrollable <= 0 || sv.Offset.Y >= scrollable - 1;

        _autoScroll = atBottom;

        if (DataContext is LogViewerViewModel vm && vm.AutoScroll != atBottom)
            vm.AutoScroll = atBottom;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_autoScroll)
            return;

        Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Render);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LogViewerViewModel.AutoScroll))
            return;

        if (DataContext is LogViewerViewModel vm && vm.AutoScroll)
        {
            _autoScroll = true;
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        var sv = LogScrollViewer;
        sv.Offset = new Vector(sv.Offset.X, sv.Extent.Height);
    }
}
