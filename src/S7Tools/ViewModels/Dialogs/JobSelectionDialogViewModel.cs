using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;

namespace S7Tools.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the job selection dialog.
/// Allows users to select a job profile from the available profiles to create a new task.
/// </summary>
public class JobSelectionDialogViewModel : ViewModelBase
{
    private ObservableCollection<JobProfile> _availableJobs = new();
    private ObservableCollection<JobProfile> _filteredJobs = new();
    private JobProfile? _selectedJob;
    private string _searchText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSelectionDialogViewModel"/> class.
    /// </summary>
    /// <param name="availableJobs">The collection of available job profiles.</param>
    public JobSelectionDialogViewModel(ObservableCollection<JobProfile> availableJobs)
    {
        _availableJobs = availableJobs;
        _filteredJobs = new ObservableCollection<JobProfile>(availableJobs);

        // Setup commands
        var canConfirm = this.WhenAnyValue(x => x.SelectedJob)
            .Select(job => job != null);

        ConfirmCommand = ReactiveCommand.Create(() => { }, canConfirm);
        CancelCommand = ReactiveCommand.Create(() => { });

        // Setup search filtering
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => FilterJobs());
    }

    /// <summary>
    /// Gets the collection of available job profiles.
    /// </summary>
    public ObservableCollection<JobProfile> AvailableJobs
    {
        get => _availableJobs;
        private set => this.RaiseAndSetIfChanged(ref _availableJobs, value);
    }

    /// <summary>
    /// Gets the filtered collection of job profiles based on search text.
    /// </summary>
    public ObservableCollection<JobProfile> FilteredJobs
    {
        get => _filteredJobs;
        private set => this.RaiseAndSetIfChanged(ref _filteredJobs, value);
    }

    /// <summary>
    /// Gets or sets the currently selected job profile.
    /// </summary>
    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }

    /// <summary>
    /// Gets or sets the search text for filtering job profiles.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// Gets the command to confirm the job selection.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    /// <summary>
    /// Gets the command to cancel the dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Filters the job profiles based on the search text.
    /// </summary>
    private void FilterJobs()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredJobs = new ObservableCollection<JobProfile>(AvailableJobs);
        }
        else
        {
            var searchLower = SearchText.ToLowerInvariant();
            var filtered = AvailableJobs
                .Where(job => job.Name.ToLowerInvariant().Contains(searchLower) ||
                             (job.Description?.ToLowerInvariant().Contains(searchLower) ?? false))
                .ToList();
            FilteredJobs = new ObservableCollection<JobProfile>(filtered);
        }
    }
}
