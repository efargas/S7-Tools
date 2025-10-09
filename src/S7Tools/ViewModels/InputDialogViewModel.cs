using ReactiveUI;
using S7Tools.Models;
using System.Reactive;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the input dialog that allows users to enter text.
/// </summary>
public class InputDialogViewModel : ViewModelBase
{
    private string _inputValue = string.Empty;
    private InputResult? _result;

    /// <summary>
    /// Initializes a new instance of the InputDialogViewModel class.
    /// </summary>
    /// <param name="request">The input request containing dialog configuration.</param>
    public InputDialogViewModel(InputRequest request)
    {
        Title = request.Title;
        Message = request.Message;
        Placeholder = request.Placeholder;
        _inputValue = request.DefaultValue ?? string.Empty;

        OkCommand = ReactiveCommand.Create(OnOk);
        CancelCommand = ReactiveCommand.Create(OnCancel);
    }

    /// <summary>
    /// Design-time constructor.
    /// </summary>
    public InputDialogViewModel() : this(new InputRequest("Enter Value", "Please enter a value:", "Default", "Placeholder..."))
    {
    }

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the dialog message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the placeholder text for the input field.
    /// </summary>
    public string? Placeholder { get; }

    /// <summary>
    /// Gets or sets the input value.
    /// </summary>
    public string InputValue
    {
        get => _inputValue;
        set => this.RaiseAndSetIfChanged(ref _inputValue, value);
    }

    /// <summary>
    /// Gets the result of the dialog interaction.
    /// </summary>
    public InputResult Result => _result ?? InputResult.Cancelled();

    /// <summary>
    /// Gets the command for the OK button.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OkCommand { get; }

    /// <summary>
    /// Gets the command for the Cancel button.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Handles the OK button action.
    /// </summary>
    private void OnOk()
    {
        _result = InputResult.Success(InputValue?.Trim() ?? string.Empty);
        // The dialog will be closed by the interaction handler
    }

    /// <summary>
    /// Handles the Cancel button action.
    /// </summary>
    private void OnCancel()
    {
        _result = InputResult.Cancelled();
        // The dialog will be closed by the interaction handler
    }
}
