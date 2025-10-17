# Project Brief: S7Tools

**Created:** 2025-10-14
**Project Type:** Cross-Platform Desktop Application
**Architecture:** Clean Architecture + MVVM Pattern

## Core Mission

Build a professional-grade desktop application for **Siemens S7-1200 PLC memory analysis** with **automated job management** and **VSCode-like interface**. The application provides systematic memory dumping capabilities through bootloader access, unified profile management, and intelligent resource coordination for parallel operations.

## Primary Goals

### 1. Automated Memory Dumping System
- **Job Management** - Create, schedule, and execute complex memory dump operations
- **Task Coordination** - Parallel execution with intelligent resource management
- **Bootloader Integration** - Systematic S7-1200 bootloader access and memory extraction
- **Progress Monitoring** - Real-time operation tracking with detailed logging

### 2. Unified Profile Management System
- **Standardized CRUD operations** across all profile types (Serial, Socat, PowerSupply, Memory Regions)
- **IProfileManager<T> interface** providing consistent behavior
- **Dialog-based editing** with validation and business rule enforcement
- **Export/Import capabilities** with conflict resolution

### 3. Professional Desktop Experience
- **VSCode-inspired UI** with activity bars, sidebars, and collapsible groups
- **Task Manager Activity** - Job creation, scheduling, monitoring, and management
- **Jobs Activity** - Job configuration, templates, and batch operations
- **Cross-platform support** (Windows, Linux, macOS) via Avalonia UI
- **Real-time logging system** with filtering, search, and export

### 4. Industrial-Grade Architecture
- **Clean Architecture** with clear layer separation
- **Resource Coordination** - Prevent conflicts and optimize hardware utilization
- **Dependency Injection** throughout the application
- **Reactive MVVM** using ReactiveUI patterns
- **Comprehensive testing** with growing test suite

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
1. **Job Management**: Create, edit, delete, schedule jobs with complete validation and template support
2. **Task Execution**: Automated bootloader operations with memory dumping capabilities
3. **Resource Coordination**: Parallel execution when possible, intelligent queuing for conflicts
4. **Profile Management**: Unified CRUD operations for all profile types
5. **Real-time Monitoring**: Progress tracking, operation logging, and status reporting
6. **Cross-platform Deployment**: Single codebase running on all major platforms

### Technical Requirements
1. **Clean Compilation**: Zero errors with minimal warnings
2. **Test Coverage**: Comprehensive unit tests with 100% pass rate
3. **Performance**: Responsive UI with proper async/await patterns
4. **Maintainability**: SOLID principles with clear separation of concerns

## Current Status

**Phase:** Foundation Architecture Complete - Preparing Core Functionality Implementation
**Build Status:** ✅ Clean compilation (0 errors)
**Test Status:** ✅ 206 tests passing (100% success rate)
**Architecture:** ✅ Unified IProfileManager<T> interface fully implemented

### Recent Achievements
- ✅ **Unified Profile Architecture**: IProfileManager<T> interface with StandardProfileManager<T> base class
- ✅ **Complete Service Migration**: All profile services using unified interface
- ✅ **Custom Exception Implementation**: Domain-specific exceptions across all services
- ✅ **Code Quality Improvements**: Zero errors, zero warnings build status
- ✅ **Profile Management UI**: Create/Edit auto-refresh with dialog close fixes

### Active Work Preparation
- **TASK017**: Task Manager and Jobs Implementation (Pending)
- **Core Functionality**: Bootloader integration and memory dumping operations
- **UI Enhancement**: VSCode-style activity bar with Task Manager and Jobs activities
- **Resource Coordination**: Parallel execution engine with conflict resolution

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

### Phase 1: Foundation Architecture (COMPLETE)
- ✅ Unified Profile Management Architecture
- ✅ Clean Architecture Implementation
- ✅ Build System and Testing Framework
- ✅ Custom Exception Implementation

### Phase 2: Core Functionality Implementation (CURRENT)
- 🔄 Task Manager and Jobs System (TASK017)
- 🔄 Bootloader Integration Enhancement
- 🔄 Resource Coordination Engine
- ⏳ Memory Dumping Operations

### Phase 3: Advanced Features (PLANNED)
- ⏳ Parallel Execution Optimization
- ⏳ Job Templates and Batch Operations
- ⏳ Advanced Progress Monitoring
- ⏳ Export/Import Enhancements

### Phase 4: Production Readiness (PLANNED)
- ⏳ Performance Optimization
- ⏳ Security Hardening
- ⏳ Comprehensive Documentation
- ⏳ Deployment Automation

This project brief serves as the foundation for all memory bank documentation and guides architectural decisions throughout development.
