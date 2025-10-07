# Blueprint: Implementación y extensión del Factory Pattern en S7Tools

## 1. Estructura recomendada

```
S7Tools.Core/
├── Factories/
│   ├── IFactory.cs
│   ├── BaseFactory.cs
│   ├── ValidatorFactory.cs         # Ejemplo concreto
│   └── ...
├── Validation/
│   ├── IValidator.cs
│   ├── BaseValidator.cs
│   ├── PlcAddressValidator.cs
│   └── ...
```

## 2. Plantilla para factoría multiclase

Ver `/templates/FactoryTemplate.cs.txt` y `/templates/FactoryWithParamsTemplate.cs.txt`.

## 3. Registro en DI

```csharp
services.TryAddSingleton<IKeyedFactory<string, IValidator>, ValidatorFactory>();
```

## 4. Ejemplo de extensión
- Para cada nuevo validador, añade un `RegisterFactory("Key", () => new NuevoValidator())` en la factoría.
- Para factorías con parámetros, usa la plantilla `FactoryWithParamsTemplate.cs.txt`.

## 5. Buenas prácticas
- Usa factorías multiclase para desacoplar lógica y facilitar pruebas.
- Registra factorías por interfaz en DI.
- Mantén la documentación y plantillas actualizadas.
- Usa tests AAA para factorías y consumidores.

---

**Este blueprint es la base para escalar el uso de factorías en S7Tools.**
