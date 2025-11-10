# Feature Implementation Template

**Version**: 1.0  
**Purpose**: Step-by-step guide for implementing new features in S7Tools  
**Follows**: Clean Architecture + SOLID Principles + Modern .NET Patterns  

## 📋 **FEATURE IMPLEMENTATION CHECKLIST**

### **Phase 1: Planning & Design (30 minutes)**
- [ ] Define feature requirements and acceptance criteria
- [ ] Identify domain entities and value objects needed
- [ ] Design service interfaces following ISP
- [ ] Plan data models and validation rules
- [ ] Identify integration points with existing services
- [ ] Create implementation timeline

### **Phase 2: Domain Layer (45 minutes)**
- [ ] Create/update domain entities (immutable records)
- [ ] Implement value objects with validation
- [ ] Add domain services if needed
- [ ] Update Result types for new operations
- [ ] Write unit tests for domain logic

### **Phase 3: Application Layer (60 minutes)**
- [ ] Define service interfaces
- [ ] Implement service classes with DI
- [ ] Add request/response models
- [ ] Implement validation logic
- [ ] Add comprehensive logging
- [ ] Write unit tests for services

### **Phase 4: Infrastructure Layer (45 minutes)**
- [ ] Implement repository interfaces
- [ ] Add database entities/migrations (if needed)
- [ ] Configure external service integrations
- [ ] Update dependency injection registration
- [ ] Write integration tests

### **Phase 5: Presentation Layer (60 minutes)**
- [ ] Create/update ViewModels with ReactiveUI
- [ ] Implement XAML views with proper bindings
- [ ] Add command implementations
- [ ] Handle user interactions and validation
- [ ] Update navigation if needed
- [ ] Test UI functionality

### **Phase 6: Testing & Documentation (30 minutes)**
- [ ] Run all tests and ensure >90% coverage
- [ ] Update API documentation
- [ ] Add feature documentation
- [ ] Perform manual testing
- [ ] Update changelog

## 🏗️ **STEP-BY-STEP IMPLEMENTATION GUIDE**

### **Step 1: Domain Entity Creation**

```csharp
// 1. Create immutable domain entity
namespace S7Tools.Core.Models;

/// <summary>
/// Represents a [FeatureName] with validation and business rules.
/// Immutable record following DDD principles.
/// </summary>
/// <param name="Id">Unique identifier for the [FeatureName].</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="IsActive">Whether the [FeatureName] is active.</param>
public sealed record FeatureEntity(
    string Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    bool IsActive = true)
{
    /// <summary>
    /// Creates a new FeatureEntity with validation.
    /// </summary>
    /// <param name="name">The name of the feature.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A Result containing the FeatureEntity or validation errors.</returns>
    public static Result<FeatureEntity> Create(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<FeatureEntity>.Failure("Name cannot be null or empty");

        if (name.Length > 100)
            return Result<FeatureEntity>.Failure("Name cannot exceed 100 characters");

        var id = Guid.NewGuid().ToString();
        var entity = new FeatureEntity(
            id,
            name.Trim(),
            description.Trim(),
            DateTimeOffset.UtcNow);

        return Result<FeatureEntity>.Success(entity);
    }

    /// <summary>
    /// Creates a new FeatureEntity with updated name.
    /// </summary>
    /// <param name="newName">The new name.</param>
    /// <returns>A new FeatureEntity with the updated name.</returns>
    public FeatureEntity WithName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be null or empty", nameof(newName));

        return this with { Name = newName.Trim() };
    }

    /// <summary>
    /// Creates a new FeatureEntity with updated active state.
    /// </summary>
    /// <param name="isActive">The new active state.</param>
    /// <returns>A new FeatureEntity with the updated state.</returns>
    public FeatureEntity WithActiveState(bool isActive) => this with { IsActive = isActive };
}
```

### **Step 2: Service Interface Definition**

```csharp
// 2. Define service interface
namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines operations for managing [FeatureName] entities.
/// Follows the Repository pattern with Result-based error handling.
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// Retrieves a feature by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the FeatureEntity or an error.</returns>
    Task<Result<FeatureEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active features.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the collection of active features.</returns>
    Task<Result<IReadOnlyCollection<FeatureEntity>>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new feature.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the created FeatureEntity or an error.</returns>
    Task<Result<FeatureEntity>> CreateAsync(CreateFeatureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing feature.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> UpdateAsync(UpdateFeatureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a feature by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
```

### **Step 3: Request/Response Models**

```csharp
// 3. Create request/response models
namespace S7Tools.Core.Models.Requests;

/// <summary>
/// Request model for creating a new feature.
/// </summary>
public sealed record CreateFeatureRequest
{
    /// <summary>
    /// Gets the name of the feature.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    [StringLength(500)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the feature should be active initially.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Request model for updating an existing feature.
/// </summary>
public sealed record UpdateFeatureRequest
{
    /// <summary>
    /// Gets the unique identifier of the feature to update.
    /// </summary>
    [Required]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the new name for the feature.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the new description.
    /// </summary>
    [StringLength(500)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new active state.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
```

### **Step 4: Service Implementation**

```csharp
// 4. Implement the service
namespace S7Tools.Services;

/// <summary>
/// Implementation of feature management service with comprehensive error handling.
/// </summary>
public sealed class FeatureService : IFeatureService, IDisposable
{
    private readonly IFeatureRepository _repository;
    private readonly IValidator<CreateFeatureRequest> _createValidator;
    private readonly IValidator<UpdateFeatureRequest> _updateValidator;
    private readonly ILogger<FeatureService> _logger;
    private bool _disposed;

    public FeatureService(
        IFeatureRepository repository,
        IValidator<CreateFeatureRequest> createValidator,
        IValidator<UpdateFeatureRequest> updateValidator,
        ILogger<FeatureService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<FeatureEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving feature with ID: {FeatureId}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                const string error = "Feature ID cannot be null or empty";
                _logger.LogWarning(error);
                return Result<FeatureEntity>.Failure(error);
            }

            var result = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully retrieved feature: {FeatureName}", result.Value.Name);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve feature {FeatureId}: {Error}", id, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature with ID: {FeatureId}", id);
            return Result<FeatureEntity>.Failure($"Failed to retrieve feature: {ex.Message}", ex);
        }
    }

    public async Task<Result<FeatureEntity>> CreateAsync(CreateFeatureRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating feature: {@CreateRequest}", request);

            // Validate the request
            var validationResult = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Feature creation validation failed: {Errors}", errors);
                return Result<FeatureEntity>.Failure($"Validation failed: {errors}");
            }

            // Create the entity
            var entityResult = FeatureEntity.Create(request.Name, request.Description);
            if (entityResult.IsFailure)
            {
                _logger.LogWarning("Failed to create feature entity: {Error}", entityResult.Error);
                return entityResult;
            }

            var entity = entityResult.Value.WithActiveState(request.IsActive);

            // Persist the entity
            var saveResult = await _repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save feature: {Error}", saveResult.Error);
                return Result<FeatureEntity>.Failure(saveResult.Error);
            }

            _logger.LogInformation("Successfully created feature: {FeatureName} with ID: {FeatureId}", 
                entity.Name, entity.Id);

            return Result<FeatureEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature: {@CreateRequest}", request);
            return Result<FeatureEntity>.Failure($"Failed to create feature: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _repository?.Dispose();
            _disposed = true;
        }
    }
}
```

### **Step 5: Repository Interface & Implementation**

```csharp
// 5. Define repository interface
namespace S7Tools.Core.Repositories;

/// <summary>
/// Repository interface for FeatureEntity persistence operations.
/// </summary>
public interface IFeatureRepository : IDisposable
{
    /// <summary>
    /// Retrieves a feature by its unique identifier.
    /// </summary>
    Task<Result<FeatureEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all features matching the specified criteria.
    /// </summary>
    Task<Result<IReadOnlyCollection<FeatureEntity>>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new feature to the repository.
    /// </summary>
    Task<Result> AddAsync(FeatureEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing feature in the repository.
    /// </summary>
    Task<Result> UpdateAsync(FeatureEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a feature from the repository.
    /// </summary>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

// 6. Implement repository (example with in-memory storage)
namespace S7Tools.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IFeatureRepository for development/testing.
/// </summary>
public sealed class InMemoryFeatureRepository : IFeatureRepository
{
    private readonly ConcurrentDictionary<string, FeatureEntity> _features = new();
    private readonly ILogger<InMemoryFeatureRepository> _logger;
    private bool _disposed;

    public InMemoryFeatureRepository(ILogger<InMemoryFeatureRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<FeatureEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_features.TryGetValue(id, out var feature))
            {
                _logger.LogDebug("Found feature with ID: {FeatureId}", id);
                return Task.FromResult(Result<FeatureEntity>.Success(feature));
            }

            _logger.LogDebug("Feature not found with ID: {FeatureId}", id);
            return Task.FromResult(Result<FeatureEntity>.Failure($"Feature with ID '{id}' not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature with ID: {FeatureId}", id);
            return Task.FromResult(Result<FeatureEntity>.Failure($"Error retrieving feature: {ex.Message}", ex));
        }
    }

    public Task<Result> AddAsync(FeatureEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_features.TryAdd(entity.Id, entity))
            {
                _logger.LogDebug("Added feature with ID: {FeatureId}", entity.Id);
                return Task.FromResult(Result.Success());
            }

            _logger.LogWarning("Feature with ID {FeatureId} already exists", entity.Id);
            return Task.FromResult(Result.Failure($"Feature with ID '{entity.Id}' already exists"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feature: {@Feature}", entity);
            return Task.FromResult(Result.Failure($"Error adding feature: {ex.Message}", ex));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _features.Clear();
            _disposed = true;
        }
    }
}
```

### **Step 6: ViewModel Implementation**

```csharp
// 7. Create ViewModel with ReactiveUI
namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for managing features with ReactiveUI patterns.
/// </summary>
public sealed class FeatureManagementViewModel : ViewModelBase, IDisposable
{
    private readonly IFeatureService _featureService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<FeatureManagementViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private ObservableCollection<FeatureEntity> _features = new();
    private FeatureEntity? _selectedFeature;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public FeatureManagementViewModel(
        IFeatureService featureService,
        IDialogService dialogService,
        ILogger<FeatureManagementViewModel> logger)
    {
        _featureService = featureService ?? throw new ArgumentNullException(nameof(featureService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeCommands();
        LoadFeaturesAsync().ConfigureAwait(false);
    }

    #region Properties

    /// <summary>
    /// Gets the collection of features.
    /// </summary>
    public ObservableCollection<FeatureEntity> Features
    {
        get => _features;
        private set => this.RaiseAndSetIfChanged(ref _features, value);
    }

    /// <summary>
    /// Gets or sets the currently selected feature.
    /// </summary>
    public FeatureEntity? SelectedFeature
    {
        get => _selectedFeature;
        set => this.RaiseAndSetIfChanged(ref _selectedFeature, value);
    }

    /// <summary>
    /// Gets or sets whether the view is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to load features.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFeaturesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to create a new feature.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateFeatureCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to delete the selected feature.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteFeatureCommand { get; private set; } = null!;

    #endregion

    private void InitializeCommands()
    {
        // Load features command
        LoadFeaturesCommand = ReactiveCommand.CreateFromTask(LoadFeaturesAsync);
        LoadFeaturesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "Failed to load features"))
            .DisposeWith(_disposables);

        // Create feature command
        CreateFeatureCommand = ReactiveCommand.CreateFromTask(CreateFeatureAsync);
        CreateFeatureCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "Failed to create feature"))
            .DisposeWith(_disposables);

        // Delete feature command - only enabled when a feature is selected
        var canDelete = this.WhenAnyValue(x => x.SelectedFeature)
            .Select(feature => feature != null);

        DeleteFeatureCommand = ReactiveCommand.CreateFromTask(DeleteFeatureAsync, canDelete);
        DeleteFeatureCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "Failed to delete feature"))
            .DisposeWith(_disposables);
    }

    private async Task LoadFeaturesAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            _logger.LogDebug("Loading features");

            var result = await _featureService.GetActiveAsync();
            if (result.IsSuccess)
            {
                Features.Clear();
                foreach (var feature in result.Value)
                {
                    Features.Add(feature);
                }

                _logger.LogDebug("Loaded {Count} features", Features.Count);
            }
            else
            {
                ErrorMessage = result.Error;
                _logger.LogWarning("Failed to load features: {Error}", result.Error);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateFeatureAsync()
    {
        try
        {
            // In a real implementation, this would show a dialog to get user input
            var request = new CreateFeatureRequest
            {
                Name = $"New Feature {DateTime.Now:HH:mm:ss}",
                Description = "Created from UI",
                IsActive = true
            };

            var result = await _featureService.CreateAsync(request);
            if (result.IsSuccess)
            {
                Features.Add(result.Value);
                SelectedFeature = result.Value;
                _logger.LogInformation("Created feature: {FeatureName}", result.Value.Name);
            }
            else
            {
                ErrorMessage = result.Error;
                _logger.LogWarning("Failed to create feature: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "Failed to create feature");
        }
    }

    private async Task DeleteFeatureAsync()
    {
        if (SelectedFeature == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Feature",
                $"Are you sure you want to delete '{SelectedFeature.Name}'?");

            if (!confirmed) return;

            var result = await _featureService.DeleteAsync(SelectedFeature.Id);
            if (result.IsSuccess)
            {
                Features.Remove(SelectedFeature);
                SelectedFeature = null;
                _logger.LogInformation("Deleted feature: {FeatureName}", SelectedFeature.Name);
            }
            else
            {
                ErrorMessage = result.Error;
                _logger.LogWarning("Failed to delete feature: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "Failed to delete feature");
        }
    }

    private void HandleCommandException(Exception ex, string operation)
    {
        ErrorMessage = $"{operation}: {ex.Message}";
        _logger.LogError(ex, operation);
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
```

### **Step 7: Dependency Injection Registration**

```csharp
// 8. Register services in DI container
namespace S7Tools.Extensions;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds feature management services to the service collection.
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IFeatureRepository, InMemoryFeatureRepository>();

        // Validators
        services.AddScoped<IValidator<CreateFeatureRequest>, CreateFeatureRequestValidator>();
        services.AddScoped<IValidator<UpdateFeatureRequest>, UpdateFeatureRequestValidator>();

        // ViewModels
        services.AddTransient<FeatureManagementViewModel>();

        return services;
    }
}

// 9. Add to Program.cs
private static void ConfigureServices(IServiceCollection services)
{
    // ... existing services ...
    
    // Add feature services
    services.AddFeatureServices();
}
```

### **Step 8: Unit Tests**

```csharp
// 10. Create comprehensive unit tests
namespace S7Tools.Tests.Services;

public sealed class FeatureServiceTests : IDisposable
{
    private readonly Mock<IFeatureRepository> _mockRepository;
    private readonly Mock<IValidator<CreateFeatureRequest>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateFeatureRequest>> _mockUpdateValidator;
    private readonly Mock<ILogger<FeatureService>> _mockLogger;
    private readonly FeatureService _service;

    public FeatureServiceTests()
    {
        _mockRepository = new Mock<IFeatureRepository>();
        _mockCreateValidator = new Mock<IValidator<CreateFeatureRequest>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateFeatureRequest>>();
        _mockLogger = new Mock<ILogger<FeatureService>>();

        _service = new FeatureService(
            _mockRepository.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var featureId = "test-id";
        var expectedFeature = FeatureEntity.Create("Test Feature").GetValueOrThrow();
        
        _mockRepository.Setup(x => x.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result<FeatureEntity>.Success(expectedFeature));

        // Act
        var result = await _service.GetByIdAsync(featureId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedFeature.Name, result.Value.Name);
        _mockRepository.Verify(x => x.GetByIdAsync(featureId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WithInvalidId_ReturnsFailure(string? featureId)
    {
        // Act
        var result = await _service.GetByIdAsync(featureId!);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Feature ID cannot be null or empty", result.Error);
        _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
```

## 📝 **IMPLEMENTATION NOTES**

### **Key Principles Applied**
1. **Immutable Domain Models**: Using records for thread-safety and predictability
2. **Result Pattern**: Functional error handling without exceptions
3. **Dependency Injection**: Proper IoC container usage
4. **Async/Await**: Non-blocking operations with cancellation support
5. **Comprehensive Logging**: Structured logging with correlation IDs
6. **Unit Testing**: High coverage with proper mocking
7. **SOLID Principles**: Clean, maintainable, and extensible code

### **Performance Considerations**
- Use `ConfigureAwait(false)` in library code
- Implement proper cancellation token propagation
- Consider caching for frequently accessed data
- Use `IAsyncEnumerable<T>` for large data sets
- Implement proper disposal patterns

### **Security Considerations**
- Validate all inputs at service boundaries
- Use parameterized queries to prevent injection
- Implement proper authorization checks
- Log security-relevant events
- Handle sensitive data appropriately

---

**This template provides a complete, production-ready implementation pattern following S7Tools architectural standards.**