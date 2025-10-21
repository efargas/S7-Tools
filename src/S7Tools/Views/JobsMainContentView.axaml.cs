using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using S7Tools.ViewModels;

namespace S7Tools.Views;

public partial class JobsMainContentView : UserControl, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public JobsMainContentView()
    {
        InitializeComponent();
        SetupBindings();
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
