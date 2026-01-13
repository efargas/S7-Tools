using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ReactiveUI;
using S7Tools.ViewModels.Tasks;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace S7Tools.Views.Tasks;

/// <summary>
/// View for displaying detailed information about a selected task.
/// </summary>
public partial class TaskDetailsView : ReactiveUserControl<TaskDetailsViewModel>
{
    private DataGrid? _mainLogGrid;
    private DataGrid? _socatLogGrid;
    private DataGrid? _protocolLogGrid;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDetailsView"/> class.
    /// </summary>
    public TaskDetailsView()
    {
        InitializeComponent();

        // Find DataGrids and setup scroll logic
        this.WhenActivated(disposables =>
        {
            _mainLogGrid = this.FindControl<DataGrid>("MainLogGrid");
            _socatLogGrid = this.FindControl<DataGrid>("SocatLogGrid");
            _protocolLogGrid = this.FindControl<DataGrid>("ProtocolLogGrid");

            SetupAutoScroll(_mainLogGrid, disposables);
            SetupAutoScroll(_socatLogGrid, disposables);
            SetupAutoScroll(_protocolLogGrid, disposables);
        });
    }

    private void SetupAutoScroll(DataGrid? dataGrid, CompositeDisposable disposables)
    {
        if (dataGrid == null)
            return;

        // Auto-Scroll on Collection Changed
        if (dataGrid.ItemsSource is INotifyCollectionChanged collection)
        {
            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => collection.CollectionChanged += h,
                h => collection.CollectionChanged -= h)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e =>
                {
                    if (ViewModel != null && ViewModel.AutoScroll && e.EventArgs.Action == NotifyCollectionChangedAction.Add)
                    {
                        var lastItem = dataGrid.ItemsSource?.Cast<object>().LastOrDefault();
                        if (lastItem != null)
                        {
                            dataGrid.ScrollIntoView(lastItem, null);
                        }
                    }
                })
                .DisposeWith(disposables);


            // React to AutoScroll Property Changes (User Toggle)
            ViewModel.WhenAnyValue(x => x.AutoScroll)
                .Where(autoScroll => autoScroll) // Only when enabled
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (ViewModel == null)
                        return;

                    // Use Dispatcher.Post to ensure UI is ready and layout has completed
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var lastItem = dataGrid.ItemsSource?.Cast<object>().LastOrDefault();
                        if (lastItem != null)
                        {
                            dataGrid.ScrollIntoView(lastItem, null);
                            ViewModel.IsStuckToBottom = true;
                        }
                    }, Avalonia.Threading.DispatcherPriority.Background);
                })
                .DisposeWith(disposables);
        }

        // Smart Scroll Detection (User Scroll)
        // We need to wait for the template to be applied to find the ScrollViewer?
        // Actually, AddHandler works on the DataGrid itself for bubbling events
        dataGrid.AddHandler(ScrollViewer.ScrollChangedEvent, (s, e) =>
        {
            if (ViewModel == null)
                return;

            var scrollArgs = e as ScrollChangedEventArgs;
            if (scrollArgs == null)
                return;

            // Get the ScrollViewer that triggered the event
            if (e.Source is not ScrollViewer scrollViewer)
                return;

            // Only consider the Vertical scroll of the DataGrid's internal ScrollViewer
            // Filter out horizontal scrolL or inner scrollviewers
            if (scrollArgs.OffsetDelta.Y == 0)
                return;

            // Simple logic: 
            // 1. If user scrolls UP (negative delta), disable AutoScroll
            // 2. If user is at BOTTOM, enable AutoScroll (if they weren't already)

            bool isAtBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - 20); // 20px tolerance

            if (scrollArgs.OffsetDelta.Y < 0)
            {
                // User scrolled up
                ViewModel.AutoScroll = false;
            }
            else if (isAtBottom && scrollArgs.OffsetDelta.Y > 0)
            {
                // User scrolled down to bottom
                ViewModel.AutoScroll = true;
            }

            ViewModel.IsStuckToBottom = isAtBottom;

        }, RoutingStrategies.Bubble);
    }
}
