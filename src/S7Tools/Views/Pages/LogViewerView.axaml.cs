using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Threading;
using S7Tools.ViewModels.Pages;

namespace S7Tools.Views.Pages;

public partial class LogViewerView : UserControl
{
    private bool _autoScroll = true;
    private ScrollViewer? _scrollViewer;

    public LogViewerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        LogListBox.AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged, RoutingStrategies.Bubble);

        if (DataContext is LogViewerViewModel vm)
        {
            if (vm.FilteredLogEntries is INotifyCollectionChanged c)
            {
                c.CollectionChanged += OnCollectionChanged;
            }

            vm.PropertyChanged += OnViewModelPropertyChanged;

            Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Render);
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        LogListBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);

        if (DataContext is LogViewerViewModel vm)
        {
            if (vm.FilteredLogEntries is INotifyCollectionChanged c)
            {
                c.CollectionChanged -= OnCollectionChanged;
            }

            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source is ScrollViewer sv)
        {
            _scrollViewer = sv;
        }

        if (_scrollViewer == null)
        {
            return;
        }

        double scrollable = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
        bool atBottom = scrollable <= 0 || _scrollViewer.Offset.Y >= scrollable - 5; // Added small tolerance

        _autoScroll = atBottom;

        if (DataContext is LogViewerViewModel vm && vm.AutoScroll != atBottom)
        {
            vm.AutoScroll = atBottom;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_autoScroll)
        {
            return;
        }

        Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Render);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LogViewerViewModel.AutoScroll))
        {
            return;
        }

        if (DataContext is LogViewerViewModel vm && vm.AutoScroll)
        {
            _autoScroll = true;
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Extent.Height);
        }
        else if (DataContext is LogViewerViewModel vm && vm.FilteredLogEntries.Count > 0)
        {
            LogListBox.ScrollIntoView(vm.FilteredLogEntries.Count - 1);
        }
    }
}
