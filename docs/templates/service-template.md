---
title: "Service Template"
created: "2025-11-10"
last-updated: "2025-11-11"
version: "1.0.0"
status: "current"
tags:
  - template
  - code
  - service
  - dependency-injection
related:
  - docs/patterns/system-patterns.md
  - docs/architecture/clean-architecture.md
  - docs/guides/development-workflow.md
---

# Service Template

Template for creating service classes with dependency injection.

## Usage

1. Copy code below to `src/S7Tools/Services/YourService.cs`
2. Replace `[SERVICE_NAME]` with your service name
3. Add interface to `src/S7Tools.Core/Interfaces/I[SERVICE_NAME].cs`
4. Register in `ServiceCollectionExtensions.cs`

## Template Code

```csharp
using System;
using System.Threading.Tasks;
using S7Tools.Core.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for [SERVICE_NAME] operations.
/// </summary>
public class [SERVICE_NAME]Service : I[SERVICE_NAME]Service
{
    private readonly ILoggingService _loggingService;

    public [SERVICE_NAME]Service(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    public async Task<bool> ExecuteAsync()
    {
        try
        {
            _loggingService.LogInformation("[SERVICE_NAME]: Starting operation");

            // TODO: Implement service logic

            _loggingService.LogInformation("[SERVICE_NAME]: Operation completed");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "[SERVICE_NAME]: Operation failed");
            throw;
        }
    }
}
```

## Registration

Add to `ServiceCollectionExtensions.cs`:

```csharp
services.TryAddSingleton<I[SERVICE_NAME]Service, [SERVICE_NAME]Service>();
```

## See Also

- ViewModel Template - `viewmodel-template.md`
- Test Template - `test-template.md`

## Related Documentation

- [Clean Architecture](../architecture/clean-architecture.md)
- [Development Workflow](../guides/development-workflow.md)
- [Profile Manager Example](../patterns/examples/profile-manager-example.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
