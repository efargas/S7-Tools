# Technical Context: S7Tools

**Last Updated**: Current Session  
**Context Type**: Technologies, Tools, and Development Environment  

## Technology Stack

### **Core Technologies**

#### **.NET 8.0**
- **Version**: 8.0 (Latest LTS)
- **Language**: C# with latest language features
- **Features Used**:
  - Nullable reference types enabled
  - Implicit usings enabled
  - Primary constructors
  - Pattern matching enhancements
  - Record types for data models

#### **Avalonia UI 11.3.6**
- **Purpose**: Cross-platform desktop UI framework
- **Advantages**: 
  - Cross-platform (Windows, Linux, macOS)
  - XAML-based UI similar to WPF
  - Modern styling and theming support
  - Good performance and memory usage
- **Key Features Used**:
  - Data binding and MVVM support
  - Custom controls and styling
  - Reactive UI integration
  - Designer support

#### **ReactiveUI 20.1.1**
- **Purpose**: Reactive MVVM framework
- **Integration**: Primary MVVM implementation
- **Key Features**:
  - Reactive property changes
  - Command handling with ReactiveCommand
  - Interaction patterns for View-ViewModel communication
  - Automatic UI thread marshalling

### **Supporting Libraries**

#### **Microsoft.Extensions.DependencyInjection 8.0.0**
- **Purpose**: Dependency injection container
- **Usage**: Primary IoC container for all services
- **Integration**: Configured in Program.cs with service extensions

#### **Microsoft.Extensions.Logging 8.0.0**
- **Purpose**: Logging abstraction and infrastructure
- **Custom Integration**: DataStore provider for UI logging
- **Providers**: Console, Debug, and custom DataStore

#### **CommunityToolkit.Mvvm 8.2.0**
- **Purpose**: Additional MVVM helpers and source generators
- **Usage**: Complementary to ReactiveUI for specific scenarios

#### **Projektanker.Icons.Avalonia.FontAwesome 9.6.2**
- **Purpose**: FontAwesome icon integration
- **Usage**: Icons throughout the UI (activity bar, menus, buttons)

#### **FluentAvaloniaUI 2.4.0**
- **Purpose**: Fluent Design System components
- **Usage**: Modern UI components and styling

### **Development Tools**

#### **Primary IDEs**
- **Visual Studio 2022** (Recommended)
- **JetBrains Rider** (Alternative)
- **VS Code** with C# extension (Lightweight option)

#### **Build and Package Management**
- **dotnet CLI** - Primary build tool
- **NuGet** - Package management
- **MSBuild** - Build system (SDK-style projects)

#### **Code Quality Tools**
- **EditorConfig** - Code style enforcement
- **Roslyn Analyzers** - Static code analysis
- **XML Documentation** - API documentation generation

## Development Setup

### **Prerequisites**

#### **Required Software**
- **.NET 8.0 SDK** or later
- **Git** for version control
- **IDE** (Visual Studio 2022, Rider, or VS Code)

#### **Optional Tools**
- **Avalonia for Visual Studio** extension
- **GitKraken** or similar Git GUI
- **Postman** for API testing (future PLC communication)

### **Environment Configuration**

#### **Project Setup Commands**
```bash
# Clone repository
git clone <repository-url>
cd S7Tools

# Restore dependencies
dotnet restore src/S7Tools.sln

# Build solution
dotnet build src/S7Tools.sln

# Run application
dotnet run --project src/S7Tools/S7Tools.csproj
```

#### **Development Workflow**
```bash
# Navigate to source directory
cd src

# Run with hot reload (if supported)
dotnet watch run --project S7Tools/S7Tools.csproj

# Build specific configuration
dotnet build --configuration Release

# Run tests (when implemented)
dotnet test
```

### **Code Style Configuration**

#### **EditorConfig Settings**
- **C# Files**: 4-space indentation, PascalCase naming
- **XAML Files**: 2-space indentation, PascalCase elements
- **JSON Files**: 2-space indentation
- **Comprehensive Rules**: 200+ style and quality rules defined

#### **Naming Conventions**
- **Interfaces**: `I` prefix (e.g., `IActivityBarService`)
- **Private Fields**: Underscore prefix (e.g., `_fieldName`)
- **Services**: `{Feature}Service` pattern
- **ViewModels**: `{View}ViewModel` pattern
- **Views**: `{Feature}View.axaml` pattern

## Technical Constraints

### **Platform Requirements**

#### **Target Platforms**
- **Primary**: Windows 10/11 (x64)
- **Secondary**: Linux (Ubuntu 20.04+, x64)
- **Tertiary**: macOS (10.15+, x64/ARM64)

#### **Runtime Requirements**
- **.NET 8.0 Runtime** (Desktop)
- **Minimum RAM**: 512MB
- **Minimum Storage**: 100MB
- **Display**: 1024x768 minimum resolution

### **Performance Constraints**

#### **Response Time Requirements**
- **Application Startup**: < 3 seconds
- **UI Operations**: < 100ms response time
- **PLC Communication**: < 500ms for standard operations
- **Log Display**: Handle 10,000+ entries efficiently

#### **Memory Constraints**
- **Base Memory Usage**: < 100MB at startup
- **Maximum Memory**: < 500MB during normal operation
- **Log Buffer**: Circular buffer with configurable size (default 10,000 entries)
- **Memory Leaks**: Zero tolerance for memory leaks

### **Security Constraints**

#### **Data Security**
- **PLC Communication**: Secure connection protocols where available
- **Configuration Storage**: Encrypted sensitive configuration data
- **Logging**: No sensitive data in log files
- **Network**: Firewall-friendly communication patterns

#### **Code Security**
- **Input Validation**: All user inputs validated
- **Exception Handling**: No sensitive information in error messages
- **Dependencies**: Regular security updates for all packages
- **Static Analysis**: Security-focused code analysis rules

## Dependencies

### **Direct Dependencies (NuGet Packages)**

#### **S7Tools Project**
```xml
<PackageReference Include="Avalonia" Version="11.3.6" />
<PackageReference Include="Avalonia.Desktop" Version="11.3.6" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.3.6" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.6" />
<PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.6" />
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.6" />
<PackageReference Include="ReactiveUI" Version="20.1.1" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
<PackageReference Include="Projektanker.Icons.Avalonia.FontAwesome" Version="9.6.2" />
<PackageReference Include="FluentAvaloniaUI" Version="2.4.0" />
```

#### **S7Tools.Infrastructure.Logging Project**
```xml
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
<PackageReference Include="System.Collections.Concurrent" Version="8.0.0" />
```

#### **S7Tools.Core Project**
- **No external dependencies** (pure domain layer)

### **Development Dependencies**

#### **Testing (Planned)**
```xml
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

#### **Code Analysis**
```xml
<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" />
<PackageReference Include="StyleCop.Analyzers" Version="1.1.118" />
```

### **Future Dependencies (Planned)**

#### **PLC Communication**
- **S7.NET** or similar S7 protocol library
- **System.IO.Ports** for serial communication
- **Network communication libraries** for Ethernet/IP

#### **Data Visualization**
- **OxyPlot.Avalonia** for charts and graphs
- **LiveCharts2** for real-time data visualization

#### **Configuration Management**
- **Microsoft.Extensions.Configuration** for settings
- **Microsoft.Extensions.Options** for strongly-typed configuration

## Build and Deployment

### **Build Configuration**

#### **Debug Configuration**
- **Optimizations**: Disabled
- **Debug Symbols**: Full
- **Avalonia Diagnostics**: Enabled
- **Logging Level**: Debug
- **Performance**: Development-optimized

#### **Release Configuration**
- **Optimizations**: Enabled
- **Debug Symbols**: Portable PDB
- **Avalonia Diagnostics**: Disabled
- **Logging Level**: Information
- **Performance**: Production-optimized

### **Deployment Options**

#### **Framework-Dependent Deployment**
```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release
```
- **Advantages**: Smaller package size
- **Requirements**: .NET 8.0 Runtime must be installed
- **Use Case**: Corporate environments with managed runtimes

#### **Self-Contained Deployment**
```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r win-x64 --self-contained
```
- **Advantages**: No runtime dependencies
- **Disadvantages**: Larger package size
- **Use Case**: Standalone installations

#### **Single-File Deployment**
```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
- **Advantages**: Single executable file
- **Use Case**: Portable installations

### **Cross-Platform Considerations**

#### **Windows-Specific**
- **Native Interop**: COM interop enabled for Windows APIs
- **File Paths**: Windows path separator handling
- **Registry Access**: Windows registry for configuration (optional)

#### **Linux-Specific**
- **Dependencies**: Additional system libraries may be required
- **File Permissions**: Proper file permission handling
- **Package Management**: Consider AppImage or Snap packaging

#### **macOS-Specific**
- **Code Signing**: Required for distribution
- **Notarization**: Required for Gatekeeper compatibility
- **Bundle Structure**: macOS app bundle format

## Development Workflow

### **Version Control**
- **Git** with feature branch workflow
- **Commit Messages**: Conventional commit format
- **Branch Protection**: Main branch protected with PR requirements

### **Code Review Process**
- **Pull Requests**: Required for all changes
- **Review Requirements**: Architecture compliance, code quality, testing
- **Automated Checks**: Build verification, code analysis, formatting

### **Continuous Integration (Planned)**
```yaml
# GitHub Actions workflow (planned)
- Build on multiple platforms
- Run automated tests
- Code quality analysis
- Security vulnerability scanning
- Automated deployment to staging
```

### **Release Process**
1. **Version Tagging**: Semantic versioning (SemVer)
2. **Release Notes**: Automated generation from commits
3. **Package Creation**: Multi-platform packages
4. **Distribution**: GitHub Releases or package managers

---

**Document Status**: Living document reflecting current technical state  
**Next Review**: After major technology updates  
**Owner**: Development Team and DevOps