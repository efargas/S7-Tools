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
    private ListBox? _listBox;
    private bool _isStuckToTarget = true;

    public LogListControl()
    {
        InitializeComponent();

        _listBox = this.FindControl<ListBox>("LogListBox");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_listBox != null)
        {
            _listBox.AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollViewerScrollChanged, RoutingStrategies.Bubble);
        }

        if (ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnCollectionChanged;
        }

        // Initial scroll if needed
        if (AutoScroll)
        {
            _isStuckToTarget = true;
            ScrollToTarget();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_listBox != null)
        {
            _listBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollViewerScrollChanged);
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
                // Force scroll to target when re-enabled
                _isStuckToTarget = true;
                ScrollToTarget();
            }
        }
        else if (change.Property == InvertAutoScrollDirectionProperty && AutoScroll)
        {
            // If the direction changed, force a scroll to the new target
            _isStuckToTarget = true;
            ScrollToTarget();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (AutoScroll && _isStuckToTarget)
        {
            Dispatcher.UIThread.Post(ScrollToTarget);
        }
    }

    private void ScrollToTarget()
    {
        if (!AutoScroll || !_isStuckToTarget)
        {
            return;
        }

        if (_scrollViewer != null)
        {
            if (InvertAutoScrollDirection)
            {
                _scrollViewer.Offset = new Avalonia.Vector(_scrollViewer.Offset.X, 0);
            }
            else
            {
                _scrollViewer.ScrollToEnd();
            }
        }
        else if (_listBox != null && ItemsSource is IList list && list.Count > 0)
        {
            if (InvertAutoScrollDirection)
            {
                _listBox.ScrollIntoView(0);
            }
            else
            {
                _listBox.ScrollIntoView(list.Count - 1);
            }
        }
    }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source is ScrollViewer sv)
        {
            _scrollViewer = sv;
        }

        if (_scrollViewer == null)
        {
            return;
        }

        // Tolerance for floating point comparison
        bool isAtBottom = _scrollViewer.Offset.Y >= (_scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - 5.0);
        bool isAtTop = _scrollViewer.Offset.Y <= 5.0;

        // Update stickiness state
        bool isAtTarget = InvertAutoScrollDirection ? isAtTop : isAtBottom;
        bool scrolledAwayFromTarget = InvertAutoScrollDirection ? e.OffsetDelta.Y > 0 : e.OffsetDelta.Y < 0;
        bool scrolledTowardTarget = InvertAutoScrollDirection ? e.OffsetDelta.Y < 0 : e.OffsetDelta.Y > 0;

        if (scrolledAwayFromTarget)
        {
            if (AutoScroll)
            {
                // Disable autoscroll if user manually scrolls away
                SetCurrentValue(AutoScrollProperty, false);
            }
            _isStuckToTarget = false;
        }
        else if (isAtTarget)
        {
            _isStuckToTarget = true;

            // Optional: Re-enable AutoScroll if user scrolled to target
            if (!AutoScroll && scrolledTowardTarget)
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

    public static readonly StyledProperty<ICommand?> CopySelectedEntryCommandProperty =
        AvaloniaProperty.Register<LogListControl, ICommand?>(nameof(CopySelectedEntryCommand));

    public ICommand? CopySelectedEntryCommand
    {
        get => GetValue(CopySelectedEntryCommandProperty);
        set => SetValue(CopySelectedEntryCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> CopySelectedMessageCommandProperty =
        AvaloniaProperty.Register<LogListControl, ICommand?>(nameof(CopySelectedMessageCommand));

    public ICommand? CopySelectedMessageCommand
    {
        get => GetValue(CopySelectedMessageCommandProperty);
        set => SetValue(CopySelectedMessageCommandProperty, value);
    }

    public static readonly StyledProperty<bool> InvertAutoScrollDirectionProperty =
        AvaloniaProperty.Register<LogListControl, bool>(nameof(InvertAutoScrollDirection));

    public bool InvertAutoScrollDirection
    {
        get => GetValue(InvertAutoScrollDirectionProperty);
        set => SetValue(InvertAutoScrollDirectionProperty, value);
    }
}
