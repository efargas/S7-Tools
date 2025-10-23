# Data Model: Resources and Settings Paths Management

**Feature**: 007-resources-paths-management
**Date**: 2025-10-22
**Status**: Complete

## Core Entities

### PathConfiguration

Represents the configuration for application paths and directories.

**Fields**:
- `BaseDirectory: string` - Root directory for the application (executable location)
- `ResourcesDirectory: string` - Path to Resources folder (base for all resources)
- `AppSettingsPath: string` - Path to Resources/AppSettings/AppSettings.json
- `ProfilesDirectory: string` - Path to Resources/Profiles/ (contains Serial, Socat, PowerSupply subdirs)
- `LogsDirectory: string` - Path to Resources/Logs/ (contains Main and Exported subdirs)
- `JobsPath: string` - Path to Resources/Jobs/Jobs.json
- `TasksPath: string` - Path to Resources/Tasks/Tasks.json
- `PayloadsDirectory: string` - Path to Resources/Payloads/
- `DumpsDirectory: string` - Path to Resources/Dumps/
- `MemoryRegionsDirectory: string` - Path to Resources/Profiles/MemoryRegions/
- `IsInitialized: bool` - Whether paths have been resolved and validated

**Validation Rules**:
- BaseDirectory must be a valid, existing directory
- All directory paths must be absolute paths
- Directory paths must be writable (where applicable)

**Relationships**:
- Used by all services that need file system access
- Referenced by configuration management services

### ApplicationSettings

Represents the hierarchical settings structure with default and user overrides.

**Fields**:
- `DefaultSettings: Dictionary<string, object>` - Built-in default values
- `UserSettings: Dictionary<string, object>` - User-customized values
- `EffectiveSettings: Dictionary<string, object>` - Computed merged settings
- `SettingsFilePath: string` - Path to user settings file
- `LastModified: DateTime` - When settings were last updated

**Validation Rules**:
- Settings keys must follow hierarchical naming convention
- Values must be serializable to JSON
- User settings must not contain invalid keys

**State Transitions**:
- Loading: Default → User → Effective (merge process)
- Saving: User changes → File → Reload cycle

### ResourceManifest

Represents the catalog of required application resources and their expected locations.

**Fields**:
- `RequiredDirectories: List<DirectoryInfo>` - Directories that must exist
- `RequiredFiles: List<FileInfo>` - Files that must be present
- `OptionalResources: List<ResourceInfo>` - Resources created on-demand
- `CreationStatus: Dictionary<string, bool>` - Track what was created vs existed

**Validation Rules**:
- Required resources must be creatable if missing
- Resource names must be valid for target filesystem
- Paths must not conflict with system locations

**Actual Folder Structure from Executable**:
```
Resources/
├── AppSettings/
│   └── AppSettings.json
├── Profiles/
│   ├── Serial/
│   │   └── SerialProfiles.json
│   ├── Socat/
│   │   └── SocatProfiles.json
│   ├── PowerSupply/
│   │   └── PowerSupplyProfiles.json
│   └── MemoryRegions/
│       └── **/* (various memory region files)
├── Logs/
│   ├── Main/
│   │   └── MainLog_{timestamp}_{RollingNumber}.json
│   └── Exported/
│       ├── CSV/
│       │   └── s7tools_logs_{timestamp}.csv
│       ├── TXT/
│       │   └── s7tools_logs_{timestamp}.txt
│       └── JSON/
│           └── s7tools_logs_{timestamp}.json
├── Jobs/
│   └── Jobs.json
├── Tasks/
│   └── Tasks.json
├── Payloads/
│   └── **/* (various payload files)
└── Dumps/
    └── **/* (various dump files)
```

### DirectoryInfo

Represents metadata about a required directory.

**Fields**:
- `Name: string` - Directory name
- `RelativePath: string` - Path relative to base directory
- `AbsolutePath: string` - Computed absolute path
- `IsWritable: bool` - Whether directory needs write access
- `CreateIfMissing: bool` - Whether to create if it doesn't exist
- `Purpose: string` - Human-readable description

### FileInfo

Represents metadata about a required file.

**Fields**:
- `Name: string` - File name
- `RelativePath: string` - Path relative to base directory
- `AbsolutePath: string` - Computed absolute path
- `DefaultContent: string` - Content to write if file doesn't exist
- `IsTemplate: bool` - Whether file should be created from template
- `Purpose: string` - Human-readable description

### ResourceInfo

Represents optional resources that may be created on-demand.

**Fields**:
- `Type: ResourceType` - Directory, File, or Template
- `Name: string` - Resource name
- `Path: string` - Location path
- `Dependencies: List<string>` - Other resources this depends on
- `CreationStrategy: CreationStrategy` - When/how to create

### Enumerations

**ResourceType**:
- Directory
- File
- Template
- Profile

**CreationStrategy**:
- OnStartup - Create during application initialization
- OnDemand - Create when first accessed
- Never - Must be created externally

## Entity Relationships

```
PathConfiguration
├── Contains → ResourceManifest
├── References → ApplicationSettings
└── Validates → DirectoryInfo[], FileInfo[]

ResourceManifest
├── Contains → DirectoryInfo[]
├── Contains → FileInfo[]
└── Contains → ResourceInfo[]

ApplicationSettings
├── References → PathConfiguration.SettingsDirectory
└── Depends on → FileInfo (for settings file)
```

## Data Flow

1. **Initialization Phase**:
   - PathConfiguration resolves base directory from executable location
   - ResourceManifest loads definition of required resources
   - Directories are created/validated according to manifest
   - ApplicationSettings loads default and user configurations

2. **Runtime Phase**:
   - Services request paths through PathConfiguration
   - Settings accessed through merged EffectiveSettings
   - Resource creation happens on-demand as needed

3. **Persistence Phase**:
   - User settings changes written to settings file
   - Profile data persisted to profiles directory
   - Logs written to designated log directory

## Validation Constraints

- All paths must be cross-platform compatible
- Settings values must be JSON-serializable
- Directory creation must handle permission errors gracefully
- File operations must be atomic where possible
- Resource creation must be idempotent

## Performance Considerations

- Path resolution results should be cached after first calculation
- Settings loading should be lazy-loaded on first access
- Directory existence checks should be minimized
- File watching should be used for settings change detection
