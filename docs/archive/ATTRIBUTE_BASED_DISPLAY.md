---
title: "Attribute-Based Property Display Documentation"
version: "1.0.0"
created: "2025-01-15"
last-updated: "2025-11-10"
status: "deprecated"
deprecated-date: "2025-11-10"
superseded-by: "docs/patterns/reusable-controls.md"
removal-date: "2027-11-10"
tags: ["deprecated", "ui", "attributes", "display"]
related:
  - docs/patterns/reusable-controls.md
  - docs/architecture/mvvm-patterns.md
---

# ⚠️ DEPRECATED: Attribute-Based Property Display Documentation

**This document is deprecated as of 2025-11-10.**

**Use instead**: [Reusable Controls Pattern](./patterns/reusable-controls.md)

**Removal Date**: 2027-11-10 (2-year retention policy)

**Reason**: Content has been consolidated into the Reusable Controls pattern documentation with expanded examples and best practices.

---

## Original Content (For Reference)

## Overview

The S7Tools application uses a reflection-based system (`ObjectToPropertiesConverter`) to automatically generate UI displays of model properties. This system is controlled through .NET attributes, allowing developers to explicitly define which properties are shown, their labels, and their display order.

## Supported Attributes

### [Browsable(false)]
Hides a property from reflection-based displays.

```csharp
[Browsable(false)]
public int Id { get; set; }

[Browsable(false)]
public DateTime CreatedAt { get; set; }
```

**Use cases:**
- Internal properties (Id, Version, timestamps)
- Metadata and extensibility fields
- Properties handled elsewhere in the UI
- Low-level configuration flags combined into higher-level properties

### [Display(Name = "...", Order = N)]
Customizes the display label and ordering of properties.

```csharp
[Display(Name = "TCP Port", Order = 1)]
public int TcpPort { get; set; }

[Display(Name = "Host Address", Order = 2)]
public string Host { get; set; }
```

**Name parameter:**
- Provides a human-friendly label instead of the property name
- Use clear, descriptive names (e.g., "Baud Rate" instead of "BaudRate")
- Include units where appropriate (e.g., "Block Size (bytes)")

**Order parameter:**
- Controls the display sequence (1 = first, 2 = second, etc.)
- Properties without Order appear last, sorted alphabetically
- Use gaps (1, 2, 3, 5, 10) to allow future insertions

## Implementation Pattern

### Configuration Models

Configuration classes should:
1. Display key settings with proper ordering
2. Hide internal/metadata fields
3. Use clear, user-friendly labels

```csharp
public class SocatConfiguration
{
    [Display(Name = "TCP Port", Order = 1)]
    [Range(1, 65535)]
    public int TcpPort { get; set; } = 1238;

    [Display(Name = "Block Size (bytes)", Order = 7)]
    [Range(1, 65536)]
    public int BlockSize { get; set; } = 4;

    [Browsable(false)]
    public string Version { get; set; } = "1.0";

    [Browsable(false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Record Types

Records use the `[property:]` target for attributes:

```csharp
public sealed record MemoryRegionProfile(
    [property: Display(Name = "Start Address (bytes)", Order = 1)]
    uint Start,
    [property: Display(Name = "Length (bytes)", Order = 2)]
    uint Length
);
```

### Profile Classes

Profile classes typically:
- Hide: Id, Version, Options, Flags, Metadata, timestamps, Configuration object
- Show: Name, Description, IsDefault, IsReadOnly

```csharp
public class SerialPortProfile : IProfileBase
{
    [Browsable(false)]
    public int Id { get; set; }

    [Display(Name = "Profile Name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description", Order = 2)]
    public string Description { get; set; } = string.Empty;

    [Browsable(false)]
    public SerialPortConfiguration Configuration { get; set; } = new();

    [Display(Name = "Default Profile", Order = 3)]
    public bool IsDefault { get; set; }
}
```

## Converter Behavior

The `ObjectToPropertiesConverter` processes attributes as follows:

1. **Filtering**: Excludes properties with `[Browsable(false)]`
2. **Ordering**: Sorts by `[Display(Order = N)]` ascending, then by name
3. **Labeling**: Uses `[Display(Name = "...")]` if present, otherwise formats property name
4. **Formatting**: Applies type-specific formatting (bool → "True"/"False", DateTime → "yyyy-MM-dd HH:mm")

### Example Output

Given this model:
```csharp
public class ModbusTcpConfiguration
{
    [Display(Name = "Host Address", Order = 1)]
    public string Host { get; set; } = "192.168.1.100";

    [Display(Name = "TCP Port", Order = 2)]
    public int Port { get; set; } = 502;

    [Browsable(false)]
    public DateTime CreatedAt { get; set; }
}
```

The converter produces:
```
Host Address: 192.168.1.100
TCP Port: 502
```

## Best Practices

### DO:
✅ Use Display attributes on all user-facing configuration properties
✅ Apply logical ordering (1, 2, 3...) based on importance/workflow
✅ Hide internal properties (Id, Version, timestamps, metadata)
✅ Include units in labels where applicable ("Timeout (ms)", "Size (bytes)")
✅ Use clear, descriptive names that match user expectations

### DON'T:
❌ Leave gaps in ordering that might confuse (1, 2, 10, 11...)
❌ Display Configuration objects (they're rendered separately)
❌ Show low-level implementation details to end users
❌ Use technical property names without Display attribute
❌ Forget to mark metadata/extensibility fields as Browsable(false)

## Testing

The test suite (`ObjectToPropertiesConverterTests.cs`) validates:
- Browsable attribute filtering
- Display name customization
- Display order enforcement
- Null value handling
- Boolean formatting
- Backward compatibility (properties without attributes still work)

## Related Files

- **Converter**: `/src/S7Tools/Converters/ObjectToPropertiesConverter.cs`
- **Tests**: `/tests/S7Tools.Tests/Converters/ObjectToPropertiesConverterTests.cs`
- **Example Models**:
  - `/src/S7Tools.Core/Models/SocatConfiguration.cs`
  - `/src/S7Tools.Core/Models/SerialPortConfiguration.cs`
  - `/src/S7Tools.Core/Models/ModbusTcpConfiguration.cs`
  - `/src/S7Tools.Core/Models/SerialPortProfile.cs`
  - `/src/S7Tools.Core/Models/Jobs/MemoryRegionProfile.cs`

## Migration from Implicit to Explicit

### Before (Implicit):
```csharp
public int TcpPort { get; set; } = 1238;
public bool EnableFork { get; set; } = true;
public DateTime CreatedAt { get; set; }
```

Properties displayed as:
- "Created At: 2025-10-23 06:29"
- "Enable Fork: True"
- "Tcp Port: 1238"

### After (Explicit):
```csharp
[Display(Name = "TCP Port", Order = 1)]
public int TcpPort { get; set; } = 1238;

[Display(Name = "Enable Fork Mode", Order = 2)]
public bool EnableFork { get; set; } = true;

[Browsable(false)]
public DateTime CreatedAt { get; set; }
```

Properties displayed as:
- "TCP Port: 1238"
- "Enable Fork Mode: True"

The CreatedAt property is hidden and doesn't appear in the UI.

## Summary

The attribute-based approach provides:
- **Explicit Control**: Clear declaration of what should be displayed
- **Better UX**: User-friendly labels and logical ordering
- **Maintainability**: Self-documenting models that specify UI requirements
- **Flexibility**: Easy to adjust display without changing UI code
- **Robustness**: Protected against accidental display of internal properties
