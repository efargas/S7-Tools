using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;
using S7Tools.ViewModels;

namespace S7Tools.Views;

public partial class JobsMainContentView : UserControl, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;
    private double _lastPanelWidth = 400; // Store the last panel width for restoration

    public JobsMainContentView()
    {
        InitializeComponent();
        SetupBindings();
        SetupLayoutEvents();
    }

    private void SetupBindings()
    {
        // Wire up the job selection binding when DataContext is set
        this.WhenAnyValue(x => x.DataContext)
            .Select(dataContext => dataContext as JobsMainContentViewModel)
            .Where(viewModel => viewModel != null)
            .Subscribe(viewModel =>
            {
                // Clear previous subscriptions
                _disposables.Clear();

                // Set up the JobInfoDisplayView DataContext from the parent ViewModel
                if (viewModel!.JobInfoDisplayViewModel != null)
                {
                    JobInfoDisplay.DataContext = viewModel.JobInfoDisplayViewModel;
                }

                // Bind the selected job from the main ViewModel to the JobInfoDisplay
                viewModel.WhenAnyValue(x => x.SelectedProfile)
                    .Subscribe(selectedJob =>
                    {
                        if (viewModel.JobInfoDisplayViewModel != null)
                        {
                            viewModel.JobInfoDisplayViewModel.SelectedJob = selectedJob;
                        }
                    })
                    .DisposeWith(_disposables);
            })
            .DisposeWith(_disposables);
    }

    private void SetupLayoutEvents()
    {
        // Subscribe to GridSplitter events for real-time layout feedback
        // Note: Avalonia GridSplitter doesn't expose resize events directly
        // The layout updates happen automatically during resize operations
        // Panel width constraints are enforced by the MinWidth/MaxWidth in XAML
    }

    private void OnJobInfoToggleClick(object? sender, RoutedEventArgs e)
    {
        // Show the job info panel, splitter, and hide the activity bar
        ActivityBar.IsVisible = false;
        JobInfoPanel.IsVisible = true;
        JobInfoSplitter.IsVisible = true;

        // Update column widths for expanded state
        MainGrid.ColumnDefinitions[1].Width = new GridLength(4, GridUnitType.Pixel); // Show splitter (4px)

        // Restore the last panel width (with constraints enforcement)
        double targetWidth = Math.Max(300, Math.Min(600, _lastPanelWidth));
        MainGrid.ColumnDefinitions[2].Width = new GridLength(targetWidth, GridUnitType.Pixel);
    }
    private void OnCloseJobInfoPanelClick(object? sender, RoutedEventArgs e)
    {
        // Store the current panel width before closing (for restoration)
        GridLength currentWidth = MainGrid.ColumnDefinitions[2].Width;
        if (currentWidth.IsAbsolute)
        {
            _lastPanelWidth = currentWidth.Value;
        }

        // Hide the job info panel, splitter, and show the activity bar
        JobInfoPanel.IsVisible = false;
        JobInfoSplitter.IsVisible = false;
        ActivityBar.IsVisible = true;

        // Update column widths for collapsed state
        MainGrid.ColumnDefinitions[1].Width = new GridLength(0); // Hide splitter
        MainGrid.ColumnDefinitions[2].Width = new GridLength(48, GridUnitType.Pixel); // Show activity bar
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposables?.Dispose();
            _disposed = true;
        }
    }
}
