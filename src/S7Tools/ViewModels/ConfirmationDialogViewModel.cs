using Avalonia.Controls;
using ReactiveUI;
using System.Windows.Input;

namespace S7Tools.ViewModels;

public class ConfirmationDialogViewModel : ViewModelBase
{
    public string Title { get; }
    public string Message { get; }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    public ConfirmationDialogViewModel(Window window, string title, string message)
    {
        Title = title;
        Message = message;

        OkCommand = ReactiveCommand.Create(() => window.Close(true));
        CancelCommand = ReactiveCommand.Create(() => window.Close(false));
    }
}