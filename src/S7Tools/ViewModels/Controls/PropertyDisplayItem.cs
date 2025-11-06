using ReactiveUI;

namespace S7Tools.ViewModels.Controls;

/// <summary>
/// Represents a single profile property for display in the UI
/// </summary>
public class PropertyDisplayItem : ReactiveObject
{
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string? _tooltip;
    private bool _isHighlighted;
    private PropertyValidationState _validationState;

    /// <summary>
    /// Human-readable property name/label
    /// </summary>
    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }

    /// <summary>
    /// Formatted property value for display
    /// </summary>
    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    /// <summary>
    /// Optional tooltip with additional information
    /// </summary>
    public string? Tooltip
    {
        get => _tooltip;
        set => this.RaiseAndSetIfChanged(ref _tooltip, value);
    }

    /// <summary>
    /// True for important properties that should be emphasized
    /// </summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
    }

    /// <summary>
    /// Validation state for visual styling
    /// </summary>
    public PropertyValidationState ValidationState
    {
        get => _validationState;
        set => this.RaiseAndSetIfChanged(ref _validationState, value);
    }
}

/// <summary>
/// Validation state for property display items
/// </summary>
public enum PropertyValidationState
{
    Valid,
    Warning,
    Error
}
