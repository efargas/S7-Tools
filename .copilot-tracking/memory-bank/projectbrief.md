# Project Brief: S7Tools

**Created:** 2025-10-14
**Project Type:** Cross-Platform Desktop Application
**Architecture:** Clean Architecture + MVVM Pattern

## Core Mission

Build a professional-grade desktop application for **Siemens S7-1200 PLC communication** with **VSCode-like interface** and **advanced logging capabilities**. The application provides unified profile management for Serial Ports, Socat proxies, and Power Supply configurations.

## Primary Goals

### 1. Unified Profile Management System
- **Standardized CRUD operations** across all profile types (Serial, Socat, PowerSupply)
- **IProfileManager<T> interface** providing consistent behavior
- **Dialog-based editing** with validation and business rule enforcement
- **Export/Import capabilities** with conflict resolution

### 2. Professional Desktop Experience
- **VSCode-inspired UI** with activity bars, sidebars, and panels
- **Cross-platform support** (Windows, Linux, macOS) via Avalonia UI
- **Real-time logging system** with filtering, search, and export
- **Responsive design** with proper thread safety

### 3. Industrial-Grade Architecture
- **Clean Architecture** with clear layer separation
- **Dependency Injection** throughout the application
- **Reactive MVVM** using ReactiveUI patterns
- **Comprehensive testing** with 178+ passing tests

## Technology Foundation

### Core Stack
- **.NET 8.0** - Latest C# language features and performance
- **Avalonia UI 11.3.6** - Cross-platform XAML framework
- **ReactiveUI 20.1.1** - Reactive MVVM implementation
- **Microsoft.Extensions.*** - Logging, DI, and hosting

### Architecture Layers
- **S7Tools.Core** - Domain models and service interfaces (dependency-free)
- **S7Tools.Infrastructure.*** - External concerns (logging, persistence)
- **S7Tools** - Application layer (UI, ViewModels, Services)
- **S7Tools.Diagnostics** - Development and diagnostic tools

## Success Criteria

### Functional Requirements
1. **Profile Management**: Create, edit, delete, duplicate profiles with full validation
2. **Configuration Export**: JSON-based export/import with metadata preservation
3. **Real-time Logging**: Circular buffer logging with live filtering and search
4. **Cross-platform Deployment**: Single codebase running on all major platforms

### Technical Requirements
1. **Clean Compilation**: Zero errors with minimal warnings
2. **Test Coverage**: Comprehensive unit tests with 100% pass rate
3. **Performance**: Responsive UI with proper async/await patterns
4. **Maintainability**: SOLID principles with clear separation of concerns

## Current Status

**Phase:** Major Architecture Implementation Complete
**Build Status:** ✅ Clean compilation (0 errors)
**Test Status:** ✅ 178 tests passing (100% success rate)
**Architecture:** ✅ Unified IProfileManager<T> interface fully implemented

### Recent Achievements
- ✅ **Unified Profile Architecture**: IProfileManager<T> interface with StandardProfileManager<T> base class
- ✅ **Complete Service Migration**: All three profile services using unified interface
- ✅ **ViewModels Integration**: All UI components updated to use standardized methods
- ✅ **Build Verification**: Clean compilation with zero compilation errors achieved

### Active Work
- **TASK008 Phase 1**: Architecture + ViewModels Integration **COMPLETE**
- **Dependency Injection Updates**: Service registration optimization
- **ProfileManagementViewModelBase Integration**: Template method pattern verification
- **Build Verification and Testing**: Comprehensive functionality validation

## Key Constraints

### Technical Constraints
- **Cross-platform Compatibility**: Must work identically on Windows, Linux, macOS
- **Memory Efficiency**: Circular buffer logging to prevent memory leaks
- **Thread Safety**: All UI operations properly marshaled to UI thread
- **Reactive Patterns**: Individual property subscriptions to avoid ReactiveUI limitations

### Business Constraints
- **Profile Name Uniqueness**: Within each profile type
- **ID Assignment**: Gap-filling algorithm for efficient ID usage
- **Default Profile Management**: Only one default per profile type
- **Read-only Protection**: System defaults cannot be modified or deleted

## Future Roadmap

### Phase 1: Foundation (COMPLETE)
- ✅ Unified Profile Management Architecture
- ✅ Clean Architecture Implementation
- ✅ Build System and Testing Framework

### Phase 2: Integration (IN PROGRESS)
- 🔄 Dependency Injection Optimization
- 🔄 Template Method Pattern Integration
- ⏳ UI Standardization and Enhancement

### Phase 3: Advanced Features (PLANNED)
- ⏳ PLC Communication Protocol Integration
- ⏳ Advanced Configuration Management
- ⏳ Plugin Architecture Implementation

This project brief serves as the foundation for all memory bank documentation and guides architectural decisions throughout development.
