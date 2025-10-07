# Factory Pattern - S7Tools

## 1. Estado actual del patrón Factory

### Interfaces y clases base
- **IFactory<T> / IFactory<T, TParams>**: Factoría simple y parametrizada.
- **IAsyncFactory<T> / IAsyncFactory<T, TParams>**: Factoría asíncrona simple y parametrizada.
- **IKeyedFactory<TKey, TBase> / IKeyedFactory<TKey, TBase, TParams>**: Factoría multiclase basada en clave (útil para plugin, validadores, recursos, etc.).
- **BaseKeyedFactory<TKey, TBase> / BaseKeyedFactory<TKey, TBase, TParams>**: Implementaciones base para factorías multiclase, con registro y logging integrados.

### Características
- Soporte para factorías sin parámetros, con parámetros, asíncronas y multiclase.
- Logging integrado en factorías base.
- Pensado para integración con DI y descubrimiento automático en el futuro.
- No existen implementaciones concretas en el dominio aún (solo infraestructura base).

### Ejemplo de uso (factoría simple)
```csharp
public class MyServiceFactory : IFactory<IMyService>
{
    public IMyService Create() => new MyService();
}
```

### Ejemplo de uso (factoría multiclase)
```csharp
public class ValidatorFactory : BaseKeyedFactory<string, IValidator>
{
    public ValidatorFactory(ILogger logger) : base(logger) { }
    protected override void RegisterFactories()
    {
        RegisterFactory("PlcAddress", () => new PlcAddressValidator());
        // Agrega más validadores aquí
    }
}
```

---

## 2. Integración prevista y compatibilidad

- **Compatibilidad total** con Resource y Validation Pattern: permite factorías de validadores, recursos, servicios, etc.
- **Preparado para Factory Pattern avanzado**: descubrimiento automático, registro por convención, integración con DI.
- **Extensible**: puedes crear factorías para cualquier tipo, con o sin parámetros, y con soporte para claves (plugins, tipos, etc.).
- **Logging**: todas las factorías base soportan logging estructurado para trazabilidad.

### Siguiente paso recomendado
- Implementar una factoría concreta de ejemplo (por ejemplo, para validadores o recursos) y registrar en DI.
- Documentar el uso avanzado y patrones de extensión.

---

## 3. Mejores prácticas
- Usa siempre las interfaces de factoría en tus servicios y ViewModels.
- Prefiere factorías multiclase para escenarios de plugin, validadores, recursos o servicios polimórficos.
- Integra el registro DI en `ServiceCollectionExtensions` para factorías reutilizables.
- Mantén la documentación y ejemplos actualizados para el equipo.

---

**Este patrón está listo para ser extendido y automatizado según las necesidades del proyecto.**
