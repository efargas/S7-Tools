using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for displaying dialogs using the interaction pattern.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc/>
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; }

    /// <inheritdoc/>
    public Interaction<ConfirmationRequest, Unit> ShowError { get; }

    /// <inheritdoc/>
    public Interaction<InputRequest, InputResult> ShowInput { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    public DialogService()
    {
        ShowConfirmation = new Interaction<ConfirmationRequest, bool>();
        ShowError = new Interaction<ConfirmationRequest, Unit>();
        ShowInput = new Interaction<InputRequest, InputResult>();
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var request = new ConfirmationRequest(title, message);
        return await ShowConfirmation.Handle(request).FirstAsync();
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        var request = new ConfirmationRequest(title, message);
        await ShowError.Handle(request).FirstAsync();
    }

    /// <inheritdoc/>
    public async Task<InputResult> ShowInputAsync(string title, string message, string? defaultValue = null, string? placeholder = null)
    {
        var request = new InputRequest(title, message, defaultValue, placeholder);
        return await ShowInput.Handle(request).FirstAsync();
    }
}
