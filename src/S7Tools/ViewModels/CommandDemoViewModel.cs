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

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDemoViewModel"/> class.
    /// </summary>
    /// <param name="commandFactory">The factory to create commands.</param>
    public CommandDemoViewModel(IKeyedFactory<string, ICommand> commandFactory)
    {
        _commandFactory = commandFactory;
    }

    [RelayCommand]
    private void RunHello()
    {
        // TODO: Implement command execution using ICommandDispatcher
        // var command = _commandFactory.Create("SayHello");
        // await _commandDispatcher.DispatchAsync(command);
    }
}
