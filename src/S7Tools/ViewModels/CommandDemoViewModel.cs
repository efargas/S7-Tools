using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S7Tools.Core.Factories;
using S7Tools.Core.Commands;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel de ejemplo para ejecutar comandos usando una factoría de comandos.
/// </summary>
public partial class CommandDemoViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, ICommand> _commandFactory;

    public CommandDemoViewModel(IKeyedFactory<string, ICommand> commandFactory)
    {
        _commandFactory = commandFactory;
    }

    [RelayCommand]
    private void RunHello()
    {
        _commandFactory.Create("SayHello").Execute();
    }
}
