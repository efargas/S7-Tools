---
title: "Templates Catalog"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - index
  - templates
  - catalog
related:
  - docs/guides/development-workflow.md
  - docs/patterns/_index.md
---

# Templates Catalog

Comprehensive catalog of all templates available in the S7Tools documentation system.

## Code Templates

### ViewModel Template
**File**: `viewmodel-template.md`
**Purpose**: Create ReactiveUI ViewModels with proper MVVM patterns
**Use When**: Adding new feature ViewModels
**Related**: [MVVM Patterns](../architecture/mvvm-patterns.md)

### Service Template
**File**: `service-template.md`
**Purpose**: Create service classes with dependency injection
**Use When**: Adding new service layer components
**Related**: [System Patterns](../patterns/system-patterns.md)

### Test Template
**File**: `test-template.md`
**Purpose**: Create xUnit tests following AAA pattern
**Use When**: Writing unit tests for new features
**Related**: [Testing Guide](../guides/testing-guide.md)

## Documentation Templates

### Pattern Template
**File**: `pattern-template.md`
**Purpose**: Document implementation patterns
**Use When**: Creating new pattern documentation
**Related**: [Patterns Index](../patterns/_index.md)

### Guide Template
**File**: `guide-template.md`
**Purpose**: Create developer guides and workflows
**Use When**: Writing new development guides
**Related**: [Guides Index](../guides/_index.md)

### ADR Template
**File**: `adr-template.md`
**Purpose**: Document architecture decisions
**Use When**: Recording significant architectural choices
**Related**: [ADR Index](../architecture/decisions/_index.md)

## UI Integration Templates

**Location**: `ui-integration/`
**Overview**: Complete feature scaffold with ViewModel, Views, and integration checklist
**Use When**: Adding new UI features following sidebar-main-content pattern

### Available Templates:
- `FeatureViewModel.template.cs` - Main ViewModel with sidebar integration
- `FeatureSidebarView.template.axaml` - Sidebar panel XAML
- `FeatureMainView.template.axaml` - Main content area XAML
- `INTEGRATION_CHECKLIST.md` - Step-by-step implementation checklist
- `README.md` - Detailed usage instructions

**Related**: [UI Integration Workflow](../UI_INTEGRATION_WORKFLOW.md)

## Usage Guidelines

### Choosing the Right Template

1. **New Feature UI**: Start with UI Integration templates
2. **Business Logic**: Use Service Template
3. **UI Logic**: Use ViewModel Template
4. **Testing**: Use Test Template
5. **Documentation**: Use Pattern/Guide/ADR templates

### Template Workflow

1. **Copy** template to appropriate location
2. **Replace** placeholders (`[FEATURE_NAME]`, `[CATEGORY]`, etc.)
3. **Customize** for specific requirements
4. **Register** services in `ServiceCollectionExtensions.cs` if applicable
5. **Test** implementation thoroughly
6. **Document** any deviations from template

### Placeholder Conventions

- `[FEATURE_NAME]` - Feature name in PascalCase (e.g., `Reports`, `Analytics`)
- `[CATEGORY]` - ViewModel/View category (e.g., `Pages`, `Dialogs`)
- `[SERVICE_NAME]` - Service name (e.g., `UserProfile`, `DataExport`)
- `NNNN` - Sequential number (e.g., ADR-0001)
- `YYYY-MM-DD` - ISO date format

## Template Maintenance

### Version Control
All templates are version-controlled with frontmatter metadata tracking:
- Created date
- Last updated date
- Semantic version
- Status (current/deprecated)

### Updates
When updating templates:
1. Update `last-updated` date
2. Increment version following semver
3. Document changes in version history section
4. Update related cross-references

### Deprecation
When deprecating templates:
1. Set status to `deprecated`
2. Add `superseded-by` field
3. Provide migration guide
4. Maintain 2-year retention period

## Contributing

Found a template improvement? See [Development Workflow](../guides/development-workflow.md) for contribution guidelines.

---

*Last updated: 2025-11-10 | Version: 1.0.0*

## Related Documentation

- [Ui_Integration_Workflow](../UI_INTEGRATION_WORKFLOW.md)
- [_Index](../architecture/decisions/_index.md)
- [Mvvm Patterns](../architecture/mvvm-patterns.md)
- [_Index](../guides/_index.md)
- [Code Style](../guides/code-style.md)
- [Contributing To Docs](../guides/contributing-to-docs.md)
- [Development Workflow](../guides/development-workflow.md)
- [Testing Guide](../guides/testing-guide.md)
- [_Index](../patterns/_index.md)
- [System Patterns](../patterns/system-patterns.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
