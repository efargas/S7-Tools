using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Edit Memory Region Profile dialog, providing comprehensive segment editing and validation capabilities.
/// </summary>
/// <remarks>
/// This dialog enables users to modify existing memory region profiles including name, description,
/// and detailed segment configuration. It provides real-time validation, overlap detection,
/// and maintains referential integrity for profile modifications.
/// </remarks>
public class EditMemoryRegionProfileDialogViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<EditMemoryRegionProfileDialogViewModel> _logger;
    private readonly MemoryMappingProfile _originalProfile;

    private string _profileName = string.Empty;
    private string _description = string.Empty;
    private ObservableCollection<MemorySegment> _segments = new();
    private MemorySegment? _selectedSegment;
    private bool _isValid = true;
    private string _validationMessage = string.Empty;
    private bool _hasChanges;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the EditMemoryRegionProfileDialogViewModel class.
    /// </summary>
    /// <param name="profile">The profile to edit.</param>
    /// <param name="logger">The logger instance.</param>
    public EditMemoryRegionProfileDialogViewModel(
        MemoryMappingProfile profile,
        ILogger<EditMemoryRegionProfileDialogViewModel> logger)
    {
        _originalProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadProfile(profile);
        InitializeCommands();
        InitializeValidation();

        _logger.LogDebug("EditMemoryRegionProfileDialogViewModel initialized for profile: {ProfileName}", profile.Name);
    }

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    public EditMemoryRegionProfileDialogViewModel()
        : this(
            MemoryMappingProfile.CreateUserProfile("Example Profile", "Example description"),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EditMemoryRegionProfileDialogViewModel>.Instance)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public string ProfileName
    {
        get => _profileName;
        set => this.RaiseAndSetIfChanged(ref _profileName, value);
    }

    /// <summary>
    /// Gets or sets the profile description.
    /// </summary>
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    /// <summary>
    /// Gets the collection of memory segments.
    /// </summary>
    public ObservableCollection<MemorySegment> Segments
    {
        get => _segments;
        private set => this.RaiseAndSetIfChanged(ref _segments, value);
    }

    /// <summary>
    /// Gets or sets the selected segment for editing.
    /// </summary>
    public MemorySegment? SelectedSegment
    {
        get => _selectedSegment;
        set => this.RaiseAndSetIfChanged(ref _selectedSegment, value);
    }

    /// <summary>
    /// Gets whether the dialog input is valid.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    /// <summary>
    /// Gets the current validation message.
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => this.RaiseAndSetIfChanged(ref _validationMessage, value);
    }

    /// <summary>
    /// Gets whether there are unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
    }

    /// <summary>
    /// Gets whether the profile can be modified.
    /// </summary>
    public bool CanModify => _originalProfile.CanModify();

    /// <summary>
    /// Gets the number of selected segments.
    /// </summary>
    public int SelectedSegmentCount => Segments.Count(s => s.IsSelected);

    /// <summary>
    /// Gets the total size of selected segments.
    /// </summary>
    public string TotalSelectedSize
    {
        get
        {
            long totalBytes = Segments.Where(s => s.IsSelected).Sum(s => s.Size);
            return FormatSize(totalBytes);
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to save profile changes.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to cancel editing.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to add a new segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to remove a segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to edit segment details.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to duplicate a segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DuplicateSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to move a segment up.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveUpCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to move a segment down.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDownCommand { get; private set; } = null!;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates an updated MemoryMappingProfile from the current dialog settings.
    /// </summary>
    /// <returns>The updated profile, or null if creation failed.</returns>
    public MemoryMappingProfile? CreateUpdatedProfile()
    {
        try
        {
            if (!IsValid)
            {
                _logger.LogWarning("Cannot create updated profile: validation failed");
                return null;
            }

            MemoryMappingProfile updatedProfile = _originalProfile.Clone();
            updatedProfile.Name = ProfileName.Trim();
            updatedProfile.Description = Description.Trim();
            updatedProfile.Segments = Segments.ToList();
            updatedProfile.ModifiedAt = DateTime.UtcNow;

            _logger.LogInformation("Created updated profile '{ProfileName}' with {SegmentCount} segments",
                ProfileName, Segments.Count);

            return updatedProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating updated memory region profile");
            return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads the profile data into the dialog.
    /// </summary>
    private void LoadProfile(MemoryMappingProfile profile)
    {
        ProfileName = profile.Name;
        Description = profile.Description;

        // Create observable collection from profile segments
        Segments.Clear();
        foreach (MemorySegment segment in profile.Segments)
        {
            Segments.Add(segment);
        }

        _logger.LogDebug("Loaded profile with {SegmentCount} segments", Segments.Count);
    }

    /// <summary>
    /// Initializes the dialog commands.
    /// </summary>
    private void InitializeCommands()
    {
        IObservable<bool> canSave = this.WhenAnyValue(
            x => x.IsValid,
            x => x.HasChanges,
            (valid, changes) => valid && changes && CanModify);

        SaveCommand = ReactiveCommand.Create(ExecuteSave, canSave)
            .DisposeWith(_disposables);

        CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, false))
            .DisposeWith(_disposables);

        IObservable<bool> canModifySegment = this.WhenAnyValue(x => x.SelectedSegment)
            .Select(selected => selected != null && CanModify);

        IObservable<bool> canAddSegment = Observable.Return(CanModify);

        AddSegmentCommand = ReactiveCommand.Create(ExecuteAddSegment, canAddSegment)
            .DisposeWith(_disposables);

        RemoveSegmentCommand = ReactiveCommand.Create(ExecuteRemoveSegment, canModifySegment)
            .DisposeWith(_disposables);

        EditSegmentCommand = ReactiveCommand.Create(ExecuteEditSegment, canModifySegment)
            .DisposeWith(_disposables);

        DuplicateSegmentCommand = ReactiveCommand.Create(ExecuteDuplicateSegment, canModifySegment)
            .DisposeWith(_disposables);

        IObservable<bool> canMoveUp = this.WhenAnyValue(
            x => x.SelectedSegment,
            selected => selected != null && CanModify && Segments.IndexOf(selected) > 0);

        IObservable<bool> canMoveDown = this.WhenAnyValue(
            x => x.SelectedSegment,
            selected => selected != null && CanModify && Segments.IndexOf(selected) < Segments.Count - 1);

        MoveUpCommand = ReactiveCommand.Create(ExecuteMoveUp, canMoveUp)
            .DisposeWith(_disposables);

        MoveDownCommand = ReactiveCommand.Create(ExecuteMoveDown, canMoveDown)
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes validation logic.
    /// </summary>
    private void InitializeValidation()
    {
        // Validation observable
        IObservable<bool> validationObservable = this.WhenAnyValue(
            x => x.ProfileName,
            x => x.Segments.Count,
            ValidateInput);

        validationObservable
            .Subscribe(isValid => IsValid = isValid)
            .DisposeWith(_disposables);

        // Changes detection
        IObservable<bool> changesObservable = this.WhenAnyValue(
            x => x.ProfileName,
            x => x.Description,
            x => x.Segments.Count,
            DetectChanges);

        changesObservable
            .Subscribe(hasChanges => HasChanges = hasChanges)
            .DisposeWith(_disposables);

        // Update validation message
        this.WhenAnyValue(x => x.ProfileName, x => x.Segments.Count)
            .Subscribe(_ => UpdateValidationMessage())
            .DisposeWith(_disposables);

        // Update computed properties when segments change
        Segments.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
        };

        // React to segment selection changes
        this.WhenAnyValue(x => x.SelectedSegment)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectedSegmentCount));
                this.RaisePropertyChanged(nameof(TotalSelectedSize));
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Validates the current input state.
    /// </summary>
    private bool ValidateInput(string profileName, int segmentCount)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        if (segmentCount == 0)
        {
            return false;
        }

        // Check for segment overlaps
        List<string> validationErrors = ValidateSegments();
        return validationErrors.Count == 0;
    }

    /// <summary>
    /// Detects if changes have been made from the original profile.
    /// </summary>
    private bool DetectChanges(string profileName, string description, int segmentCount)
    {
        if (profileName.Trim() != _originalProfile.Name.Trim())
        {
            return true;
        }

        if (description.Trim() != _originalProfile.Description.Trim())
        {
            return true;
        }

        if (segmentCount != _originalProfile.Segments.Count)
        {
            return true;
        }

        // Check if segments have changed
        for (int i = 0; i < Math.Min(Segments.Count, _originalProfile.Segments.Count); i++)
        {
            if (!SegmentsEqual(Segments[i], _originalProfile.Segments[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates all segments for overlaps and consistency.
    /// </summary>
    private List<string> ValidateSegments()
    {
        var errors = new List<string>();

        try
        {
            // Check individual segments
            for (int i = 0; i < Segments.Count; i++)
            {
                MemorySegment segment = Segments[i];
                if (!segment.IsValid())
                {
                    errors.Add($"Segment {i + 1} ('{segment.Name}') has invalid properties");
                }
            }

            // Check for overlapping segments
            for (int i = 0; i < Segments.Count; i++)
            {
                for (int j = i + 1; j < Segments.Count; j++)
                {
                    try
                    {
                        if (Segments[i].OverlapsWith(Segments[j]))
                        {
                            errors.Add($"Segments '{Segments[i].Name}' and '{Segments[j].Name}' have overlapping address ranges");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to validate overlap between '{Segments[i].Name}' and '{Segments[j].Name}': {ex.Message}");
                    }
                }
            }

            // Check for duplicate names
            IEnumerable<string> duplicateNames = Segments
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string duplicateName in duplicateNames)
            {
                errors.Add($"Duplicate segment name: '{duplicateName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during segment validation");
            errors.Add($"Validation error: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Updates the validation message based on current state.
    /// </summary>
    private void UpdateValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ValidationMessage = "Profile name is required";
            return;
        }

        if (Segments.Count == 0)
        {
            ValidationMessage = "At least one segment is required";
            return;
        }

        List<string> errors = ValidateSegments();
        if (errors.Count > 0)
        {
            ValidationMessage = string.Join("; ", errors.Take(2));
            if (errors.Count > 2)
            {
                ValidationMessage += $" (and {errors.Count - 2} more...)";
            }
            return;
        }

        ValidationMessage = string.Empty;
    }

    /// <summary>
    /// Compares two segments for equality.
    /// </summary>
    private static bool SegmentsEqual(MemorySegment segment1, MemorySegment segment2)
    {
        return segment1.Name.Equals(segment2.Name, StringComparison.OrdinalIgnoreCase) &&
               segment1.StartAddress.Equals(segment2.StartAddress, StringComparison.OrdinalIgnoreCase) &&
               segment1.Size == segment2.Size &&
               segment1.Type == segment2.Type &&
               segment1.IsSelected == segment2.IsSelected &&
               segment1.Description.Equals(segment2.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// Formats a size in bytes to human readable format.
    /// </summary>
    private static string FormatSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F1} GB",
            >= MB => $"{bytes / (double)MB:F1} MB",
            >= KB => $"{bytes / (double)KB:F1} KB",
            _ => $"{bytes} bytes"
        };
    }

    /// <summary>
    /// Executes the save command.
    /// </summary>
    private void ExecuteSave()
    {
        try
        {
            _logger.LogInformation("Saving changes to memory region profile '{ProfileName}'", ProfileName);

            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during profile save");
            ValidationMessage = $"Error saving profile: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes the add segment command.
    /// </summary>
    private void ExecuteAddSegment()
    {
        try
        {
            var newSegment = new MemorySegment
            {
                Name = $"Segment {Segments.Count + 1}",
                StartAddress = "0x00000000",
                Size = 1024,
                Type = MemorySegmentType.Flash,
                IsSelected = false,
                Description = "Custom memory segment"
            };

            Segments.Add(newSegment);
            SelectedSegment = newSegment;

            _logger.LogDebug("Added new segment: {SegmentName}", newSegment.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding segment");
        }
    }

    /// <summary>
    /// Executes the remove segment command.
    /// </summary>
    private void ExecuteRemoveSegment()
    {
        try
        {
            if (SelectedSegment != null)
            {
                string segmentName = SelectedSegment.Name;
                Segments.Remove(SelectedSegment);
                SelectedSegment = null;

                _logger.LogDebug("Removed segment: {SegmentName}", segmentName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing segment");
        }
    }

    /// <summary>
    /// Executes the edit segment command.
    /// </summary>
    private void ExecuteEditSegment()
    {
        try
        {
            if (SelectedSegment != null)
            {
                // TODO: Open segment edit dialog when available
                _logger.LogDebug("Edit segment requested for: {SegmentName}", SelectedSegment.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing segment");
        }
    }

    /// <summary>
    /// Executes the duplicate segment command.
    /// </summary>
    private void ExecuteDuplicateSegment()
    {
        try
        {
            if (SelectedSegment != null)
            {
                var duplicated = new MemorySegment
                {
                    Name = $"{SelectedSegment.Name} Copy",
                    StartAddress = SelectedSegment.StartAddress,
                    Size = SelectedSegment.Size,
                    Type = SelectedSegment.Type,
                    IsSelected = false,
                    Description = $"Copy of {SelectedSegment.Description}"
                };

                int insertIndex = Segments.IndexOf(SelectedSegment) + 1;
                Segments.Insert(insertIndex, duplicated);
                SelectedSegment = duplicated;

                _logger.LogDebug("Duplicated segment: {SegmentName}", SelectedSegment.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating segment");
        }
    }

    /// <summary>
    /// Executes the move up command.
    /// </summary>
    private void ExecuteMoveUp()
    {
        try
        {
            if (SelectedSegment != null)
            {
                int index = Segments.IndexOf(SelectedSegment);
                if (index > 0)
                {
                    Segments.Move(index, index - 1);
                    _logger.LogDebug("Moved segment up: {SegmentName}", SelectedSegment.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving segment up");
        }
    }

    /// <summary>
    /// Executes the move down command.
    /// </summary>
    private void ExecuteMoveDown()
    {
        try
        {
            if (SelectedSegment != null)
            {
                int index = Segments.IndexOf(SelectedSegment);
                if (index < Segments.Count - 1)
                {
                    Segments.Move(index, index + 1);
                    _logger.LogDebug("Moved segment down: {SegmentName}", SelectedSegment.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving segment down");
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables?.Dispose();
            _logger.LogDebug("EditMemoryRegionProfileDialogViewModel disposed");
        }
    }

    #endregion
}
