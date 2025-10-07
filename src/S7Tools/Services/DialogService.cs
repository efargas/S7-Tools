using ReactiveUI;
using S7Tools.Models;
using S7Tools.Services.Interfaces;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    public DialogService()
    {
        ShowConfirmation = new Interaction<ConfirmationRequest, bool>();
        ShowError = new Interaction<ConfirmationRequest, Unit>();
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
}