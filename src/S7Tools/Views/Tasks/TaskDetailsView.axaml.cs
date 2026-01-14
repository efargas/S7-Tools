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
    private ScrollViewer? _mainLogScrollViewer;
    private ScrollViewer? _socatLogScrollViewer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDetailsView"/> class.
    /// </summary>
    public TaskDetailsView()
    {
        InitializeComponent();

        // Find ScrollViewers and setup scroll logic
        this.WhenActivated(disposables =>
        {
            _mainLogScrollViewer = this.FindControl<ScrollViewer>("MainLogScrollViewer");
            _socatLogScrollViewer = this.FindControl<ScrollViewer>("SocatLogScrollViewer");

            SetupAutoScroll(_mainLogScrollViewer, ViewModel?.MainLogEntries, disposables);
            SetupAutoScroll(_socatLogScrollViewer, ViewModel?.ProcessLogEntries, disposables);
        });
    }

    private void SetupAutoScroll(ScrollViewer? scrollViewer, INotifyCollectionChanged? collection, CompositeDisposable disposables)
    {
        if (scrollViewer == null || collection == null)
            return;

        // Auto-Scroll on Collection Changed
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
            h => collection.CollectionChanged += h,
            h => collection.CollectionChanged -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(e =>
            {
                if (ViewModel != null && ViewModel.AutoScroll &&
                    (e.EventArgs.Action == NotifyCollectionChangedAction.Add || e.EventArgs.Action == NotifyCollectionChangedAction.Reset))
                {
                    // Scroll to bottom using background priority to ensure layout is ready
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), Avalonia.Threading.DispatcherPriority.Background);
                }
            })
            .DisposeWith(disposables);


        // React to AutoScroll Property Changes (User Toggle)
        ViewModel!.WhenAnyValue(x => x.AutoScroll)
            .Where(autoScroll => autoScroll) // Only when enabled
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                if (ViewModel == null)
                    return;

                // Use Dispatcher.Post to ensure UI is ready and layout has completed
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer.ScrollToEnd();
                    ViewModel.IsStuckToBottom = true;
                }, Avalonia.Threading.DispatcherPriority.Background);
            })
            .DisposeWith(disposables);


        // Smart Scroll Detection (User Scroll)
        // ScrollViewer exposes ScrollChanged event directly
        scrollViewer.ScrollChanged += (s, e) =>
        {
            if (ViewModel == null)
                return;

            // Only consider vertical scroll
            if (e.OffsetDelta.Y == 0)
                return;

            // Simple logic: 
            // 1. If user scrolls UP (negative delta), disable AutoScroll
            // 2. If user is at BOTTOM, enable AutoScroll (if they weren't already)

            bool isAtBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - 20); // 20px tolerance

            if (e.OffsetDelta.Y < 0)
            {
                // User scrolled up
                ViewModel.AutoScroll = false;
            }
            else if (isAtBottom && e.OffsetDelta.Y > 0)
            {
                // User scrolled down to bottom
                ViewModel.AutoScroll = true;
            }

            ViewModel.IsStuckToBottom = isAtBottom;
        };
    }
}
