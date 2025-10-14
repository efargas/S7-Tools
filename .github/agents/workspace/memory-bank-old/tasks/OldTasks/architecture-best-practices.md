# S7Tools Architecture Best Practices & Implementation Guide

**Version**: 2.0  
**Last Updated**: Current Session  
**Status**: Comprehensive Guide for Modern .NET Development  

## 🏗️ **ARCHITECTURAL PRINCIPLES**

### **1. Clean Architecture Foundation**
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   Views (XAML)  │  │   ViewModels    │  │  Converters  │ │
│  └─────────────────┘  └─────────────────┘  └─��────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │    Services     │  │   Interfaces    │  │   Commands   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │    Entities     │  │  Value Objects  │  │    Result    │ │
│  │      (Tag)      │  │ (PlcAddress)    │  │   Pattern    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   Repositories  │  │     Logging     │  │  External    │ │
│  │   (Data Access) │  │   (DataStore)   │  │   Services   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### **2. SOLID Principles Implementation**

#### **Single Responsibility Principle (SRP)**
```csharp
// ✅ GOOD: Each class has one reason to change
public sealed class TagValidationService : ITagValidationService
{
    public Result<Tag> ValidateTag(string name, string address, object? value)
    {
        // Only responsible for tag validation logic
    }
}

public sealed class TagPersistenceService : ITagPersistenceService  
{
    public Task<Result> SaveTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        // Only responsible for tag persistence
    }
}

// ❌ BAD: Multiple responsibilities
public class TagManager
{
    public Result<Tag> ValidateTag(...) { } // Validation responsibility
    public Task SaveTagAsync(...) { }       // Persistence responsibility  
    public void LogActivity(...) { }        // Logging responsibility
}
```

#### **Open/Closed Principle (OCP)**
```csharp
// ✅ GOOD: Open for extension, closed for modification
public abstract class PlcConnectionBase
{
    public abstract Task<Result> ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken);
    
    protected virtual Task<Result> ValidateConfigAsync(ConnectionConfig config)
    {
        // Base validation logic
        return Task.FromResult(Result.Success());
    }
}

public sealed class S7PlcConnection : PlcConnectionBase
{
    public override async Task<Result> ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateConfigAsync(config);
        if (validationResult.IsFailure) return validationResult;
        
        // S7-specific connection logic
        return Result.Success();
    }
    
    protected override async Task<Result> ValidateConfigAsync(ConnectionConfig config)
    {
        var baseResult = await base.ValidateConfigAsync(config);
        if (baseResult.IsFailure) return baseResult;
        
        // S7-specific validation
        return Result.Success();
    }
}
```

#### **Liskov Substitution Principle (LSP)**
```csharp
// ✅ GOOD: Derived classes are substitutable for base classes
public interface ITagRepository
{
    Task<Result<Tag>> GetTagAsync(string name, CancellationToken cancellationToken = default);
}

public sealed class InMemoryTagRepository : ITagRepository
{
    public async Task<Result<Tag>> GetTagAsync(string name, CancellationToken cancellationToken = default)
    {
        // Implementation that honors the contract
        if (string.IsNullOrWhiteSpace(name))
            return Result<Tag>.Failure("Tag name cannot be empty");
            
        // ... implementation
    }
}

public sealed class DatabaseTagRepository : ITagRepository  
{
    public async Task<Result<Tag>> GetTagAsync(string name, CancellationToken cancellationToken = default)
    {
        // Different implementation but same contract behavior
        if (string.IsNullOrWhiteSpace(name))
            return Result<Tag>.Failure("Tag name cannot be empty");
            
        // ... database implementation
    }
}
```

#### **Interface Segregation Principle (ISP)**
```csharp
// ✅ GOOD: Specific, focused interfaces
public interface ITagReader
{
    Task<Result<Tag>> ReadTagAsync(PlcAddress address, CancellationToken cancellationToken = default);
}

public interface ITagWriter
{
    Task<Result> WriteTagAsync(PlcAddress address, object? value, CancellationToken cancellationToken = default);
}

public interface ITagValidator
{
    Result<Tag> ValidateTag(string name, string address, object? value);
}

// ❌ BAD: Fat interface
public interface ITagManager
{
    Task<Result<Tag>> ReadTagAsync(...);
    Task<Result> WriteTagAsync(...);
    Result<Tag> ValidateTag(...);
    Task<Result> SaveConfigAsync(...);
    Task<Result> LoadConfigAsync(...);
    void StartMonitoring();
    void StopMonitoring();
    // ... many more methods
}
```

#### **Dependency Inversion Principle (DIP)**
```csharp
// ✅ GOOD: Depend on abstractions, not concretions
public sealed class PlcDataService : IPlcDataService
{
    private readonly ITagRepository _tagRepository;        // Abstraction
    private readonly IConnectionProvider _connectionProvider; // Abstraction
    private readonly ILogger<PlcDataService> _logger;     // Abstraction
    
    public PlcDataService(
        ITagRepository tagRepository,
        IConnectionProvider connectionProvider,
        ILogger<PlcDataService> logger)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// ❌ BAD: Depend on concrete implementations
public class PlcDataService
{
    private readonly SqlTagRepository _tagRepository;      // Concrete class
    private readonly S7Connection _connection;             // Concrete class
    private readonly FileLogger _logger;                  // Concrete class
    
    public PlcDataService()
    {
        _tagRepository = new SqlTagRepository();           // Hard dependency
        _connection = new S7Connection();                  // Hard dependency
        _logger = new FileLogger();                       // Hard dependency
    }
}
```

## 🔧 **SERVICE IMPLEMENTATION PATTERNS**

### **1. Service Interface Design**
```csharp
/// <summary>
/// Defines operations for managing PLC tag data with modern async patterns.
/// Follows the Repository pattern with Result-based error handling.
/// </summary>
public interface ITagDataService
{
    /// <summary>
    /// Retrieves a tag by its unique identifier.
    /// </summary>
    /// <param name="tagId">The unique identifier of the tag.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the Tag or an error message.</returns>
    Task<Result<Tag>> GetTagByIdAsync(string tagId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves tags filtered by the specified criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the filtered tags or an error message.</returns>
    Task<Result<IReadOnlyCollection<Tag>>> GetTagsAsync(TagFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new tag with validation.
    /// </summary>
    /// <param name="createRequest">The tag creation request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the created Tag or an error message.</returns>
    Task<Result<Tag>> CreateTagAsync(CreateTagRequest createRequest, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    /// <param name="updateRequest">The tag update request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> UpdateTagAsync(UpdateTagRequest updateRequest, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a tag by its identifier.
    /// </summary>
    /// <param name="tagId">The unique identifier of the tag to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteTagAsync(string tagId, CancellationToken cancellationToken = default);
}
```

### **2. Service Implementation Template**
```csharp
/// <summary>
/// Implementation of tag data service with comprehensive error handling and logging.
/// Follows modern .NET patterns with proper async/await usage.
/// </summary>
public sealed class TagDataService : ITagDataService, IDisposable
{
    private readonly ITagRepository _repository;
    private readonly ITagValidator _validator;
    private readonly ILogger<TagDataService> _logger;
    private readonly IMetrics _metrics;
    private bool _disposed;

    public TagDataService(
        ITagRepository repository,
        ITagValidator validator,
        ILogger<TagDataService> logger,
        IMetrics metrics)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task<Result<Tag>> GetTagByIdAsync(string tagId, CancellationToken cancellationToken = default)
    {
        using var activity = _metrics.StartActivity("TagDataService.GetTagById");
        activity?.SetTag("tagId", tagId);
        
        try
        {
            _logger.LogDebug("Retrieving tag with ID: {TagId}", tagId);
            
            if (string.IsNullOrWhiteSpace(tagId))
            {
                const string error = "Tag ID cannot be null or empty";
                _logger.LogWarning(error);
                return Result<Tag>.Failure(error);
            }

            var result = await _repository.GetByIdAsync(tagId, cancellationToken).ConfigureAwait(false);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully retrieved tag: {TagName}", result.Value.Name);
                _metrics.IncrementCounter("tags.retrieved");
            }
            else
            {
                _logger.LogWarning("Failed to retrieve tag {TagId}: {Error}", tagId, result.Error);
                _metrics.IncrementCounter("tags.retrieval_failed");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tag with ID: {TagId}", tagId);
            _metrics.IncrementCounter("tags.retrieval_error");
            return Result<Tag>.Failure($"Failed to retrieve tag: {ex.Message}", ex);
        }
    }

    public async Task<Result<Tag>> CreateTagAsync(CreateTagRequest createRequest, CancellationToken cancellationToken = default)
    {
        using var activity = _metrics.StartActivity("TagDataService.CreateTag");
        activity?.SetTag("tagName", createRequest.Name);
        
        try
        {
            _logger.LogDebug("Creating tag: {@CreateRequest}", createRequest);
            
            // Validate the request
            var validationResult = await _validator.ValidateCreateRequestAsync(createRequest, cancellationToken).ConfigureAwait(false);
            if (validationResult.IsFailure)
            {
                _logger.LogWarning("Tag creation validation failed: {Error}", validationResult.Error);
                return Result<Tag>.Failure(validationResult.Error);
            }

            // Create the tag entity
            var tagResult = Tag.Create(
                createRequest.Name,
                createRequest.Address,
                createRequest.InitialValue,
                createRequest.DataType,
                createRequest.Description,
                createRequest.Group);
                
            if (tagResult.IsFailure)
            {
                _logger.LogWarning("Failed to create tag entity: {Error}", tagResult.Error);
                return tagResult;
            }

            // Persist the tag
            var saveResult = await _repository.AddAsync(tagResult.Value, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save tag: {Error}", saveResult.Error);
                return Result<Tag>.Failure(saveResult.Error);
            }

            _logger.LogInformation("Successfully created tag: {TagName} at address {Address}", 
                tagResult.Value.Name, tagResult.Value.Address);
            _metrics.IncrementCounter("tags.created");
            
            return tagResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag: {@CreateRequest}", createRequest);
            _metrics.IncrementCounter("tags.creation_error");
            return Result<Tag>.Failure($"Failed to create tag: {ex.Message}", ex);
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

### **3. Request/Response Models**
```csharp
/// <summary>
/// Request model for creating a new tag with validation attributes.
/// </summary>
public sealed record CreateTagRequest
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the PLC address for the tag.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string Address { get; init; }

    /// <summary>
    /// Gets the initial value for the tag.
    /// </summary>
    public object? InitialValue { get; init; }

    /// <summary>
    /// Gets the data type of the tag.
    /// </summary>
    public PlcDataType DataType { get; init; } = PlcDataType.Unknown;

    /// <summary>
    /// Gets the optional description of the tag.
    /// </summary>
    [StringLength(500)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional group/category for the tag.
    /// </summary>
    [StringLength(100)]
    public string Group { get; init; } = string.Empty;

    /// <summary>
    /// Gets the scan rate in milliseconds.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ScanRate { get; init; } = 1000;

    /// <summary>
    /// Gets whether the tag is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Filter criteria for querying tags.
/// </summary>
public sealed record TagFilter
{
    /// <summary>
    /// Gets the optional name pattern for filtering.
    /// </summary>
    public string? NamePattern { get; init; }

    /// <summary>
    /// Gets the optional group filter.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Gets the optional data type filter.
    /// </summary>
    public PlcDataType? DataType { get; init; }

    /// <summary>
    /// Gets whether to include only enabled tags.
    /// </summary>
    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    [Range(1, 1000)]
    public int MaxResults { get; init; } = 100;

    /// <summary>
    /// Gets the number of results to skip (for pagination).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Skip { get; init; } = 0;
}
```

## 🧪 **TESTING BEST PRACTICES**

### **1. Unit Test Structure**
```csharp
/// <summary>
/// Comprehensive unit tests for TagDataService following AAA pattern.
/// Tests cover happy path, edge cases, and error scenarios.
/// </summary>
public sealed class TagDataServiceTests : IDisposable
{
    private readonly Mock<ITagRepository> _mockRepository;
    private readonly Mock<ITagValidator> _mockValidator;
    private readonly Mock<ILogger<TagDataService>> _mockLogger;
    private readonly Mock<IMetrics> _mockMetrics;
    private readonly TagDataService _service;

    public TagDataServiceTests()
    {
        _mockRepository = new Mock<ITagRepository>();
        _mockValidator = new Mock<ITagValidator>();
        _mockLogger = new Mock<ILogger<TagDataService>>();
        _mockMetrics = new Mock<IMetrics>();
        
        _service = new TagDataService(
            _mockRepository.Object,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockMetrics.Object);
    }

    [Theory]
    [InlineData("TAG001")]
    [InlineData("Motor_Speed")]
    [InlineData("DB1.Temperature")]
    public async Task GetTagByIdAsync_WithValidId_ReturnsSuccess(string tagId)
    {
        // Arrange
        var expectedTag = CreateTestTag(tagId);
        _mockRepository.Setup(x => x.GetByIdAsync(tagId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result<Tag>.Success(expectedTag));

        // Act
        var result = await _service.GetTagByIdAsync(tagId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(tagId, result.Value.Name);
        
        _mockRepository.Verify(x => x.GetByIdAsync(tagId, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLoggerCalled(LogLevel.Debug, "Retrieving tag with ID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTagByIdAsync_WithInvalidId_ReturnsFailure(string? tagId)
    {
        // Act
        var result = await _service.GetTagByIdAsync(tagId!);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Tag ID cannot be null or empty", result.Error);
        
        _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTagAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var createRequest = new CreateTagRequest
        {
            Name = "TestTag",
            Address = "DB1.DBX0.0",
            DataType = PlcDataType.Bool,
            Description = "Test tag for unit testing"
        };

        _mockValidator.Setup(x => x.ValidateCreateRequestAsync(createRequest, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Success());
        
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result.Success());

        // Act
        var result = await _service.CreateTagAsync(createRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(createRequest.Name, result.Value.Name);
        
        _mockValidator.Verify(x => x.ValidateCreateRequestAsync(createRequest, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTagAsync_WithValidationFailure_ReturnsFailure()
    {
        // Arrange
        var createRequest = new CreateTagRequest
        {
            Name = "InvalidTag",
            Address = "InvalidAddress"
        };

        _mockValidator.Setup(x => x.ValidateCreateRequestAsync(createRequest, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Failure("Invalid address format"));

        // Act
        var result = await _service.CreateTagAsync(createRequest);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid address format", result.Error);
        
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTagAsync_WithRepositoryException_ReturnsFailure()
    {
        // Arrange
        var createRequest = new CreateTagRequest
        {
            Name = "TestTag",
            Address = "DB1.DBX0.0"
        };

        _mockValidator.Setup(x => x.ValidateCreateRequestAsync(createRequest, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Success());
        
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _service.CreateTagAsync(createRequest);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Failed to create tag", result.Error);
        Assert.Contains("Database connection failed", result.Error);
        
        VerifyLoggerCalled(LogLevel.Error, "Error creating tag");
    }

    private static Tag CreateTestTag(string name) =>
        Tag.Create(name, "DB1.DBX0.0", true, PlcDataType.Bool).GetValueOrThrow();

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
```

### **2. Integration Test Template**
```csharp
/// <summary>
/// Integration tests for TagDataService with real dependencies.
/// Uses TestContainers for database testing and proper cleanup.
/// </summary>
[Collection("Database")]
public sealed class TagDataServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly IServiceProvider _serviceProvider;
    private ITagDataService _service = null!;

    public TagDataServiceIntegrationTests()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("s7tools_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<S7ToolsDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));
        
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITagValidator, TagValidator>();
        services.AddScoped<ITagDataService, TagDataService>();
        services.AddSingleton<IMetrics, TestMetrics>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Apply migrations
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<S7ToolsDbContext>();
        await context.Database.MigrateAsync();
        
        _service = _serviceProvider.GetRequiredService<ITagDataService>();
    }

    [Fact]
    public async Task CreateAndRetrieveTag_EndToEnd_Success()
    {
        // Arrange
        var createRequest = new CreateTagRequest
        {
            Name = "IntegrationTestTag",
            Address = "DB1.DBX0.0",
            DataType = PlcDataType.Bool,
            Description = "Integration test tag"
        };

        // Act - Create
        var createResult = await _service.CreateTagAsync(createRequest);
        
        // Assert - Create
        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);
        
        // Act - Retrieve
        var retrieveResult = await _service.GetTagByIdAsync(createResult.Value.Name);
        
        // Assert - Retrieve
        Assert.True(retrieveResult.IsSuccess);
        Assert.Equal(createRequest.Name, retrieveResult.Value.Name);
        Assert.Equal(createRequest.Address, retrieveResult.Value.Address.Value);
        Assert.Equal(createRequest.Description, retrieveResult.Value.Description);
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await _dbContainer.DisposeAsync();
    }
}
```

## 🔄 **ASYNC/AWAIT BEST PRACTICES**

### **1. Proper Async Patterns**
```csharp
// ✅ CORRECT: Async all the way down
public async Task<Result<IReadOnlyCollection<Tag>>> GetTagsAsync(CancellationToken cancellationToken = default)
{
    try
    {
        // Use ConfigureAwait(false) in library code
        var tags = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        
        // Parallel processing when appropriate
        var validationTasks = tags.Select(tag => ValidateTagAsync(tag, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);
        
        var validTags = validationResults
            .Where(result => result.IsSuccess)
            .Select(result => result.Value)
            .ToList()
            .AsReadOnly();
            
        return Result<IReadOnlyCollection<Tag>>.Success(validTags);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        _logger.LogInformation("Tag retrieval was cancelled");
        return Result<IReadOnlyCollection<Tag>>.Failure("Operation was cancelled");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving tags");
        return Result<IReadOnlyCollection<Tag>>.Failure($"Failed to retrieve tags: {ex.Message}", ex);
    }
}

// ✅ CORRECT: Proper cancellation token usage
public async Task<Result> ProcessTagsAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromMinutes(5)); // Timeout protection
    
    try
    {
        foreach (var tag in tags)
        {
            cts.Token.ThrowIfCancellationRequested();
            
            var result = await ProcessSingleTagAsync(tag, cts.Token).ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process tag {TagName}: {Error}", tag.Name, result.Error);
            }
        }
        
        return Result.Success();
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        return Result.Failure("Processing was cancelled by user");
    }
    catch (OperationCanceledException)
    {
        return Result.Failure("Processing timed out after 5 minutes");
    }
}

// ❌ AVOID: Blocking async code
public Tag GetTag(string id)
{
    // DON'T DO THIS - blocks the thread
    return GetTagAsync(id).Result;
}

// ❌ AVOID: Async void (except for event handlers)
public async void ProcessTags() // Should be async Task
{
    await ProcessTagsAsync();
}

// ❌ AVOID: Unnecessary Task.Run in ASP.NET Core
public async Task<Result> ProcessDataAsync()
{
    // DON'T DO THIS in web applications
    return await Task.Run(() => ProcessData());
}
```

### **2. Exception Handling in Async Code**
```csharp
public async Task<Result<T>> SafeAsyncOperation<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
{
    try
    {
        var result = await operation(cancellationToken).ConfigureAwait(false);
        return Result<T>.Success(result);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        _logger.LogInformation("Operation was cancelled by user request");
        return Result<T>.Failure("Operation was cancelled");
    }
    catch (TimeoutException ex)
    {
        _logger.LogWarning(ex, "Operation timed out");
        return Result<T>.Failure("Operation timed out", ex);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Invalid argument provided");
        return Result<T>.Failure($"Invalid argument: {ex.Message}", ex);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Invalid operation state");
        return Result<T>.Failure($"Invalid operation: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during async operation");
        return Result<T>.Failure($"Unexpected error: {ex.Message}", ex);
    }
}
```

## 📋 **IMPLEMENTATION CHECKLIST**

### **Service Development**
- [ ] Interface follows ISP (Interface Segregation Principle)
- [ ] Implementation uses dependency injection
- [ ] All methods return Result<T> for error handling
- [ ] Proper async/await patterns with CancellationToken
- [ ] Comprehensive logging with structured data
- [ ] Exception handling with specific catch blocks
- [ ] Metrics/telemetry integration
- [ ] IDisposable implementation when needed

### **Testing Coverage**
- [ ] Unit tests for all public methods
- [ ] Happy path scenarios covered
- [ ] Edge cases and error scenarios tested
- [ ] Mocking of all dependencies
- [ ] Integration tests for critical paths
- [ ] Performance tests for high-load scenarios
- [ ] Cancellation token behavior verified

### **Code Quality**
- [ ] XML documentation for all public members
- [ ] Nullable reference types enabled
- [ ] Code analysis warnings addressed
- [ ] SOLID principles followed
- [ ] Clean code practices applied
- [ ] Performance considerations addressed

---

**This guide provides the foundation for implementing high-quality, maintainable features in S7Tools following modern .NET best practices.**