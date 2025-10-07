# systemPatterns.md

## Arquitectura Mejorada S7Tools (Octubre 2025)

### Resumen
La arquitectura de S7Tools evoluciona hacia una implementación avanzada de Clean Architecture, integrando patrones Command, Factory, Resource, validación centralizada y pruebas exhaustivas. El objetivo es maximizar la mantenibilidad, extensibilidad y robustez, alineando el desarrollo con estándares empresariales .NET.

### Capas y Patrones Clave

- **Dominio (S7Tools.Core):**
  - Entidades, Value Objects, lógica de negocio pura
  - Interfaces para servicios, comandos y validadores
- **Aplicación (S7Tools):**
  - ViewModels (MVVM, ReactiveUI)
  - Servicios de aplicación, comandos, validadores
  - Integración de patrones Command y Factory
- **Infraestructura:**
  - Implementaciones de servicios, acceso a datos, logging, exportación
  - Fábricas y proveedores concretos
- **Recursos:**
  - Archivos .resx para mensajes, errores y textos UI
  - Acceso centralizado mediante ResourceManager
- **Validación:**
  - Atributos y validadores centralizados para DTOs y settings
- **Testing:**
  - Multi-proyecto, xUnit, FluentAssertions, Moq/NSubstitute

### Flujo de Comandos (Command Pattern)
1. ViewModel crea y configura un comando (Command/CommandHandler)
2. El comando es validado (Validator)
3. El handler ejecuta la lógica (servicio, exportación, etc.)
4. El resultado se comunica al usuario (UI, logs, mensajes)

### Fábricas (Factory Pattern)
- Fábricas centralizan la creación de servicios complejos/configurables
- Integración con DI para resolución flexible

### Recursos (Resource Pattern)
- Mensajes y textos accedidos mediante claves fuertemente tipadas
- Soporte multi-idioma y localización

### Validación y Manejo de Errores
- Validación previa a la ejecución de comandos y servicios
- Logging estructurado de errores y excepciones

### Testing
- AAA pattern, cobertura mínima 85%, tests de concurrencia y edge cases

---

## Templates de Implementación

### 1. Command Pattern
```csharp
public interface ICommand<TOptions, TResult> { TOptions Options { get; } }
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TOptions, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public class ExportLogsCommand : ICommand<ExportLogsOptions, ExportResult> { /* ... */ }
public class ExportLogsCommandHandler : ICommandHandler<ExportLogsCommand, ExportResult> { /* ... */ }
```

### 2. Factory Pattern
```csharp
public interface IServiceFactory<TService>
{
    TService Create(params object[] args);
}

public class LogExportServiceFactory : IServiceFactory<ILogExportService> { /* ... */ }
```

### 3. Resource Pattern
```csharp
// .resx file: LogMessages.resx
// Access:
var message = LogMessages.ResourceManager.GetString("ExportSuccess");
```

### 4. Validación Centralizada
```csharp
public class ExportLogsOptions
{
    [Required]
    [StringLength(100)]
    public string FileName { get; set; }
}

public class ExportLogsOptionsValidator : AbstractValidator<ExportLogsOptions>
{
    public ExportLogsOptionsValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(100);
    }
}
```

### 5. Test Unitario AAA
```csharp
[Fact]
public void ExportLogsCommandHandler_ShouldExportSuccessfully()
{
    // Arrange
    var handler = new ExportLogsCommandHandler(...);
    var command = new ExportLogsCommand(...);
    
    // Act
    var result = handler.HandleAsync(command);
    
    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

---

## Decisiones y Consideraciones
- Todos los comandos y servicios deben ser validados antes de ejecutarse
- Los mensajes de usuario y logs deben obtenerse de recursos
- Las fábricas deben usarse para servicios con múltiples dependencias/configuración
- La cobertura de tests debe mantenerse y documentarse en progress.md
