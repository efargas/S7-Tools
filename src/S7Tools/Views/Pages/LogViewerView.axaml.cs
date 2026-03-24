using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using S7Tools.ViewModels.Pages;

namespace S7Tools.Views.Pages;

/// <summary>
/// Code-behind for the LogViewerView user control.
/// Implements smart auto-scroll and clipboard integration for DataGrid.
/// </summary>
public partial class LogViewerView : UserControl
{
    private DataGrid? _logDataGrid;
    private ScrollViewer? _scrollViewer;
    private bool _isStuckToBottom = true;
    private bool _isInitializing = true;

    public LogViewerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _logDataGrid = this.FindControl<DataGrid>("LogDataGrid");
        if (_logDataGrid == null)
            return;

        // Find the ScrollViewer inside the DataGrid
        _scrollViewer = _logDataGrid.FindDescendantOfType<ScrollViewer>();

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
        }

        // Hook into collection changes for auto-scroll behavior
        if (DataContext is LogViewerViewModel viewModel)
        {
            var collectionView = viewModel.FilteredLogEntries;
            if (collectionView is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += OnCollectionChanged;
            }

            // Initial scroll to bottom
            _isInitializing = true;
            Dispatcher.UIThread.Post(() =>
            {
                ScrollToBottom();
                _isInitializing = false;
            });

            // Watch AutoScroll property changes
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
        }

        if (DataContext is LogViewerViewModel viewModel)
        {
            var collectionView = viewModel.FilteredLogEntries;
            if (collectionView is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogViewerViewModel.AutoScroll))
        {
            if (DataContext is LogViewerViewModel viewModel && viewModel.AutoScroll)
            {
                // Force scroll to bottom when AutoScroll is re-enabled
                _isStuckToBottom = true;
                ScrollToBottom();
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only auto-scroll if AutoScroll is enabled and we haven't scrolled away
        if (DataContext is LogViewerViewModel viewModel && viewModel.AutoScroll && _isStuckToBottom)
        {
            if (_isInitializing)
            {
                ScrollToBottom();
            }
            else
            {
                Dispatcher.UIThread.Post(ScrollToBottom);
            }
        }
    }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer == null || DataContext is not LogViewerViewModel viewModel)
            return;

        // Check if we're at the bottom
        bool isAtBottom = _scrollViewer.Offset.Y >= (_scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - 5.0);
        bool scrolledAwayFromBottom = e.OffsetDelta.Y < 0;
        bool scrolledTowardBottom = e.OffsetDelta.Y > 0;

        if (scrolledAwayFromBottom)
        {
            // User manually scrolled up - disable auto-scroll
            if (viewModel.AutoScroll)
            {
                viewModel.AutoScroll = false;
            }
            _isStuckToBottom = false;
        }
        else if (isAtBottom)
        {
            _isStuckToBottom = true;

            // Re-enable AutoScroll if user scrolled back to bottom
            if (!viewModel.AutoScroll && scrolledTowardBottom)
            {
                viewModel.AutoScroll = true;
            }
        }
    }

    private void ScrollToBottom()
    {
        if (_logDataGrid == null)
            return;

        if (DataContext is not LogViewerViewModel viewModel)
            return;

        var items = viewModel.FilteredLogEntries;
        if (items.Count == 0)
            return;

        _logDataGrid.ScrollIntoView(items[items.Count - 1], null);
    }
}
