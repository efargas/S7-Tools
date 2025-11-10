# S7Tools Constants Library

This directory contains centralized constant definitions used throughout the S7Tools application. All constants are organized into focused, single-responsibility classes to maintain clarity and ease of maintenance.

## 📚 Available Constant Classes

### 1. DateTimeFormats.cs

**Purpose**: Standard date and time format strings for consistent formatting across the application.

**Usage**:

```csharp
using S7Tools.Core.Constants;

string shortDate = DateTime.Now.ToString(DateTimeFormats.ShortDateTime);
string fileTimestamp = DateTime.Now.ToString(DateTimeFormats.FileTimestamp);
```

**Available Formats**:

- `ShortDateTime` - `"yyyy-MM-dd HH:mm"` - General UI display
- `LongDateTime` - `"yyyy-MM-dd HH:mm:ss"` - Detailed timestamps
- `MillisecondDateTime` - `"yyyy-MM-dd HH:mm:ss.fff"` - High-precision logs
- `FileTimestamp` - `"yyyyMMdd_HHmmss"` - Filename-safe timestamps
- `IsoDateTime` - `"yyyy-MM-ddTHH:mm:ssZ"` - ISO 8601 standard
- `DateOnly` - `"yyyy-MM-dd"` - Date without time
- `TimeOnly` - `"HH:mm:ss"` - Time without date
- `ShortTime` - `"HH:mm"` - Abbreviated time
- `DecimalId` - `"D"` - Decimal ID formatting

**Files Using**: 11 files across converters, services, and ViewModels

---

### 2. NetworkConstants.cs

**Purpose**: Network-related constants including port ranges and validation values.

**Usage**:

```csharp
using S7Tools.Core.Constants;

if (port < NetworkConstants.MinValidPort || port > NetworkConstants.MaxValidPort)
{
    throw new ArgumentOutOfRangeException(nameof(port), NetworkConstants.PortRangeErrorMessage);
}
```

**Available Constants**:

- `MinValidPort` - `1` - Minimum valid TCP/UDP port
- `MaxValidPort` - `65535` - Maximum valid TCP/UDP port
- `DefaultSocatTcpPort` - `23` - Default Socat TCP port (Telnet)
- `DefaultModbusTcpPort` - `502` - Default Modbus TCP port
- `PortRangeErrorMessage` - Validation error message

**Files Using**: 6 files in services and ViewModels

---

### 3. MemoryConstants.cs

**Purpose**: PLC memory-related constants for default addresses and sizes.

**Usage**:

```csharp
using S7Tools.Core.Constants;

var memoryRegion = new MemoryRegionProfile(
    MemoryConstants.DefaultUserMemoryStart,
    MemoryConstants.DefaultDumpSize
);
```

**Available Constants**:

- `DefaultUserMemoryStart` - `0x20000000` - Default user memory start address
- `DefaultDumpSize` - `0x1000` - Default dump size (4KB)
- `KiB` - `1024` - Bytes per KiB
- `MiB` - `1048576` - Bytes per MiB
- `MaxSegmentNameLength` - `100` - Maximum segment name length
- `MinSegmentSize` - `1` - Minimum segment size
- `MaxSegmentSize` - `16777216` - Maximum segment size (16MB)
- `MaxTotalMemorySize` - `268435456` - Maximum total memory (256MB)

**Files Using**: 8 files in models, services, and ViewModels

---

### 4. ColorPalette.cs

**Purpose**: UI color definitions organized by semantic groups.

**Usage**:

```csharp
using S7Tools.Core.Constants;
using Avalonia.Media;

var warningColor = ColorPalette.Warning.Orange;
var errorColor = ColorPalette.Error.Crimson;
```

**Available Color Groups**:

- **Semantic Colors**:
    - `Warning.Orange` - `Color.FromRgb(255, 165, 0)`
    - `Error.Crimson` - `Color.FromRgb(220, 20, 60)`
    - `Success.Green` - `Color.FromRgb(0, 128, 0)`
    - `Info.Blue` - `Color.FromRgb(0, 123, 255)`

- **Status Colors**:
    - `Status.Active` - `Color.FromRgb(76, 175, 80)`
    - `Status.Inactive` - `Color.FromRgb(158, 158, 158)`
    - `Status.Pending` - `Color.FromRgb(255, 193, 7)`

- **Priority Colors**:
    - `Priority.High` - `Color.FromRgb(244, 67, 54)`
    - `Priority.Medium` - `Color.FromRgb(255, 152, 0)`
    - `Priority.Low` - `Color.FromRgb(33, 150, 243)`

**Files Using**: 2 files in converters

---

### 5. ResourcePathConstants.cs

**Purpose**: Default resource paths and directories.

**Usage**:

```csharp
using S7Tools.Core.Constants;

string payloadPath = ResourcePathConstants.DefaultPayloadsDirectory;
string profilesPath = ResourcePathConstants.DefaultProfilesDirectory;
```

**Available Paths**:

- `DefaultPayloadsDirectory` - Default bootloader payloads directory
- `DefaultProfilesDirectory` - Default profiles storage directory
- `DefaultDumpsDirectory` - Default memory dumps output directory
- `DefaultLogsDirectory` - Default logs output directory

**Files Using**: 3 files in models and services

---

## 🎯 Design Principles

### 1. Single Responsibility
Each constant class focuses on a specific domain:
- **DateTimeFormats** → Time/date formatting only
- **NetworkConstants** → Network configuration only
- **MemoryConstants** → Memory addresses/sizes only
- **ColorPalette** → UI colors only
- **ResourcePathConstants** → File system paths only

### 2. Type Safety
All constants use appropriate types:
- `const string` for format strings and messages
- `const int` for numeric values
- `static readonly Color` for color definitions
- `const uint` for memory addresses

### 3. Discoverability
- Clear, semantic naming (e.g., `DefaultUserMemoryStart` not `ADDR1`)
- Grouped by purpose (e.g., `Warning.Orange`, `Error.Crimson`)
- XML documentation with examples for each constant

### 4. Maintainability
- One place to update values used across multiple files
- Compile-time checking prevents typos
- Easy to find all usages via IDE "Find References"

---

## 📖 Usage Guidelines

### When to Add a New Constant

✅ **DO** add constants for:
- Values used in 3+ places
- "Magic numbers" that need explanation
- Configuration defaults
- Format strings used across multiple files
- Color values for semantic purposes

❌ **DON'T** add constants for:
- Single-use values
- Values that change frequently
- User-configurable settings (use Options pattern instead)
- Calculation results (use methods instead)

### Naming Conventions

- **PascalCase** for all constant names
- **Descriptive names** that explain the purpose
- **Prefix with category** when grouping (e.g., `MinValidPort`, `MaxValidPort`)
- **Avoid abbreviations** unless universally understood (e.g., `KiB`, `TCP`)

### Organization Patterns

```csharp
// Group related constants
public static class NetworkConstants
{
    // Port range validation
    public const int MinValidPort = 1;
    public const int MaxValidPort = 65535;

    // Default ports by protocol
    public const int DefaultSocatTcpPort = 23;
    public const int DefaultModbusTcpPort = 502;

    // Error messages
    public const string PortRangeErrorMessage = "...";
}
```

---

## 🔧 Maintenance Checklist

When modifying constants:

1. ✅ Update XML documentation if changing meaning
2. ✅ Search for usages if renaming (IDE "Find References")
3. ✅ Update this README if adding new constants
4. ✅ Run `dotnet build` to verify no compilation errors
5. ✅ Run `dotnet test` to ensure tests still pass
6. ✅ Update migration guides if deprecating constants

---

## 📊 Statistics

- **Total Constant Classes**: 5
- **Total Constants Defined**: 35+
- **Files Using Constants**: 30+
- **Code Duplication Eliminated**: ~50 hardcoded values replaced

---

## 🔗 Related Documentation

- [PATTERNS_REFERENCE.md](../../../PATTERNS_REFERENCE.md) - Architecture patterns
- [DEPRECATED_PROPERTY_MIGRATION.md](../../../docs/DEPRECATED_PROPERTY_MIGRATION.md) - Migration guides
- [systemPatterns.md](../../../.copilot-tracking/memory-bank/systemPatterns.md) - System patterns

---

**Last Updated**: 2025-11-10
**Maintainer**: S7Tools Development Team
