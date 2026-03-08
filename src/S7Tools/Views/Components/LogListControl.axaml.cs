using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace S7Tools.Views.Components;

public partial class LogListControl : UserControl
{
    private ScrollViewer? _scrollViewer;
    private bool _isStuckToBottom = true;

    public LogListControl()
    {
        InitializeComponent();

        // Find the ScrollViewer that is defined in the XAML
        // Note: InitializeComponent loads the XAML content, so FindControl should work immediately after
        _scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
        }

        if (ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnCollectionChanged;
        }

        // Initial scroll if needed
        if (AutoScroll)
        {
            _isStuckToBottom = true;
            ScrollToBottom();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
        }

        if (ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnCollectionChanged;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= OnCollectionChanged;
            }
            if (change.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += OnCollectionChanged;
            }
        }
        else if (change.Property == AutoScrollProperty)
        {
            if (AutoScroll)
            {
                // Force scroll to bottom when re-enabled
                _isStuckToBottom = true;
                ScrollToBottom();
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (AutoScroll && _isStuckToBottom)
        {
            Dispatcher.UIThread.Post(ScrollToBottom);
        }
    }

    private void ScrollToBottom()
    {
        if (!AutoScroll || !_isStuckToBottom || _scrollViewer == null)
        {
            return;
        }

        // Use ScrollToEnd which is more reliable for ScrollViewer than ScrollIntoView for listbox usually,
        // but ScrollIntoView is good for knowing WHICH item.
        // However, we own the ScrollViewer here.
        _scrollViewer.ScrollToEnd();
    }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer == null)
        {
            return;
        }

        // Tolerance for floating point comparison
        bool isAtBottom = _scrollViewer.Offset.Y >= (_scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - 5.0);

        // Update stickiness state
        // If we were stuck, we stay stuck strictly if at bottom.
        // But if user scrolls up, we become unstuck.

        if (e.OffsetDelta.Y < 0) // User scrolled UP
        {
            if (AutoScroll)
            {
                // Disable autoscroll if user manually scrolls away
                SetCurrentValue(AutoScrollProperty, false);
            }
            _isStuckToBottom = false;
        }
        else if (isAtBottom)
        {
            _isStuckToBottom = true;

            // Optional: Re-enable AutoScroll if user scrolled to bottom?
            // Standard terminal behavior often re-enables lock when hitting bottom.
            // Let's mimic that behavior.
            if (!AutoScroll && e.OffsetDelta.Y > 0)
            {
                SetCurrentValue(AutoScrollProperty, true);
            }
        }
    }

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LogListControl, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<LogListControl, IDataTemplate?>(nameof(ItemTemplate));

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly StyledProperty<bool> AutoScrollProperty =
        AvaloniaProperty.Register<LogListControl, bool>(nameof(AutoScroll), defaultValue: true);

    public bool AutoScroll
    {
        get => GetValue(AutoScrollProperty);
        set => SetValue(AutoScrollProperty, value);
    }

    public static readonly StyledProperty<ICommand?> CopyCommandProperty =
        AvaloniaProperty.Register<LogListControl, ICommand?>(nameof(CopyCommand));

    public ICommand? CopyCommand
    {
        get => GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }
}
