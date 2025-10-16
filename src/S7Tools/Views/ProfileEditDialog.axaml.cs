using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using S7Tools.Models;
using S7Tools.ViewModels;

namespace S7Tools.Views;

/// <summary>
/// Base dialog for editing profiles with dynamic content based on profile type.
/// Enhanced with improved UI behaviors: draggable, proper window chrome, and responsive design.
/// </summary>
public partial class ProfileEditDialog : Window
{
    private bool _isSaving;

    /// <summary>
    /// Gets or sets the profile edit result.
    /// </summary>
    public ProfileEditResult Result { get; private set; } = ProfileEditResult.Cancelled();

    /// <summary>
    /// Initializes a new instance of the ProfileEditDialog class.
    /// </summary>
    public ProfileEditDialog()
    {
        InitializeComponent();
        SetupEventHandlers();
        SetupWindowBehaviors();
    }

    /// <summary>
    /// Sets up the dialog with the specified request.
    /// </summary>
    /// <param name="request">The profile edit request.</param>
    public void SetupDialog(ProfileEditRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog.SetupDialog called for {request.ProfileType}");

        Title = string.IsNullOrEmpty(request.Title)
            ? $"Edit {request.ProfileType} Profile"
            : request.Title;

        System.Diagnostics.Debug.WriteLine($"DEBUG: Dialog title set to: {Title}");

        DataContext = request.ProfileViewModel;
        System.Diagnostics.Debug.WriteLine($"DEBUG: DataContext set to: {request.ProfileViewModel?.GetType().Name ?? "null"}");

        // Set the content based on profile type
        ContentPresenter? contentArea = this.FindControl<ContentPresenter>("ContentArea");
        if (contentArea != null)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: ContentArea found, setting content for {request.ProfileType}");
            try
            {
                switch (request.ProfileType)
                {
                    case ProfileType.Serial:
                        // Will be implemented in Phase 2
                        contentArea.Content = CreateSerialProfileContent();
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Serial profile content created");
                        break;
                    case ProfileType.Socat:
                        // Will be implemented in Phase 3
                        contentArea.Content = CreateSocatProfileContent();
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Socat profile content created");
                        break;
                    case ProfileType.PowerSupply:
                        contentArea.Content = CreatePowerSupplyProfileContent();
                        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupply profile content created");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"ERROR: Unknown profile type: {request.ProfileType}");
                        throw new ArgumentOutOfRangeException(nameof(request.ProfileType), request.ProfileType, "Unknown profile type");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Exception creating profile content: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
                throw;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: ContentArea not found in ProfileEditDialog");
        }

        System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog.SetupDialog completed");
    }

    /// <summary>
    /// Creates the content for serial profile editing.
    /// </summary>
    /// <returns>A control for serial profile editing.</returns>
    private Control CreateSerialProfileContent()
    {
        return new SerialProfileEditContent();
    }

    /// <summary>
    /// Creates the content for socat profile editing.
    /// </summary>
    /// <returns>A control for socat profile editing.</returns>
    private Control CreateSocatProfileContent()
    {
        return new SocatProfileEditContent();
    }

    /// <summary>
    /// Creates the content for power supply profile editing.
    /// </summary>
    /// <returns>A control for power supply profile editing.</returns>
    private Control CreatePowerSupplyProfileContent()
    {
        return new PowerSupplyProfileEditContent();
    }

    /// <summary>
    /// Sets up event handlers for the dialog buttons.
    /// </summary>
    private void SetupEventHandlers()
    {
        Button? saveButton = this.FindControl<Button>("SaveButton");
        Button? cancelButton = this.FindControl<Button>("CancelButton");

        if (saveButton != null)
        {
            saveButton.Click += OnSaveButtonClick;
        }

        if (cancelButton != null)
        {
            cancelButton.Click += OnCancelButtonClick;
        }
    }

    /// <summary>
    /// Sets up enhanced window behaviors for better UX.
    /// </summary>
    private void SetupWindowBehaviors()
    {
        // Enable keyboard shortcuts
        KeyDown += OnKeyDown;

        // Ensure proper focus handling
        Opened += OnDialogOpened;

        // Handle window close events properly
        Closing += OnDialogClosing;
    }

    /// <summary>
    /// Handles keyboard shortcuts for the dialog.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (!_isSaving)
                {
                    OnCancelButtonClick(null, new RoutedEventArgs());
                }
                e.Handled = true;
                break;

            case Key.Enter:
                if (e.KeyModifiers == KeyModifiers.Control && !_isSaving)
                {
                    OnSaveButtonClick(null, new RoutedEventArgs());
                }
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles dialog opened event.
    /// </summary>
    private void OnDialogOpened(object? sender, EventArgs e)
    {
        // Focus the first input field if available
        Focus();
    }

    /// <summary>
    /// Handles dialog closing event.
    /// </summary>
    private void OnDialogClosing(object? sender, WindowClosingEventArgs e)
    {
        // Prevent closing during save operation
        if (_isSaving)
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    /// Handles the save button click event.
    /// Calls SaveAsync directly to avoid ReactiveCommand UI thread deadlock.
    /// </summary>
    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog.OnSaveButtonClick called");

        if (_isSaving)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: Save operation already in progress, ignoring duplicate click");
            return;
        }

        if (DataContext is ViewModelBase profileViewModel)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog DataContext type: {profileViewModel.GetType().Name}");

            // Validate the profile before saving
            if (IsProfileValid(profileViewModel))
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Profile validation passed");

                _isSaving = true;
                System.Diagnostics.Debug.WriteLine($"🔒 Set _isSaving = true, starting save operation");

                bool savedSuccessfully = false;
                try
                {
                    // Call SaveAsync directly (not via ReactiveCommand) to avoid UI thread deadlock
                    // ReactiveCommand.Execute() schedules back to the UI thread, causing deadlock when called from UI thread
                    if (profileViewModel is SerialPortProfileViewModel serialViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"Calling Serial SaveAsync directly, IsValid: {serialViewModel.IsValid}, HasChanges: {serialViewModel.HasChanges}, IsReadOnly: {serialViewModel.IsReadOnly}");
                        await serialViewModel.SaveAsync().ConfigureAwait(true);
                        System.Diagnostics.Debug.WriteLine("✅ Serial SaveAsync completed successfully");
                    }
                    else if (profileViewModel is SocatProfileViewModel socatViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"Calling Socat SaveAsync directly, IsValid: {socatViewModel.IsValid}, HasChanges: {socatViewModel.HasChanges}, IsReadOnly: {socatViewModel.IsReadOnly}");
                        await socatViewModel.SaveAsync().ConfigureAwait(true);
                        System.Diagnostics.Debug.WriteLine("✅ Socat SaveAsync completed successfully");
                    }
                    else if (profileViewModel is PowerSupplyProfileViewModel powerSupplyViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"Calling PowerSupply SaveAsync directly, IsValid: {powerSupplyViewModel.IsValid}, HasChanges: {powerSupplyViewModel.HasChanges}, IsReadOnly: {powerSupplyViewModel.IsReadOnly}");
                        await powerSupplyViewModel.SaveAsync().ConfigureAwait(true);
                        System.Diagnostics.Debug.WriteLine("✅ PowerSupply SaveAsync completed successfully");
                    }

                    savedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    // Handle save errors - stay open and show error to user
                    System.Diagnostics.Debug.WriteLine($"ERROR: ============================================");
                    System.Diagnostics.Debug.WriteLine($"ERROR: EXCEPTION IN ProfileEditDialog.OnSaveButtonClick");
                    System.Diagnostics.Debug.WriteLine($"ERROR: Exception Type: {ex.GetType().FullName}");
                    System.Diagnostics.Debug.WriteLine($"ERROR: Exception Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ERROR: Stack Trace: {ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"ERROR: ============================================");

                    // Update the ViewModel's StatusMessage which is displayed in the dialog UI
                    // This makes the error visible to the user so they understand why save failed
                    string errorMessage = $"Save failed: {ex.Message}";

                    // Update StatusMessage on each ViewModel type
                    if (profileViewModel is SerialPortProfileViewModel serialVm)
                    {
                        serialVm.StatusMessage = errorMessage;
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Updated SerialPortProfileViewModel StatusMessage: {errorMessage}");
                    }
                    else if (profileViewModel is SocatProfileViewModel socatVm)
                    {
                        socatVm.StatusMessage = errorMessage;
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Updated SocatProfileViewModel StatusMessage: {errorMessage}");
                    }
                    else if (profileViewModel is PowerSupplyProfileViewModel powerVm)
                    {
                        powerVm.StatusMessage = errorMessage;
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Updated PowerSupplyProfileViewModel StatusMessage: {errorMessage}");
                    }
                }
                finally
                {
                    _isSaving = false;
                    System.Diagnostics.Debug.WriteLine($"🔓 Set _isSaving = false, save operation completed");
                }

                if (savedSuccessfully)
                {
                    Result = ProfileEditResult.Success(profileViewModel);
                    System.Diagnostics.Debug.WriteLine($"✅ Profile save successful, closing dialog with Success result");
                    Close();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Profile validation failed");
            }
            // If validation fails, stay open and show validation errors
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog DataContext is not ViewModelBase, type: {DataContext?.GetType().Name ?? "null"}");
        }
    }

    /// <summary>
    /// Handles the cancel button click event.
    /// </summary>
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialog.OnCancelButtonClick called");
        Result = ProfileEditResult.Cancelled();
        Close();
    }

    /// <summary>
    /// Validates the profile ViewModel.
    /// </summary>
    /// <param name="profileViewModel">The profile ViewModel to validate.</param>
    /// <returns>True if the profile is valid, false otherwise.</returns>
    private static bool IsProfileValid(ViewModelBase profileViewModel)
    {
        // Check if the profile ViewModel has validation errors
        if (profileViewModel is SerialPortProfileViewModel serialViewModel)
        {
            return serialViewModel.ValidationErrors.Count == 0 && serialViewModel.IsValid;
        }

        if (profileViewModel is SocatProfileViewModel socatViewModel)
        {
            return socatViewModel.ValidationErrors.Count == 0 && socatViewModel.IsValid;
        }

        if (profileViewModel is PowerSupplyProfileViewModel powerSupplyViewModel)
        {
            return powerSupplyViewModel.IsValid;
        }

        return true;
    }
}
