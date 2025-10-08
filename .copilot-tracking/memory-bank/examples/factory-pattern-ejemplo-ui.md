# Ejemplo de integración de factoría de validadores en la UI

## 1. ViewModel: PlcInputViewModel
- Usa una factoría multiclase para obtener el validador adecuado.
- Expone propiedades `Address` y `ValidationError`.
- Método `Validate()` ejecuta la validación y actualiza el error.

## 2. Vista: PlcInputView.axaml
- Entrada de texto para dirección PLC.
- Botón para validar.
- Muestra el error de validación si existe.

## 3. Código relevante

### ViewModel
```csharp
public partial class PlcInputViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, IValidator> _validatorFactory;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? validationError;
    public PlcInputViewModel(IKeyedFactory<string, IValidator> validatorFactory) { _validatorFactory = validatorFactory; }
    public void Validate()
    {
        var validator = _validatorFactory.Create("PlcAddress");
        var result = PlcAddress.Create(Address ?? string.Empty);
        if (result.IsSuccess)
        {
            var validation = validator.Validate(result.Value);
            ValidationError = validation.IsValid ? null : string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        }
        else
        {
            ValidationError = result.Error;
        }
    }
}
```

### Vista (XAML)
```xml
<UserControl ... x:Class="S7Tools.Views.PlcInputView">
  <StackPanel Margin="16" Spacing="8">
    <TextBlock Text="Dirección PLC:" FontWeight="Bold"/>
    <TextBox Text="{Binding Address, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="200"/>
    <Button Content="Validar" Command="{Binding ValidateCommand}" Width="100"/>
    <TextBlock Text="{Binding ValidationError}" Foreground="Red"/>
  </StackPanel>
</UserControl>
```

## 4. Test AAA
Ver `/tests/S7Tools.Tests/ViewModels/PlcInputViewModelTests.md`.

## 5. Resumen
- Factoría y validación desacopladas y reutilizables.
- UI reactiva y fácil de probar.
- Plantilla lista para extender a otros validadores y entradas.