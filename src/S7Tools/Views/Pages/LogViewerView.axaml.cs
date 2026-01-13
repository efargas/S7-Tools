using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using S7Tools.ViewModels.Pages;

using System.Diagnostics;

namespace S7Tools.Views.Pages;

/// <summary>
/// Code-behind for the LogViewerView user control.
/// </summary>
public partial class LogViewerView : UserControl
{
    private ListBox? _listBox;
    private ScrollViewer? _scrollViewer;
    private LogViewerViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the LogViewerView class.
    /// </summary>
    public LogViewerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _listBox = this.FindControl<ListBox>("LogListBox");

        // Find the ScrollViewer inside the ListBox once it's attached
        // We use the global capture or specific search as backup.
        // For ListBox, standard template has a ScrollViewer.
        if (_listBox != null)
        {
            _listBox.TemplateApplied += (s, args) =>
            {
                var sv = _listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
                if (sv != null)
                {
                    _scrollViewer = sv;
                    _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
                }
            };

            // Fallback if template already applied
            var sv = _listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (sv != null && _scrollViewer == null)
            {
                _scrollViewer = sv;
                _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            }
        }

        if (DataContext is LogViewerViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.FilteredLogEntries.CollectionChanged += OnLogEntriesChanged;

            if (_viewModel.AutoScroll && _viewModel.IsStuckToBottom)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(ScrollToBottom);
            }
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
            _scrollViewer = null; // Added this line to match original behavior
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.FilteredLogEntries.CollectionChanged -= OnLogEntriesChanged;
            _viewModel = null;
        }
        _listBox = null; // Added this line to match original behavior
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogViewerViewModel.AutoScroll))
        {
            if (_viewModel != null && _viewModel.AutoScroll)
            {
                // If AutoScroll is re-enabled, ensure we are stuck to bottom and scroll there.
                _viewModel.IsStuckToBottom = true; // Added this line to match original behavior
                ScrollToBottom();
            }
        }
    }

    private void OnLogEntriesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel?.AutoScroll == true && _viewModel.IsStuckToBottom)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ScrollToBottom);
        }
    }

    private void ScrollToBottom()
    {
        if (_viewModel == null || !_viewModel.AutoScroll || !_viewModel.IsStuckToBottom)
            return;

        if (_listBox != null && _viewModel.FilteredLogEntries.Count > 0)
        {
            try
            {
                var lastItem = _viewModel.FilteredLogEntries[_viewModel.FilteredLogEntries.Count - 1];
                _listBox.ScrollIntoView(lastItem);
            }
            catch { }
        }
    }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_viewModel == null || _scrollViewer == null)
            return;

        // Check if we are at the bottom using a safe tolerance (20px)
        bool isAtBottom = _scrollViewer.Offset.Y >= (_scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - 20.0);

        // Always sync the persistable state
        _viewModel.IsStuckToBottom = isAtBottom;

        // 1. Detection of Disabling: User scrolls UP
        if (e.OffsetDelta.Y < 0)
        {
            if (_viewModel.AutoScroll)
            {
                _viewModel.AutoScroll = false;
            }
        }

        // 2. Detection of Enabling: User at Bottom
        if (isAtBottom)
        {
            if (!_viewModel.AutoScroll)
            {
                // Only re-enable if user pushed down
                if (e.OffsetDelta.Y > 0)
                {
                    _viewModel.AutoScroll = true;
                }
            }
        }
    }
}
