using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    private ObservableCollection<EditableMemorySegment> _segments = new();
    private EditableMemorySegment? _selectedSegment;
    private bool _isValid = true;
    private string _validationMessage = string.Empty;
    private bool _hasChanges;
    private readonly Subject<Unit> _segmentsChanged = new();

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
        _originalProfile = profile.ClonePreserveId() ?? throw new ArgumentNullException(nameof(profile));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadProfile(profile.ClonePreserveId());
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
    /// Gets the collection of editable memory segments.
    /// </summary>
    public ObservableCollection<EditableMemorySegment> Segments
    {
        get => _segments;
        private set => this.RaiseAndSetIfChanged(ref _segments, value);
    }

    /// <summary>
    /// Gets or sets the selected segment for editing.
    /// </summary>
    public EditableMemorySegment? SelectedSegment
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
            long totalBytes = Segments.Where(s => s.IsSelected).Sum(s => s.Segment.Size);
            return FormatSize(totalBytes);
        }
    }

    /// <summary>
    /// Gets the available memory types for the combo box.
    /// </summary>
    public IReadOnlyList<MemorySegmentType> MemoryTypes { get; } = Enum.GetValues<MemorySegmentType>();

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

            MemoryMappingProfile updatedProfile = _originalProfile.ClonePreserveId();
            updatedProfile.Name = ProfileName.Trim();
            updatedProfile.Description = Description.Trim();
            updatedProfile.Segments = Segments.Select(es => es.Segment).ToList();
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
        _logger.LogDebug("LoadProfile: Starting to load profile '{ProfileName}' (ID: {ProfileId})", profile.Name, profile.Id);

        ProfileName = profile.Name;
        Description = profile.Description;

        // Create observable collection from profile segments, wrapping each in EditableMemorySegment
        Segments.Clear();
        foreach (MemorySegment segment in profile.Segments)
        {
            _logger.LogDebug("LoadProfile: Adding segment '{SegmentName}' - Start: {Start}, Size: {Size}, Type: {Type}",
                segment.Name, segment.StartAddress, segment.Size, segment.Type);
            Segments.Add(new EditableMemorySegment(segment));
        }

        _logger.LogInformation("LoadProfile: Loaded profile with {SegmentCount} segments, CanModify: {CanModify}",
            Segments.Count, CanModify);
    }

    /// <summary>
    /// Initializes the dialog commands.
    /// </summary>
    private void InitializeCommands()
    {
        // Save requires valid input AND at least one segment
        IObservable<bool> canSave = this.WhenAnyValue(
            x => x.IsValid,
            x => x.HasChanges,
            x => x.Segments.Count,
            (valid, changes, segmentCount) => valid && changes && CanModify && segmentCount > 0);

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



        DuplicateSegmentCommand = ReactiveCommand.Create(ExecuteDuplicateSegment, canModifySegment)
            .DisposeWith(_disposables);

        IObservable<bool> canMoveUp = Observable.Merge(
                this.WhenAnyValue(x => x.SelectedSegment).Select(_ => Unit.Default),
                _segmentsChanged)
            .Select(_ => SelectedSegment != null && CanModify && Segments.IndexOf(SelectedSegment) > 0);

        IObservable<bool> canMoveDown = Observable.Merge(
                this.WhenAnyValue(x => x.SelectedSegment).Select(_ => Unit.Default),
                _segmentsChanged)
            .Select(_ => SelectedSegment != null && CanModify && Segments.IndexOf(SelectedSegment) < Segments.Count - 1);

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
        IObservable<bool> changesObservable = Observable.Merge(
                this.WhenAnyValue(x => x.ProfileName, x => x.Description, x => x.Segments.Count).Select(_ => Unit.Default),
                _segmentsChanged)
            .Select(_ => DetectChanges(ProfileName, Description, Segments.Count));

        changesObservable
            .Subscribe(hasChanges => HasChanges = hasChanges)
            .DisposeWith(_disposables);

        // Update validation message
        this.WhenAnyValue(x => x.ProfileName, x => x.Segments.Count)
            .Subscribe(_ => UpdateValidationMessage())
            .DisposeWith(_disposables);

        // Update computed properties when segments change
        // Monitor Segments collection changes (handle replacement and content changes)
        this.WhenAnyValue(x => x.Segments)
            .Subscribe(segments =>
            {
                if (segments != null)
                {
                    // Subscribe to the collection changed event of the new list
                    segments.CollectionChanged += (_, _) =>
                    {
                        this.RaisePropertyChanged(nameof(SelectedSegmentCount));
                        this.RaisePropertyChanged(nameof(TotalSelectedSize));
                        SubscribeToSegmentChanges();
                        _segmentsChanged.OnNext(Unit.Default);
                    };

                    // Subscribe to property changes of all items in the new list
                    SubscribeToSegmentChanges();

                    // Trigger initial update for the new list
                    this.RaisePropertyChanged(nameof(SelectedSegmentCount));
                    this.RaisePropertyChanged(nameof(TotalSelectedSize));
                    _segmentsChanged.OnNext(Unit.Default);
                }
            })
            .DisposeWith(_disposables);



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

        // Allow profiles with 0 segments during editing (user might be about to add segments)
        // Validation will prevent saving if no segments exist

        // Check for segment overlaps only if segments exist
        if (segmentCount > 0)
        {
            List<string> validationErrors = ValidateSegments();
            return validationErrors.Count == 0;
        }

        return true; // Valid if name is present, even with no segments yet
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
            if (!SegmentsEqual(Segments[i].Segment, _originalProfile.Segments[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Subscribes to segment property changes.
    /// </summary>
    private void SubscribeToSegmentChanges()
    {
        foreach (EditableMemorySegment editableSegment in Segments)
        {
            // Subscribe to wrapper property changes instead of model properties
            // because MemorySegment auto-properties don't raise notifications
            editableSegment.PropertyChanged -= OnSegmentPropertyChanged;
            editableSegment.PropertyChanged += OnSegmentPropertyChanged;
        }
    }

    /// <summary>
    /// Handles property changes in memory segments.
    /// </summary>
    private void OnSegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemorySegment.IsSelected))
        {
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
        }

        // Notify that something changed in the segments to re-evaluate HasChanges and Commands
        _segmentsChanged.OnNext(Unit.Default);
    }

    /// <summary>
    /// Validates all segments for overlaps and consistency.
    /// </summary>
    private List<string> ValidateSegments()
    {
        var errors = new List<string>();

        try
        {
            _logger.LogDebug("ValidateSegments: Validating {SegmentCount} segments", Segments.Count);

            // Check individual segments
            for (int i = 0; i < Segments.Count; i++)
            {
                MemorySegment segment = Segments[i].Segment;
                if (!segment.IsValid())
                {
                    string error = $"Segment {i + 1} ('{segment.Name}') has invalid properties";
                    errors.Add(error);
                    _logger.LogWarning("ValidateSegments: {Error}", error);
                }
            }

            // Note: Overlap check removed from errors - firmware segments can legitimately overlap (nested segments)
            // Overlaps are shown as warnings in the UI but don't prevent saving

            // Check for duplicate names
            IEnumerable<string> duplicateNames = Segments
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string duplicateName in duplicateNames)
            {
                string error = $"Duplicate segment name: '{duplicateName}'";
                errors.Add(error);
                _logger.LogWarning("ValidateSegments: {Error}", error);
            }

            _logger.LogInformation("ValidateSegments: Validation complete - {ErrorCount} errors found", errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during segment validation");
            errors.Add($"Validation error: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Gets a list of overlapping segment warnings (does not block saving).
    /// </summary>
    private List<string> GetOverlapWarnings()
    {
        var warnings = new List<string>();

        try
        {
            // Check for overlapping segments
            for (int i = 0; i < Segments.Count; i++)
            {
                for (int j = i + 1; j < Segments.Count; j++)
                {
                    try
                    {
                        if (Segments[i].Segment.OverlapsWith(Segments[j].Segment))
                        {
                            string warning = $"Segments '{Segments[i].Name}' and '{Segments[j].Name}' have overlapping address ranges";
                            warnings.Add(warning);
                            _logger.LogDebug("GetOverlapWarnings: {Warning}", warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "GetOverlapWarnings: Overlap check failed between segments {I} and {J}", i, j);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overlap warnings");
        }

        return warnings;
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

        // Show overlap warnings (if any) but don't block saving
        List<string> warnings = GetOverlapWarnings();
        if (warnings.Count > 0)
        {
            ValidationMessage = $"⚠️ Warning: {warnings.Count} overlapping segment(s) detected (and {warnings.Count - 1} more...)";
            _logger.LogDebug("UpdateValidationMessage: Showing overlap warning for {Count} overlaps", warnings.Count);
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
            _logger.LogInformation("ExecuteSave: Saving changes to memory region profile '{ProfileName}' with {SegmentCount} segments",
                ProfileName, Segments.Count);
            _logger.LogDebug("ExecuteSave: IsValid={IsValid}, HasChanges={HasChanges}, CanModify={CanModify}",
                IsValid, HasChanges, CanModify);

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
                IsSelected = false, // Default: checkboxes disabled
                Description = "Custom memory segment"
            };

            var editableSegment = new EditableMemorySegment(newSegment);
            Segments.Add(editableSegment);
            SelectedSegment = editableSegment;

            _logger.LogInformation("ExecuteAddSegment: Added new segment '{SegmentName}' at index {Index}",
                newSegment.Name, Segments.Count - 1);
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
                int index = Segments.IndexOf(SelectedSegment);
                Segments.Remove(SelectedSegment);
                SelectedSegment = null;

                _logger.LogInformation("ExecuteRemoveSegment: Removed segment '{SegmentName}' from index {Index}",
                    segmentName, index);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing segment");
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
                    Size = SelectedSegment.Segment.Size, // Access underlying segment's Size (long)
                    Type = SelectedSegment.Type,
                    IsSelected = false, // Default: checkboxes disabled
                    Description = $"Copy of {SelectedSegment.Description}"
                };

                var editableDuplicated = new EditableMemorySegment(duplicated);
                int insertIndex = Segments.IndexOf(SelectedSegment) + 1;
                Segments.Insert(insertIndex, editableDuplicated);
                SelectedSegment = editableDuplicated;

                _logger.LogInformation("ExecuteDuplicateSegment: Duplicated segment '{Original}' to '{Copy}' at index {Index}",
                    SelectedSegment.Name, duplicated.Name, insertIndex);
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
                _logger.LogDebug("ExecuteMoveUp: Moving segment '{Name}' from index {FromIndex} to {ToIndex}",
                    SelectedSegment.Name, index, index - 1);

                if (index > 0)
                {
                    // Copy Strategy:
                    // 1. Capture current selection (object reference)
                    var selectedItem = SelectedSegment;

                    // 2. Create COPY of collection
                    var newSegments = new ObservableCollection<EditableMemorySegment>(Segments);

                    // 3. Move item in new collection
                    newSegments.Move(index, index - 1);

                    // 4. Swap collection (Forces DataGrid refresh)
                    Segments = newSegments;

                    // 5. Schedule selection restoration
                    //    We use Dispatcher.UIThread.Post to let the binding engine process the 
                    //    null selection caused by collection swap, THEN we set it back.
                    Dispatcher.UIThread.Post(() =>
                    {
                        SelectedSegment = selectedItem;
                        // Force notification mainly to be safe, though setter does it too
                        _segmentsChanged.OnNext(Unit.Default);
                    }, DispatcherPriority.Input);

                    _logger.LogInformation("Moved segment '{Name}' up successfully", selectedItem?.Name);
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
                _logger.LogDebug("ExecuteMoveDown: Moving segment '{Name}' from index {FromIndex} to {ToIndex}",
                    SelectedSegment.Name, index, index + 1);

                if (index < Segments.Count - 1)
                {
                    // Copy Strategy:
                    // 1. Capture current selection (object reference)
                    var selectedItem = SelectedSegment;

                    // 2. Create COPY of collection
                    var newSegments = new ObservableCollection<EditableMemorySegment>(Segments);

                    // 3. Move item in new collection
                    newSegments.Move(index, index + 1);

                    // 4. Swap collection (Forces DataGrid refresh)
                    Segments = newSegments;

                    // 5. Schedule selection restoration
                    Dispatcher.UIThread.Post(() =>
                   {
                       SelectedSegment = selectedItem;
                       _segmentsChanged.OnNext(Unit.Default);
                   }, DispatcherPriority.Input);

                    _logger.LogInformation("Moved segment '{Name}' down successfully", selectedItem?.Name);
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
            _segmentsChanged?.Dispose();
            _disposables?.Dispose();
            _logger.LogDebug("EditMemoryRegionProfileDialogViewModel disposed");
        }
    }

    #endregion
}
