using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Resources.Strings;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Duplicate Memory Region Profile dialog, providing name conflict resolution and profile duplication.
/// </summary>
/// <remarks>
/// This dialog enables users to create copies of existing memory region profiles with new names.
/// It provides automatic name suggestion, conflict detection, and validation to ensure unique profile names.
/// </remarks>
public class DuplicateMemoryRegionProfileDialogViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<DuplicateMemoryRegionProfileDialogViewModel> _logger;
    private readonly MemoryMappingProfile _sourceProfile;
    private readonly Func<string, bool> _nameExistsChecker;

    private string _newProfileName = string.Empty;
    private string _newDescription = string.Empty;
    private bool _copySegmentSelections = true;
    private bool _isValid = true;
    private string _validationMessage = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the DuplicateMemoryRegionProfileDialogViewModel class.
    /// </summary>
    /// <param name="sourceProfile">The profile to duplicate.</param>
    /// <param name="nameExistsChecker">Function to check if a profile name already exists.</param>
    /// <param name="logger">The logger instance.</param>
    public DuplicateMemoryRegionProfileDialogViewModel(
        MemoryMappingProfile sourceProfile,
        Func<string, bool> nameExistsChecker,
        ILogger<DuplicateMemoryRegionProfileDialogViewModel> logger)
    {
        _sourceProfile = sourceProfile ?? throw new ArgumentNullException(nameof(sourceProfile));
        _nameExistsChecker = nameExistsChecker ?? throw new ArgumentNullException(nameof(nameExistsChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeDefaults();
        InitializeCommands();
        InitializeValidation();

        _logger.LogDebug("DuplicateMemoryRegionProfileDialogViewModel initialized for source profile: {SourceProfileName}",
            sourceProfile.Name);
    }

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    public DuplicateMemoryRegionProfileDialogViewModel()
        : this(
            MemoryMappingProfile.CreateUserProfile("Example Profile", "Example description"),
            _ => false,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DuplicateMemoryRegionProfileDialogViewModel>.Instance)
    {
        NewProfileName = "Example Profile Copy";
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the name of the source profile being duplicated.
    /// </summary>
    public string SourceProfileName => _sourceProfile.Name;

    /// <summary>
    /// Gets the description of the source profile being duplicated.
    /// </summary>
    public string SourceDescription => _sourceProfile.Description;

    /// <summary>
    /// Gets the number of segments in the source profile.
    /// </summary>
    public int SourceSegmentCount => _sourceProfile.Segments.Count;

    /// <summary>
    /// Gets the number of selected segments in the source profile.
    /// </summary>
    public int SourceSelectedSegmentCount => _sourceProfile.SelectedSegments.Count();

    /// <summary>
    /// Gets or sets the new profile name.
    /// </summary>
    public string NewProfileName
    {
        get => _newProfileName;
        set => this.RaiseAndSetIfChanged(ref _newProfileName, value);
    }

    /// <summary>
    /// Gets or sets the new profile description.
    /// </summary>
    public string NewDescription
    {
        get => _newDescription;
        set => this.RaiseAndSetIfChanged(ref _newDescription, value);
    }

    /// <summary>
    /// Gets or sets whether to copy segment selections from the source profile.
    /// </summary>
    public bool CopySegmentSelections
    {
        get => _copySegmentSelections;
        set => this.RaiseAndSetIfChanged(ref _copySegmentSelections, value);
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
    /// Gets the suggested name for the duplicate profile.
    /// </summary>
    public string SuggestedName { get; private set; } = string.Empty;

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to duplicate the profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to cancel duplication.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to use the suggested name.
    /// </summary>
    public ReactiveCommand<Unit, Unit> UseSuggestedNameCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to reset to original description.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetDescriptionCommand { get; private set; } = null!;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a duplicated MemoryMappingProfile from the source profile and current dialog settings.
    /// </summary>
    /// <returns>The duplicated profile, or null if creation failed.</returns>
    public MemoryMappingProfile? CreateDuplicateProfile()
    {
        try
        {
            if (!IsValid)
            {
                _logger.LogWarning("Cannot create duplicate profile: validation failed");
                return null;
            }

            MemoryMappingProfile duplicate = _sourceProfile.Duplicate(NewProfileName.Trim());
            duplicate.Description = NewDescription.Trim();

            // Handle segment selections based on user preference
            if (!CopySegmentSelections)
            {
                foreach (MemorySegment segment in duplicate.Segments)
                {
                    segment.IsSelected = false;
                }
            }

            _logger.LogInformation("Created duplicate profile '{NewName}' from source '{SourceName}' with {SegmentCount} segments",
                NewProfileName, SourceProfileName, duplicate.Segments.Count);

            return duplicate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating duplicate memory region profile");
            return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes default values for the dialog.
    /// </summary>
    private void InitializeDefaults()
    {
        // Generate suggested name
        SuggestedName = GenerateUniqueName(_sourceProfile.Name);
        NewProfileName = SuggestedName;

        // Use source description as default
        NewDescription = _sourceProfile.Description;

        _logger.LogDebug("Initialized defaults: suggested name = '{SuggestedName}'", SuggestedName);
    }

    /// <summary>
    /// Initializes the dialog commands.
    /// </summary>
    private void InitializeCommands()
    {
        IObservable<bool> canDuplicate = this.WhenAnyValue(x => x.IsValid);

        DuplicateCommand = ReactiveCommand.Create(ExecuteDuplicate, canDuplicate)
            .DisposeWith(_disposables);

        CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, false))
            .DisposeWith(_disposables);

        UseSuggestedNameCommand = ReactiveCommand.Create(ExecuteUseSuggestedName)
            .DisposeWith(_disposables);

        ResetDescriptionCommand = ReactiveCommand.Create(ExecuteResetDescription)
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes validation logic.
    /// </summary>
    private void InitializeValidation()
    {
        // Validation observable
        IObservable<bool> validationObservable = this.WhenAnyValue(
            x => x.NewProfileName,
            ValidateInput);

        validationObservable
            .Subscribe(isValid => IsValid = isValid)
            .DisposeWith(_disposables);

        // Update validation message when name changes
        this.WhenAnyValue(x => x.NewProfileName)
            .Subscribe(_ => UpdateValidationMessage())
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Validates the current input state.
    /// </summary>
    private bool ValidateInput(string newProfileName)
    {
        if (string.IsNullOrWhiteSpace(newProfileName))
        {
            return false;
        }

        string trimmedName = newProfileName.Trim();

        // Check if name is the same as source (case-insensitive)
        if (string.Equals(trimmedName, _sourceProfile.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check if name already exists
        try
        {
            if (_nameExistsChecker(trimmedName))
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if profile name exists: {ProfileName}", trimmedName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Updates the validation message based on current state.
    /// </summary>
    private void UpdateValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            ValidationMessage = "Profile name is required";
            return;
        }

        string trimmedName = NewProfileName.Trim();

        // Check if name is the same as source
        if (string.Equals(trimmedName, _sourceProfile.Name, StringComparison.OrdinalIgnoreCase))
        {
            ValidationMessage = "New profile name cannot be the same as the source profile";
            return;
        }

        // Check if name already exists
        try
        {
            if (_nameExistsChecker(trimmedName))
            {
                ValidationMessage = UIStrings.Validation_ProfileNameExists;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if profile name exists");
            ValidationMessage = UIStrings.Validation_ErrorValidatingProfileName;
            return;
        }

        ValidationMessage = string.Empty;
    }

    /// <summary>
    /// Generates a unique name based on the source profile name.
    /// </summary>
    private string GenerateUniqueName(string baseName)
    {
        string[] candidates =
        [
            $"{baseName} Copy",
            $"{baseName} (Copy)",
            $"Copy of {baseName}"
        ];

        // Try standard naming patterns first
        foreach (string? candidate in candidates)
        {
            try
            {
                if (!_nameExistsChecker(candidate))
                {
                    return candidate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking candidate name: {CandidateName}", candidate);
            }
        }

        // Fall back to numbered copies
        for (int i = 2; i <= 100; i++)
        {
            string numberedName = $"{baseName} Copy ({i})";
            try
            {
                if (!_nameExistsChecker(numberedName))
                {
                    return numberedName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking numbered name: {NumberedName}", numberedName);
            }
        }

        // Last resort - add timestamp
        string timestamp = DateTime.UtcNow.ToLocalTime().ToString(DateTimeFormats.FileTimestamp);
        return $"{baseName} Copy {timestamp}";
    }

    /// <summary>
    /// Executes the duplicate command.
    /// </summary>
    private void ExecuteDuplicate()
    {
        try
        {
            _logger.LogInformation("Duplicating memory region profile '{SourceName}' to '{NewName}'",
                SourceProfileName, NewProfileName);

            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during profile duplication");
            ValidationMessage = $"Error duplicating profile: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes the use suggested name command.
    /// </summary>
    private void ExecuteUseSuggestedName()
    {
        try
        {
            NewProfileName = SuggestedName;
            _logger.LogDebug("Used suggested name: {SuggestedName}", SuggestedName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using suggested name");
        }
    }

    /// <summary>
    /// Executes the reset description command.
    /// </summary>
    private void ExecuteResetDescription()
    {
        try
        {
            NewDescription = _sourceProfile.Description;
            _logger.LogDebug("Reset description to source profile description");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting description");
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
            _logger.LogDebug("DuplicateMemoryRegionProfileDialogViewModel disposed");
        }
    }

    #endregion
}
