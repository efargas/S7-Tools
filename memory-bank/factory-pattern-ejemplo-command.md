# Ejemplo: Factoría de comandos y uso en la UI

## 1. Implementación de una factoría de comandos

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Core.Factories;
using S7Tools.Core.Commands;

public class CommandFactory : BaseKeyedFactory<string, ICommand>
{
    public CommandFactory(ILogger logger) : base(logger) { }
    protected override void RegisterFactories()
    {
        RegisterFactory("SayHello", () => new SayHelloCommand());
        // Agrega aquí más comandos según necesidad
    }
}

public class SayHelloCommand : ICommand
{
    public void Execute() => Console.WriteLine("¡Hola desde el comando!");
}
```

---

## 2. Registro en DI

```csharp
services.TryAddSingleton<IKeyedFactory<string, ICommand>, CommandFactory>();
```

---

## 3. Ejemplo de uso en un ViewModel

```csharp
public class CommandDemoViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, ICommand> _commandFactory;
    public CommandDemoViewModel(IKeyedFactory<string, ICommand> commandFactory)
    {
        _commandFactory = commandFactory;
    }
    public void RunHello() => _commandFactory.Create("SayHello").Execute();
}
```

---

## 4. Test AAA para la factoría y el ViewModel

```csharp
using Xunit;
using S7Tools.Core.Factories;
using Microsoft.Extensions.Logging.Abstractions;

public class CommandFactoryTests
{
    [Fact]
    public void Create_Should_Return_SayHelloCommand_Given_SayHello_Key()
    {
        // Arrange
        var factory = new CommandFactory(NullLogger.Instance);
        // Act
        var command = factory.Create("SayHello");
        // Assert
        Assert.NotNull(command);
        Assert.IsType<SayHelloCommand>(command);
    }
}
```

---

## 5. Resumen
- Factoría de comandos lista para registrar y extender.
- ViewModel desacoplado, usa factoría para obtener comandos.
- Test AAA para factoría y consumidor.
