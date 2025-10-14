using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.Views;

namespace S7Tools.Services;

/// <summary>
/// Service implementation for displaying profile editing dialogs.
/// </summary>
public class ProfileEditDialogService : IProfileEditDialogService
{
    private static bool _handlerRegistered;
    private static readonly object _lockObject = new();

    /// <summary>
    /// Static interaction shared across all service instances.
    /// </summary>
    private static readonly Interaction<ProfileEditRequest, ProfileEditResult> _staticInteraction = new();

    /// <summary>
    /// Gets the interaction for showing profile editing dialogs.
    /// </summary>
    public Interaction<ProfileEditRequest, ProfileEditResult> ShowProfileEditDialog => _staticInteraction;

    /// <summary>
    /// Initializes a new instance of the ProfileEditDialogService class.
    /// </summary>
    public ProfileEditDialogService()
    {
        RegisterInteractionHandler();
    }

    /// <summary>
    /// Registers the interaction handler for profile edit dialogs (thread-safe, one-time only).
    /// </summary>
    private void RegisterInteractionHandler()
    {
        lock (_lockObject)
        {
            if (_handlerRegistered)
            {
                return;
            }

            _staticInteraction.RegisterHandler(async interaction =>
            {
                try
                {
                    // Create and setup profile edit dialog
                    var dialog = new Views.ProfileEditDialog();
                    dialog.SetupDialog(interaction.Input);

                    // Get the main window as parent
                    var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;

                    if (mainWindow != null)
                    {
                        await dialog.ShowDialog(mainWindow);
                        interaction.SetOutput(dialog.Result);
                    }
                    else
                    {
                        interaction.SetOutput(ProfileEditResult.Cancelled());
                    }
                }
                catch (Exception)
                {
                    interaction.SetOutput(ProfileEditResult.Cancelled());
                }
            });

            _handlerRegistered = true;
        }
    }

    /// <summary>
    /// Shows a profile editing dialog for a serial port profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SerialPortProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    public async Task<ProfileEditResult> ShowSerialProfileEditAsync(string title, SerialPortProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new ProfileEditRequest(title, profileViewModel, ProfileType.Serial);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    /// <summary>
    /// Shows a profile editing dialog for a socat profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SocatProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    public async Task<ProfileEditResult> ShowSocatProfileEditAsync(string title, SocatProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new ProfileEditRequest(title, profileViewModel, ProfileType.Socat);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> ShowPowerSupplyProfileEditAsync(string title, PowerSupplyProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new ProfileEditRequest(title, profileViewModel, ProfileType.PowerSupply);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }
}
