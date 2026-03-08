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
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Create Memory Region Profile dialog, providing template selection and custom segment configuration.
/// </summary>
/// <remarks>
/// This dialog enables users to create new memory region profiles either from predefined templates
/// or by defining custom memory segments. It follows ReactiveUI patterns for property validation,
/// command execution, and reactive state management.
/// </remarks>
public sealed class CreateMemoryRegionProfileDialogViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<CreateMemoryRegionProfileDialogViewModel> _logger;
    private readonly IDialogService? _dialogService;

    private string _profileName = string.Empty;
    private string _description = string.Empty;
    private MemoryRegionTemplate? _selectedTemplate;
    private bool _useTemplate = true;
    private ObservableCollection<MemorySegment> _customSegments = [];
    private MemorySegment? _selectedSegment;
    private bool _isValid;
    private string _validationMessage = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the CreateMemoryRegionProfileDialogViewModel class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CreateMemoryRegionProfileDialogViewModel(
        ILogger<CreateMemoryRegionProfileDialogViewModel> logger,
        IDialogService? dialogService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dialogService = dialogService;

        InitializeCommands();
        InitializeValidation();
        InitializeTemplates();

        _logger.LogDebug("CreateMemoryRegionProfileDialogViewModel initialized");
    }

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    public CreateMemoryRegionProfileDialogViewModel() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateMemoryRegionProfileDialogViewModel>.Instance)
    {
        // Design-time data
        ProfileName = "Example Profile";
        Description = "Example memory region profile for design preview";
        SelectedTemplate = AvailableTemplates.FirstOrDefault();
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
    /// Gets or sets the selected template.
    /// </summary>
    public MemoryRegionTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
    }

    /// <summary>
    /// Gets or sets whether to use template-based creation.
    /// </summary>
    public bool UseTemplate
    {
        get => _useTemplate;
        set => this.RaiseAndSetIfChanged(ref _useTemplate, value);
    }

    /// <summary>
    /// Gets whether to use custom segment configuration.
    /// </summary>
    public bool UseCustomSegments => !UseTemplate;

    /// <summary>
    /// Gets the collection of custom segments.
    /// </summary>
    public ObservableCollection<MemorySegment> CustomSegments
    {
        get => _customSegments;
        private set => this.RaiseAndSetIfChanged(ref _customSegments, value);
    }

    /// <summary>
    /// Gets or sets the selected custom segment for editing.
    /// </summary>
    public MemorySegment? SelectedSegment
    {
        get => _selectedSegment;
        set => this.RaiseAndSetIfChanged(ref _selectedSegment, value);
    }

    /// <summary>
    /// Gets whether the profile can be created with the current input.
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
    /// Gets the collection of available templates.
    /// </summary>
    public ObservableCollection<MemoryRegionTemplate> AvailableTemplates { get; } = [];

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command for creating the profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to cancel profile creation.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to add a new custom segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to remove a custom segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveSegmentCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to edit a custom segment.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditSegmentCommand { get; private set; } = null!;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a MemoryMappingProfile from the current dialog settings.
    /// </summary>
    /// <returns>The created profile, or null if creation failed.</returns>
    public MemoryMappingProfile? CreateProfile()
    {
        try
        {
            if (!IsValid)
            {
                _logger.LogWarning("Cannot create profile: validation failed");
                return null;
            }

            var profile = new MemoryMappingProfile
            {
                Name = ProfileName.Trim(),
                Description = Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsDefault = false,
                IsReadOnly = false,
                IsActive = false,
                Version = "1.0"
            };

            if (UseTemplate && SelectedTemplate != null)
            {
                // Create from template
                profile.Segments = SelectedTemplate.Segments.ToList();
                _logger.LogInformation("Created profile '{ProfileName}' from template '{TemplateName}'",
                    ProfileName, SelectedTemplate.Name);
            }
            else
            {
                // Create from custom segments
                profile.Segments = CustomSegments.ToList();
                _logger.LogInformation("Created profile '{ProfileName}' with {SegmentCount} custom segments",
                    ProfileName, CustomSegments.Count);
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating memory region profile");
            return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the dialog commands.
    /// </summary>
    private void InitializeCommands()
    {
        IObservable<bool> canCreate = this.WhenAnyValue(x => x.IsValid);

        CreateCommand = ReactiveCommand.Create(ExecuteCreate, canCreate)
            .DisposeWith(_disposables);

        CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, false))
            .DisposeWith(_disposables);

        IObservable<bool> canRemoveSegment = this.WhenAnyValue(
            x => x.SelectedSegment,
            x => x.UseTemplate,
            (selected, useTemplate) => selected != null && !useTemplate);

        AddSegmentCommand = ReactiveCommand.Create(ExecuteAddSegment)
            .DisposeWith(_disposables);

        RemoveSegmentCommand = ReactiveCommand.Create(ExecuteRemoveSegment, canRemoveSegment)
            .DisposeWith(_disposables);

        EditSegmentCommand = ReactiveCommand.Create(ExecuteEditSegment, canRemoveSegment)
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes validation logic.
    /// </summary>
    private void InitializeValidation()
    {
        IObservable<bool> validationObservable = this.WhenAnyValue(
            x => x.ProfileName,
            x => x.UseTemplate,
            x => x.SelectedTemplate,
            x => x.CustomSegments.Count,
            ValidateInput);

        validationObservable
            .Subscribe(isValid => IsValid = isValid)
            .DisposeWith(_disposables);

        // Update validation message
        this.WhenAnyValue(x => x.ProfileName, x => x.UseTemplate, x => x.SelectedTemplate, x => x.CustomSegments.Count)
            .Subscribe(_ => UpdateValidationMessage())
            .DisposeWith(_disposables);

        // React to template selection changes
        this.WhenAnyValue(x => x.UseTemplate)
            .Subscribe(useTemplate => this.RaisePropertyChanged(nameof(UseCustomSegments)))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes available templates.
    /// </summary>
    private void InitializeTemplates()
    {
        // Add S7-1200 firmware template
        var s7Template = new MemoryRegionTemplate
        {
            Name = "S7-1200 Firmware v4",
            Description = "Standard memory regions for Siemens S7-1200 PLC firmware version 4.x",
            Segments =
            {
                new()
                {
                    Name = ".text",
                    StartAddress = "0x08000000",
                    Size = 128 * 1024, // 128 KB
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Program code section - executable instructions"
                },
                new()
                {
                    Name = ".data",
                    StartAddress = MemoryConstants.DefaultUserMemoryStartHex,
                    Size = 16 * 1024, // 16 KB
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Initialized data section - variables with initial values"
                },
                new()
                {
                    Name = ".bss",
                    StartAddress = "0x20004000",
                    Size = 16 * 1024, // 16 KB
                    Type = MemorySegmentType.RAM,
                    IsSelected = true,
                    Description = "Uninitialized data section - zero-initialized variables (default selection)"
                }
            }
        };

        AvailableTemplates.Add(s7Template);
        SelectedTemplate = s7Template;

        _logger.LogDebug("Initialized {TemplateCount} memory region templates", AvailableTemplates.Count);
    }

    /// <summary>
    /// Validates the current input state.
    /// </summary>
    private bool ValidateInput(string profileName, bool useTemplate, MemoryRegionTemplate? selectedTemplate, int customSegmentCount)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        if (useTemplate)
        {
            return selectedTemplate != null;
        }

        return customSegmentCount > 0;
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

        if (UseTemplate && SelectedTemplate == null)
        {
            ValidationMessage = "Please select a template";
            return;
        }

        if (!UseTemplate && CustomSegments.Count == 0)
        {
            ValidationMessage = "At least one custom segment is required";
            return;
        }

        ValidationMessage = string.Empty;
    }

    /// <summary>
    /// Executes the create command.
    /// </summary>
    private void ExecuteCreate()
    {
        try
        {
            _logger.LogInformation("Creating memory region profile '{ProfileName}'", ProfileName);

            // Validation is already performed by IsValid property
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during profile creation");
            ValidationMessage = $"Error creating profile: {ex.Message}";
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
                Name = $"Segment {CustomSegments.Count + 1}",
                StartAddress = "0x00000000",
                Size = 1024,
                Type = MemorySegmentType.Flash,
                IsSelected = false,
                Description = "Custom memory segment"
            };

            CustomSegments.Add(newSegment);
            SelectedSegment = newSegment;

            _logger.LogDebug("Added new custom segment: {SegmentName}", newSegment.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding custom segment");
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
                CustomSegments.Remove(SelectedSegment);
                SelectedSegment = null;

                _logger.LogDebug("Removed custom segment: {SegmentName}", segmentName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing custom segment");
        }
    }

    /// <summary>
    /// Executes the edit segment command.
    /// </summary>
    private async void ExecuteEditSegment()
    {
        try
        {
            if (SelectedSegment != null && _dialogService != null)
            {
                _logger.LogDebug("Edit segment requested for: {SegmentName}", SelectedSegment.Name);

                // For now, use a simple input dialog to change the start address as a placeholder
                // for a full segment edit dialog
                var result = await _dialogService.ShowInputAsync(
                    $"Edit Segment: {SelectedSegment.Name}",
                    "Enter new start address (hex):",
                    SelectedSegment.StartAddress ?? "0x00000000",
                    "0x00000000");

                if (!result.IsCancelled && !string.IsNullOrWhiteSpace(result.Value))
                {
                    SelectedSegment.StartAddress = result.Value;

                    // Force a UI refresh of the segment
                    int index = CustomSegments.IndexOf(SelectedSegment);
                    if (index >= 0)
                    {
                        var temp = SelectedSegment;
                        CustomSegments.RemoveAt(index);
                        CustomSegments.Insert(index, temp);
                        SelectedSegment = temp;
                    }
                }
            }
            else if (_dialogService == null)
            {
                _logger.LogWarning("IDialogService is not available to edit segment");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing custom segment");
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables?.Dispose();
        _logger.LogDebug("CreateMemoryRegionProfileDialogViewModel disposed");
    }

    #endregion
}

/// <summary>
/// Represents a memory region template for profile creation.
/// </summary>
public class MemoryRegionTemplate
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template segments.
    /// </summary>
    public List<MemorySegment> Segments { get; set; } = [];

    /// <summary>
    /// Returns the template name for display.
    /// </summary>
    public override string ToString() => Name;
}
