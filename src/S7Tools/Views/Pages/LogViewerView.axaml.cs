using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using S7Tools.ViewModels.Pages;

namespace S7Tools.Views.Pages;

/// <summary>
/// Code-behind for the LogViewerView user control.
/// </summary>
public partial class LogViewerView : UserControl
{
    private ScrollViewer? _logScrollViewer;
    private bool _isUserScrolling;

    /// <summary>
    /// Initializes a new instance of the LogViewerView class.
    /// </summary>
    public LogViewerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the loaded event to set up auto-scroll behavior.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");

        if (DataContext is LogViewerViewModel viewModel && _logScrollViewer != null)
        {
            viewModel.FilteredLogEntries.CollectionChanged += OnLogEntriesChanged;
            _logScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
        }
    }

    /// <summary>
    /// Handles the unloaded event to clean up subscriptions.
    /// </summary>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (DataContext is LogViewerViewModel viewModel && _logScrollViewer != null)
        {
            viewModel.FilteredLogEntries.CollectionChanged -= OnLogEntriesChanged;
            _logScrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
        }
        base.OnUnloaded(e);
    }

    /// <summary>
    /// Handles changes to the log entries collection to trigger auto-scroll.
    /// </summary>
    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is LogViewerViewModel { AutoScroll: true } && !_isUserScrolling)
        {
            _logScrollViewer?.ScrollToEnd();
        }
    }

    /// <summary>
    /// Manages the auto-scroll behavior when the user manually scrolls.
    /// </summary>
    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_logScrollViewer == null) return;

        // A small tolerance is needed for comparing double values
        bool isAtBottom = _logScrollViewer.Offset.Y >= _logScrollViewer.ScrollBarMaximum.Y - 1.0;

        if (e.ExtentDelta.Y != 0)
        {
            // If the user scrolls up, disable auto-scroll
            if (!isAtBottom)
            {
                _isUserScrolling = true;
            }
            // If the user scrolls back to the bottom, re-enable auto-scroll
            else
            {
                _isUserScrolling = false;
            }
        }
    }
}
