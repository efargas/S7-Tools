# Project Brief: S7Tools

**Project Name**: S7Tools  
**Version**: Development  
**Created**: Current Session  
**Last Updated**: Current Session  

## Project Overview

**S7Tools** is a cross-platform desktop application for **Siemens S7-1200 PLC communication**, built with .NET 8.0 and Avalonia UI. The application features a **VSCode-like interface** with advanced logging capabilities, implementing **Clean Architecture** with **MVVM pattern**.

## Core Purpose

**Primary Goal**: Provide professional tools for Siemens S7-1200 PLC communication with a modern, intuitive user interface.

**Key Objectives**:
- Enable efficient PLC data monitoring and management
- Provide real-time logging and debugging capabilities
- Deliver a professional VSCode-like user experience
- Maintain cross-platform compatibility (Windows, Linux, macOS)
- Implement enterprise-grade architecture and code quality

## Technical Foundation

### **Architecture**
- **Pattern**: Clean Architecture with MVVM
- **UI Framework**: Avalonia UI 11.3.6 with ReactiveUI 20.1.1
- **Platform**: .NET 8.0 with latest C# language features
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging with custom DataStore provider

### **Project Structure**
- **S7Tools** - Main Avalonia application (UI, ViewModels, Services)
- **S7Tools.Core** - Domain models and service interfaces (dependency-free)
- **S7Tools.Infrastructure.Logging** - Logging infrastructure with custom providers

### **Key Design Principles**
1. **Clean Architecture** - Dependencies flow inward toward core domain
2. **Service-Oriented Design** - Comprehensive DI registration and service layer
3. **MVVM Compliance** - Strict separation between Views, ViewModels, and Services
4. **Cross-Platform Support** - Avalonia UI enables Windows, Linux, and macOS support
5. **Performance First** - Circular buffer logging, UI virtualization, memory optimization

## Current Status

### **Completed Features** ✅
- **VSCode-like UI** - Complete with activity bar, sidebar, bottom panel, and menu system
- **Advanced Logging System** - Real-time log display with filtering, search, and export
- **Foundation Infrastructure** - Comprehensive service layer with DI registration
- **Clean Architecture Implementation** - Multi-project solution with proper layer separation
- **MVVM Pattern** - ReactiveUI implementation with proper data binding

### **In Development** 🔄
- **PLC Communication Protocol** - S7-1200 integration and data exchange
- **Configuration Management** - Application settings and user preferences
- **Testing Framework** - Unit and integration test implementation

### **Planned Features** 📋
- **Data Visualization** - Charts and graphs for PLC data monitoring
- **Export/Import Functionality** - Data export and configuration management
- **Plugin Architecture** - Extensibility for custom functionality
- **Multi-language Support** - Internationalization and localization

## Success Criteria

### **Technical Excellence**
- Clean, maintainable code following .NET best practices
- Comprehensive XML documentation for all public APIs
- >80% unit test coverage for critical components
- Cross-platform compatibility maintained
- Performance requirements met (UI responsiveness, memory usage)

### **User Experience**
- Professional VSCode-like interface with intuitive navigation
- Real-time data monitoring and logging capabilities
- Responsive UI with smooth animations and transitions
- Comprehensive error handling and user feedback

### **Architecture Quality**
- Clean Architecture principles properly implemented
- Service-oriented design with proper separation of concerns
- Dependency injection used throughout the application
- No circular dependencies or architectural violations

## Constraints and Requirements

### **Technical Constraints**
- Must target .NET 8.0 for modern language features
- Cross-platform compatibility required (Windows primary)
- Memory usage must remain reasonable with large datasets
- UI must remain responsive during PLC communication

### **Quality Requirements**
- All code must follow EditorConfig style guidelines
- Comprehensive error handling and logging required
- XML documentation required for all public APIs
- No breaking changes to existing functionality during development

### **Performance Requirements**
- Application startup time < 3 seconds
- UI operations must complete within 100ms
- Log viewer must handle 10,000+ entries efficiently
- Memory usage must remain stable during extended operation

## Key Stakeholders

### **Primary Users**
- Industrial automation engineers
- PLC programmers and technicians
- System integrators and maintenance personnel

### **Development Team**
- Software engineers with .NET and Avalonia expertise
- UI/UX designers familiar with VSCode design patterns
- Quality assurance engineers for testing and validation

## Project Scope

### **In Scope**
- S7-1200 PLC communication and data exchange
- Real-time logging and debugging capabilities
- Professional VSCode-like user interface
- Cross-platform desktop application support
- Configuration management and user preferences

### **Out of Scope**
- Web-based interface or mobile applications
- Support for other PLC brands or models (initially)
- Cloud-based data storage or synchronization
- Advanced data analytics or machine learning features

## Risk Assessment

### **Technical Risks**
- **PLC Communication Complexity** - S7 protocol implementation challenges
- **Cross-Platform Compatibility** - Platform-specific issues with Avalonia
- **Performance Requirements** - UI responsiveness with large datasets

### **Mitigation Strategies**
- Incremental development with comprehensive testing
- Platform-specific testing and conditional implementations
- Performance monitoring and optimization throughout development

---

**Document Status**: Living document - updated as project evolves  
**Next Review**: After major milestone completion  
**Owner**: Development Team