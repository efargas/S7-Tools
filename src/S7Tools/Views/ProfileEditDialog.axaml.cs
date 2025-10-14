using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using S7Tools.Models;
using S7Tools.ViewModels;

namespace S7Tools.Views;

/// <summary>
/// Base dialog for editing profiles with dynamic content based on profile type.
/// </summary>
public partial class ProfileEditDialog : Window
{
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
    }

    /// <summary>
    /// Sets up the dialog with the specified request.
    /// </summary>
    /// <param name="request">The profile edit request.</param>
    public void SetupDialog(ProfileEditRequest request)
    {
        Title = string.IsNullOrEmpty(request.Title)
            ? $"Edit {request.ProfileType} Profile"
            : request.Title;
        DataContext = request.ProfileViewModel;

        // Set the content based on profile type
        var contentArea = this.FindControl<ContentPresenter>("ContentArea");
        if (contentArea != null)
        {
            switch (request.ProfileType)
            {
                case ProfileType.Serial:
                    // Will be implemented in Phase 2
                    contentArea.Content = CreateSerialProfileContent();
                    break;
                case ProfileType.Socat:
                    // Will be implemented in Phase 3
                    contentArea.Content = CreateSocatProfileContent();
                    break;
                case ProfileType.PowerSupply:
                    contentArea.Content = CreatePowerSupplyProfileContent();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.ProfileType), request.ProfileType, "Unknown profile type");
            }
        }
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
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

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
    /// Handles the save button click event.
    /// </summary>
    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModelBase profileViewModel)
        {
            // Validate the profile before saving
            if (IsProfileValid(profileViewModel))
            {
                try
                {
                    // Actually save the profile by calling the SaveCommand
                    if (profileViewModel is SerialPortProfileViewModel serialViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"Executing Serial SaveCommand, IsValid: {serialViewModel.IsValid}, HasChanges: {serialViewModel.HasChanges}, IsReadOnly: {serialViewModel.IsReadOnly}");
                        await serialViewModel.SaveCommand.Execute();
                        System.Diagnostics.Debug.WriteLine("Serial SaveCommand completed successfully");
                    }
                    else if (profileViewModel is SocatProfileViewModel socatViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"Executing Socat SaveCommand, IsValid: {socatViewModel.IsValid}, HasChanges: {socatViewModel.HasChanges}, IsReadOnly: {socatViewModel.IsReadOnly}");
                        await socatViewModel.SaveCommand.Execute();
                        System.Diagnostics.Debug.WriteLine("Socat SaveCommand completed successfully");
                    }

                    Result = ProfileEditResult.Success(profileViewModel);
                    Close();
                }
                catch (Exception ex)
                {
                    // Handle save errors - stay open and let user see the error
                    System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");
                    // The ViewModel should have updated its StatusMessage with the error
                }
            }
            // If validation fails, stay open and show validation errors
        }
    }

    /// <summary>
    /// Handles the cancel button click event.
    /// </summary>
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
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

        return true;
    }
}
