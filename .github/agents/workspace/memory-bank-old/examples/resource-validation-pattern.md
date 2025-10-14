# Resource & Validation Pattern - S7Tools

## Resumen

Este documento describe la arquitectura, uso y puntos de extensión de los patrones Resource y Validation implementados en S7Tools. Está dirigido a desarrolladores que integren, extiendan o mantengan estos patrones, y sienta las bases para futuras extensiones como el Factory Pattern o recursos externos.

---

## 1. Resource Pattern

### Objetivo
Proveer acceso centralizado, seguro y extensible a recursos localizados (strings, mensajes, etc.) con soporte para múltiples culturas y fácil integración en DI.

### Componentes
- **IResourceManager**: Contrato para acceso a recursos localizados.
- **InMemoryResourceManager**: Implementación simple, útil para pruebas, desarrollo y recursos dinámicos.
- **ResourceManager**: Decorador preparado para delegar a InMemoryResourceManager o futuras fuentes externas.

### Registro en DI
```csharp
// En ServiceCollectionExtensions.cs
services.TryAddSingleton<IResourceManager, InMemoryResourceManager>();
```

### Uso básico
```csharp
public class MyViewModel
{
    private readonly IResourceManager _resources;
    public MyViewModel(IResourceManager resources) { _resources = resources; }
    public string Greeting => _resources.GetString("Hello");
}
```

### Extensión futura
- Cambia el registro DI a `ResourceManager` para soporte de recursos externos, archivos, bases de datos, etc.
- Implementa `IResourceManager` adicional y decóralo según necesidades.

---

## 2. Validation Pattern

### Objetivo
Permitir validación robusta, reutilizable y desacoplada de value objects, modelos y DTOs, con integración a DI y soporte para reglas sincrónicas/asíncronas.

### Componentes
- **IValidator<T>**: Contrato para validadores tipados.
- **BaseValidator<T>**: Base para validadores, permite definir reglas con `AddRule` y `CreateRule`.
- **ValidationService**: Servicio centralizado para registro y uso de validadores.
- **PlcAddressValidator**: Ejemplo concreto para validar direcciones PLC.

### Registro en DI
```csharp
// En ServiceCollectionExtensions.cs
services.TryAddSingleton<IValidationService, ValidationService>();
```

### Uso básico
```csharp
// Registro manual (puede automatizarse con Factory Pattern en el futuro)
var validationService = new ValidationService();
validationService.RegisterValidator(new PlcAddressValidator());

// Validación
var result = validationService.Validate(new PlcAddress("DB1.DBX0.0"));
if (!result.IsValid) { /* manejar errores */ }
```

### Extensión futura
- Factory Pattern: Automatizar el registro/descubrimiento de validadores.
- Integración con UI: Validación reactiva en formularios y ViewModels.
- Reglas externas: Cargar reglas desde archivos o base de datos.

---

## 3. Puntos de extensión y mejores prácticas

- **Inyección de dependencias**: Usa siempre las interfaces (`IResourceManager`, `IValidationService`) en tus servicios y ViewModels.
- **Pruebas**: Usa `InMemoryResourceManager` y validadores concretos para pruebas unitarias.
- **Futuro Factory Pattern**: Deja el registro de validadores y recursos desacoplado, para permitir descubrimiento automático o configuración externa.
- **Recursos externos**: Implementa y registra un decorador de `IResourceManager` para cargar recursos desde archivos, web o base de datos.
- **Documentación**: Mantén este documento actualizado ante cualquier cambio relevante en la arquitectura.

---

## 4. Ejemplo de integración en ViewModel

```csharp
public class PlcInputViewModel : ObservableObject
{
    private readonly IValidationService _validationService;
    public PlcInputViewModel(IValidationService validationService)
    {
        _validationService = validationService;
    }
    public string Address { get; set; }
    public string? ValidationError { get; private set; }
    public void Validate()
    {
        var result = PlcAddress.Create(Address);
        if (result.IsSuccess)
        {
            var validation = _validationService.Validate(result.Value);
            ValidationError = validation.IsValid ? null : string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        }
        else
        {
            ValidationError = result.Error;
        }
    }
}
```

---

## 5. Conclusión

El patrón actual es seguro, extensible y preparado para evolucionar hacia una arquitectura aún más desacoplada y automatizada. Cualquier extensión (Factory, recursos externos, reglas dinámicas) puede implementarse sin romper la compatibilidad existente.

**Mantén el registro DI y la documentación actualizados ante cualquier cambio.**