namespace S7Tools.Core.Models;

/// <summary>
/// Represents a tag (data point) from a PLC.
/// </summary>
public class Tag
{
    /// <summary>
    /// Gets or sets the name of the tag.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the address of the tag in the PLC.
    /// </summary>
    public string Address { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value of the tag.
    /// </summary>
    public object? Value { get; set; }
}
