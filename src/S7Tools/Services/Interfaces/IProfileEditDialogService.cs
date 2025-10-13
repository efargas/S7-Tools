using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Models;
using S7Tools.ViewModels;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for displaying profile editing dialogs.
/// </summary>
public interface IProfileEditDialogService
{
    /// <summary>
    /// Gets the interaction for showing profile editing dialogs.
    /// </summary>
    Interaction<ProfileEditRequest, ProfileEditResult> ShowProfileEditDialog { get; }

    /// <summary>
    /// Shows a profile editing dialog for a serial port profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SerialPortProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> ShowSerialProfileEditAsync(string title, SerialPortProfileViewModel profileViewModel);

    /// <summary>
    /// Shows a profile editing dialog for a socat profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SocatProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> ShowSocatProfileEditAsync(string title, SocatProfileViewModel profileViewModel);
}
