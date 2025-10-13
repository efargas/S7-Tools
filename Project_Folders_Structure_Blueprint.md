# S7Tools Project Folder Structure Blueprint

**Generated**: 2025-10-13  
**Project Type**: .NET 8.0 Avalonia Desktop Application  
**Architecture**: Clean Architecture with MVVM Pattern  
**Last Updated**: 2025-10-13  

---

## 1. Structural Overview

### Project Type Detection Results

**Primary Technology**: **.NET 8.0** with Avalonia UI Framework  
**Architecture Pattern**: **Clean Architecture** with **MVVM (Model-View-ViewModel)**  
**UI Framework**: **Avalonia UI 11.3.6** with ReactiveUI  
**Logging Framework**: **Microsoft.Extensions.Logging** with custom DataStore provider  
**Dependency Injection**: **Microsoft.Extensions.DependencyInjection**  

### Organizational Principles

The S7Tools project follows a **multi-project solution architecture** organized by:

1. **Layer Separation**: Core domain logic, infrastructure concerns, and presentation layer are separated into distinct projects
2. **Feature-Based Organization**: Within each project, code is organized by functional areas (Services, ViewModels, Views, etc.)
3. **Clean Architecture Compliance**: Dependencies flow inward toward the core domain, with abstractions defined in core projects
4. **MVVM Pattern**: Strict separation between UI (Views), presentation logic (ViewModels), and business logic (Services/Models)

### Solution Structure Rationale

- **S7Tools.Core**: Contains domain models and service interfaces (dependency-free core)
- **S7Tools.Infrastructure.Logging**: Specialized infrastructure for logging concerns
- **S7Tools**: Main application project containing UI, ViewModels, and application services

---

## 2. Directory Visualization

```
S7Tools/
├── .copilot-tracking/                    # AI-assisted development tracking
│   ├── bugs/                             # Bug reports and tracking
│   ├── memory/                           # Memory keeper files (preserve)
│   ├── memory-bank/                      # Structured memory bank documentation
│   │   ├── examples/                     # Example implementations and templates
│   │   ├── tasks/                        # Individual task tracking files
│   │   ├── activeContext.md              # Current development context
│   │   ├── instructions.md               # Project-specific instructions
│   │   ├── mvvm-lessons-learned.md       # MVVM pattern insights
│   │   ├── productContext.md             # Product vision and context
│   │   ├── progress.md                   # Development progress tracking
│   │   ├── projectbrief.md               # Project overview and goals
│   │   ├── session-summary.md            # Session summaries
│   │   ├── systemPatterns.md             # Architecture patterns
│   │   ├── techContext.md                # Technical environment
│   │   └── threading-and-synchronization-patterns.md  # Threading patterns
│   ├── prompts/                          # Implementation prompt templates
│   ├── research/                         # Research findings and analysis
│   └── reviews/                          # Code and design reviews
├── .github/                              # GitHub-specific configurations
│   ├── agents/                           # AI agent workspace and references
│   │   └── workspace/                    # Agent working directory
│   ├── chatmodes/                        # AI chat mode configurations
│   ├── instructions/                     # Development instructions
│   │   ├── dotnet-architecture-good-practices.instructions.md
│   │   └── memory-bank.instructions.md
│   └── prompts/                          # Reusable prompt templates
├── .vscode/                              # VS Code workspace settings
├── Screenshots/                          # UI screenshots and documentation (if exists)
│   └── prompts/                          # Reusable prompt templates
├── .qodo/                                # Qodo AI tracking (legacy)
├── .vscode/                              # VS Code workspace settings
├── Screenshots/                          # UI screenshots and documentation
├── src/                                  # Source code root
│   ├── S7Tools/                          # Main application project
│   │   ├── Assets/                       # Application assets (icons, images)
│   │   ├── Constants/                    # Application constants
│   │   ├── Converters/                   # XAML value converters
│   │   ├── Extensions/                   # Extension methods and utilities
│   │   ├── Helpers/                      # Helper classes and utilities
│   │   ├── Models/                       # Application-specific models
│   │   ├── Resources/                    # Localization and resource files
│   │   │   └── Strings/                  # String resources for i18n
│   │   ├── Services/                     # Application services
│   │   │   ├── Bootloader/               # Bootloader-specific services
│   │   │   ├── Interfaces/               # Service contracts
│   │   │   └── Tasking/                  # Task scheduling services
│   │   ├── Styles/                       # XAML styles and themes
│   │   ├── ViewModels/                   # MVVM ViewModels
│   │   └── Views/                        # XAML Views and code-behind
│   ├── S7Tools.Core/                     # Core domain project
│   │   ├── Commands/                     # Command pattern implementations
│   │   ├── Factories/                    # Factory pattern implementations
│   │   ├── Logging/                      # Logging abstractions
│   │   ├── Models/                       # Domain models
│   │   │   ├── Common/                   # Common models
│   │   │   ├── Jobs/                     # Job-related models
│   │   │   ├── Validators/               # Model validators
│   │   │   └── ValueObjects/             # Value objects
│   │   ├── Resources/                    # Resource abstractions
│   │   ├── Services/                     # Core service interfaces
│   │   │   └── Interfaces/               # Service contracts
│   │   └── Validation/                   # Validation framework
│   ├── S7Tools.Diagnostics/              # Diagnostic tools project
│   ├── S7Tools.Infrastructure.Logging/   # Logging infrastructure
│   │   ├── Core/                         # Core logging components
│   │   │   ├── Configuration/            # Logger configuration
│   │   │   ├── Models/                   # Logging models
│   │   │   └── Storage/                  # Log storage implementations
│   │   └── Providers/                    # Logging providers
│   │       ├── Extensions/               # DI registration extensions
│   │       └── Microsoft/                # Microsoft.Extensions.Logging integration
│   └── S7Tools.sln                       # Visual Studio solution file
├── tests/                                # Test projects root
│   ├── S7Tools.Core.Tests/               # Core domain tests
│   │   ├── Models/                       # Model tests
│   │   ├── Resources/                    # Resource tests
│   │   └── Validation/                   # Validation tests
│   ├── S7Tools.Infrastructure.Logging.Tests/  # Logging infrastructure tests
│   │   └── Core/                         # Core logging tests
│   └── S7Tools.Tests/                    # Application tests
│       ├── Converters/                   # Converter tests
│       ├── Services/                     # Service tests
│       └── ViewModels/                   # ViewModel tests
├── .editorconfig                         # Code style and formatting rules
├── .gitignore                            # Git ignore patterns
└── README.md                             # Project documentation
```

---

## 3. Key Directory Analysis

### .NET Project Structure

#### Solution Organization
- **Single Solution**: `S7Tools.sln` contains all related projects
- **Solution Folders**: Projects are organized under a "src" solution folder
- **Project Dependencies**: Clear dependency hierarchy (Core ← Infrastructure ← Application)
- **Multi-targeting**: All projects target .NET 8.0 with latest C# language features

#### Project Organization

##### S7Tools (Main Application)
- **Purpose**: Primary Avalonia UI application with MVVM architecture
- **Dependencies**: References both Core and Infrastructure.Logging projects
- **Key Features**: VSCode-like UI, comprehensive logging, PLC communication
- **Architecture**: Clean separation of concerns with service-oriented design

##### S7Tools.Core (Domain Layer)
- **Purpose**: Domain models and service contracts
- **Dependencies**: No external dependencies (pure domain layer)
- **Contents**: Business entities, service interfaces, domain logic
- **Principle**: Dependency-free core following Clean Architecture

##### S7Tools.Infrastructure.Logging (Infrastructure Layer)
- **Purpose**: Specialized logging infrastructure with Microsoft.Extensions.Logging
- **Dependencies**: Microsoft.Extensions.Logging packages
- **Features**: Circular buffer storage, real-time notifications, multiple providers
- **Integration**: Custom DataStore provider for high-performance logging

#### Domain/Feature Organization
- **Services**: Grouped by functional area (Activity Bar, Layout, Theme, etc.)
- **ViewModels**: One ViewModel per View with clear naming conventions
- **Views**: XAML views with corresponding code-behind files
- **Models**: Application-specific models separate from domain models

#### Layer Organization
- **Presentation Layer**: Views, ViewModels, Converters (S7Tools project)
- **Application Layer**: Services, Extensions (S7Tools project)
- **Domain Layer**: Core models and interfaces (S7Tools.Core project)
- **Infrastructure Layer**: Logging, external integrations (S7Tools.Infrastructure.* projects)

#### Configuration Management
- **Project Files**: `.csproj` files with modern SDK-style format
- **EditorConfig**: Comprehensive code style and quality rules
- **App Manifest**: Windows-specific application manifest
- **Resource Files**: Localization resources with designer-generated code

#### Test Project Organization
- **Current State**: No test projects present
- **Recommended Structure**: Mirror source structure with `.Tests` suffix
- **Categories**: Unit tests, integration tests, UI tests
- **Location**: `tests/` folder parallel to `src/`

### UI Project Structure

#### Component Organization
- **Views**: XAML files with code-behind, organized by functional area
- **ViewModels**: Corresponding ViewModels following MVVM pattern
- **Converters**: Value converters for data binding transformations
- **Shared Components**: Reusable UI components and behaviors

#### State Management
- **ReactiveUI**: Used for reactive programming and state management
- **Services**: Application state managed through service layer
- **ViewModels**: Local state management within ViewModels
- **Dependency Injection**: State services registered in DI container

#### Routing Organization
- **Navigation**: Handled through ViewModels and service layer
- **View Resolution**: ViewLocator pattern for View-ViewModel mapping
- **Content Areas**: Dynamic content switching based on navigation state

#### API Integration
- **Services**: Service layer abstracts external API calls
- **Interfaces**: Service contracts defined in Core project
- **Models**: Data transfer objects in Models folder
- **Error Handling**: Centralized error handling through service layer

#### Asset Management
- **Assets Folder**: Application icons, images, and static resources
- **Resource Files**: Localization strings and embedded resources
- **Styles**: XAML styles and themes for consistent UI appearance

#### Style Organization
- **Styles.axaml**: Main style definitions
- **Theme System**: VSCode-inspired color schemes and styling
- **Converters**: Style-related value converters for dynamic styling

---

## 4. File Placement Patterns

### Configuration Files
- **Solution Level**: `.editorconfig`, `.gitignore`, `README.md` at repository root
- **Project Level**: `.csproj` files in respective project directories
- **Application Config**: `app.manifest` in main application project
- **Development Config**: `.vscode/` folder for VS Code settings

### Model/Entity Definitions
- **Domain Models**: `S7Tools.Core/Models/` for core business entities
- **Application Models**: `S7Tools/Models/` for UI-specific models
- **Logging Models**: `S7Tools.Infrastructure.Logging/Core/Models/` for logging entities
- **ViewModels**: `S7Tools/ViewModels/` for MVVM presentation models

### Business Logic
- **Core Services**: Interfaces in `S7Tools.Core/Services/Interfaces/`
- **Application Services**: Implementations in `S7Tools/Services/`
- **Infrastructure Services**: Specialized services in infrastructure projects
- **Extensions**: Utility extensions in `S7Tools/Extensions/`

### Interface Definitions
- **Service Contracts**: All interfaces in `*/Services/Interfaces/` folders
- **Naming Convention**: Interfaces prefixed with 'I' (e.g., `IActivityBarService`)
- **Separation**: Interfaces separated from implementations
- **Dependencies**: Core interfaces have no external dependencies

### Test Files
- **Recommended Structure**: `tests/` folder parallel to `src/`
- **Naming Pattern**: `{ProjectName}.Tests` for test projects
- **Organization**: Mirror source project structure
- **Categories**: Unit, Integration, and UI test separation

### Documentation Files
- **Repository Root**: `README.md` for project overview
- **Screenshots**: `Screenshots/` folder for UI documentation
- **Tracking**: `.copilot-tracking/` for development documentation
- **Code Documentation**: XML documentation in source files

---

## 5. Naming and Organization Conventions

### File Naming Patterns
- **C# Files**: PascalCase (e.g., `MainWindowViewModel.cs`)
- **XAML Files**: PascalCase with `.axaml` extension (e.g., `MainWindow.axaml`)
- **Interface Files**: PascalCase with 'I' prefix (e.g., `IActivityBarService.cs`)
- **Resource Files**: PascalCase with descriptive names (e.g., `UIStrings.resx`)

### Folder Naming Patterns
- **Project Folders**: PascalCase (e.g., `ViewModels`, `Services`)
- **Namespace Folders**: Match namespace structure exactly
- **Functional Grouping**: Folders represent functional areas or technical concerns
- **Consistency**: Same naming pattern across all projects

### Namespace/Module Patterns
- **Root Namespace**: Matches project name (e.g., `S7Tools`, `S7Tools.Core`)
- **Folder Mapping**: Namespaces directly map to folder structure
- **Using Statements**: Organized with System namespaces first
- **Implicit Usings**: Enabled for common namespaces

### Organizational Patterns
- **Feature Co-location**: Related files grouped by functional area
- **Separation of Concerns**: Clear boundaries between layers and responsibilities
- **Interface Segregation**: Interfaces separated from implementations
- **Dependency Direction**: Dependencies flow toward core domain

---

## 6. Navigation and Development Workflow

### Entry Points
- **Application Entry**: `Program.cs` - application startup and configuration
- **Main Window**: `MainWindow.axaml` - primary UI entry point
- **Solution File**: `S7Tools.sln` - Visual Studio/Rider entry point
- **Configuration**: `.editorconfig` - code style and quality rules

### Common Development Tasks

#### Adding New Features
1. **Define Interface**: Add service interface to `S7Tools.Core/Services/Interfaces/`
2. **Implement Service**: Create implementation in `S7Tools/Services/`
3. **Register Service**: Add to DI container in `ServiceCollectionExtensions.cs`
4. **Create ViewModel**: Add ViewModel to `S7Tools/ViewModels/`
5. **Create View**: Add XAML view to `S7Tools/Views/`
6. **Wire Navigation**: Update navigation logic in appropriate ViewModels

#### Extending Existing Functionality
- **Service Extension**: Extend existing service interfaces and implementations
- **ViewModel Enhancement**: Add properties and commands to existing ViewModels
- **View Updates**: Modify XAML and code-behind as needed
- **Converter Addition**: Add new value converters to `Converters/` folder

#### Adding New Tests
- **Test Project**: Create `{ProjectName}.Tests` project in `tests/` folder
- **Test Structure**: Mirror source project folder structure
- **Test Categories**: Separate unit, integration, and UI tests
- **Test Utilities**: Common test utilities in shared test project

#### Configuration Modifications
- **Project Settings**: Modify `.csproj` files for build configuration
- **Code Style**: Update `.editorconfig` for style rules
- **Dependencies**: Add NuGet packages through project files
- **Resources**: Add localization strings to resource files

### Dependency Patterns
- **Inward Dependencies**: All dependencies flow toward S7Tools.Core
- **Service Registration**: All services registered in `ServiceCollectionExtensions.cs`
- **Interface Usage**: Services depend on interfaces, not implementations
- **Circular Dependencies**: Avoided through proper layering

### Content Statistics
- **Total Projects**: 3 (.NET projects)
- **Main Application Files**: ~50+ C# files, ~20+ XAML files
- **Service Interfaces**: 8+ service contracts
- **ViewModels**: 10+ ViewModels for different UI areas
- **Views**: 15+ XAML views with code-behind

---

## 7. Build and Output Organization

### Build Configuration
- **Solution Build**: `dotnet build` from solution root
- **Project Files**: Modern SDK-style `.csproj` files
- **Target Framework**: .NET 8.0 for all projects
- **Build Properties**: Nullable reference types, implicit usings enabled

### Output Structure
- **Debug Output**: `bin/Debug/net8.0/` in each project
- **Release Output**: `bin/Release/net8.0/` in each project
- **Intermediate Files**: `obj/` folders for build artifacts
- **Published Output**: Configurable through publish profiles

### Environment-Specific Builds
- **Development**: Debug configuration with diagnostics enabled
- **Production**: Release configuration with optimizations
- **Platform Targets**: Windows primary, cross-platform capable
- **Deployment**: Self-contained or framework-dependent options

---

## 8. .NET-Specific Structure Patterns

### Project File Organization
- **SDK Style**: Modern `<Project Sdk="Microsoft.NET.Sdk">` format
- **Property Groups**: Organized by build configuration and metadata
- **Package References**: NuGet packages with explicit version management
- **Project References**: Clear dependency relationships

### Assembly Organization
- **Single Assembly**: Each project produces one assembly
- **Assembly Names**: Match project names exactly
- **Strong Naming**: Not currently used (standard for applications)
- **Assembly Metadata**: Generated from project properties

### Resource Organization
- **Embedded Resources**: Assets included as embedded resources
- **Localization**: Resource files with designer-generated code
- **Static Assets**: Application icons and images in Assets folder
- **XAML Resources**: Styles and templates in dedicated files

### Package Management
- **NuGet Packages**: Managed through project files
- **Version Management**: Explicit version specifications
- **Package Sources**: Standard NuGet.org and Microsoft feeds
- **Dependency Resolution**: Automatic through NuGet restore

---

## 9. Extension and Evolution

### Extension Points
- **Service Layer**: Add new services by implementing interfaces
- **UI Components**: Extend through new Views and ViewModels
- **Converters**: Add new value converters for data binding
- **Logging**: Extend logging through custom providers

### Scalability Patterns
- **Modular Services**: Services can be easily extended or replaced
- **Plugin Architecture**: Potential for plugin-based extensions
- **Configuration-Driven**: Behavior controlled through configuration
- **Dependency Injection**: Easy to swap implementations

### Refactoring Patterns
- **Interface Extraction**: Extract interfaces for better testability
- **Service Decomposition**: Break large services into smaller ones
- **Layer Separation**: Move logic to appropriate architectural layers
- **Code Organization**: Reorganize by feature or technical concern

---

## 10. Structure Templates

### New Feature Template
```
S7Tools/
├── Services/
│   ├── Interfaces/
│   │   └── I{FeatureName}Service.cs
│   └── {FeatureName}Service.cs
├── ViewModels/
│   └── {FeatureName}ViewModel.cs
├── Views/
│   ├── {FeatureName}View.axaml
│   └── {FeatureName}View.axaml.cs
└── Models/
    └── {FeatureName}Model.cs
```

### New Service Template
```
S7Tools.Core/Services/Interfaces/I{ServiceName}.cs
S7Tools/Services/{ServiceName}.cs
S7Tools/Extensions/ServiceCollectionExtensions.cs (registration)
```

### New Component Template
```
S7Tools/Views/{ComponentName}.axaml
S7Tools/Views/{ComponentName}.axaml.cs
S7Tools/ViewModels/{ComponentName}ViewModel.cs
S7Tools/Converters/{ComponentName}Converter.cs (if needed)
```

### New Test Structure Template
```
tests/
├── S7Tools.Tests/
│   ├── Services/
│   ├── ViewModels/
│   └── Converters/
├── S7Tools.Core.Tests/
│   └── Models/
└── S7Tools.Infrastructure.Logging.Tests/
    └── Core/
```

---

## 11. Structure Enforcement

### Structure Validation
- **EditorConfig**: Enforces code style and formatting rules
- **Build Warnings**: Treat style violations as warnings
- **Static Analysis**: Comprehensive CA rules for code quality
- **Dependency Rules**: Architectural constraints through project references

### Documentation Practices
- **XML Documentation**: Required for all public APIs
- **README Files**: Project-level documentation
- **Code Comments**: Inline documentation for complex logic
- **Architecture Decisions**: Documented in tracking files

### Structure Evolution History
- **Version Control**: Git tracks all structural changes
- **Change Tracking**: AI-assisted development tracking in `.copilot-tracking/`
- **Status Reports**: Regular status updates and reviews
- **Migration Guides**: Documentation for structural changes

---

## Maintenance Guidelines

### Blueprint Updates
- **Regular Reviews**: Review structure quarterly or after major changes
- **Change Documentation**: Document all structural modifications
- **Team Communication**: Share structural changes with development team
- **Tool Updates**: Update development tools and configurations as needed

### Quality Assurance
- **Structure Compliance**: Regular checks against this blueprint
- **Code Reviews**: Include structural considerations in reviews
- **Automated Checks**: Use tools to enforce structural rules
- **Documentation Sync**: Keep documentation in sync with actual structure

---

**Blueprint Version**: 1.0  
**Last Updated**: Current Session  
**Next Review**: After next major structural change  
**Maintained By**: Development Team with AI Assistance