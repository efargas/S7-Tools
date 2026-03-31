namespace S7Tools.Core.Exceptions;

/// <summary>
/// Base exception for all memory region related errors.
/// </summary>
/// <remarks>
/// This exception serves as the base for all memory region profiling specific exceptions,
/// providing a way to catch all memory region related errors uniformly.
/// </remarks>
public class MemoryRegionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionException"/> class.
    /// </summary>
    public MemoryRegionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public MemoryRegionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MemoryRegionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a memory segment has invalid properties.
/// </summary>
/// <remarks>
/// This exception is thrown when validation fails for a memory segment due to invalid
/// address format, negative size, or other property validation failures.
/// </remarks>
public class InvalidMemorySegmentException : MemoryRegionException
{
    /// <summary>
    /// Gets the name of the segment that caused the exception.
    /// </summary>
    public string SegmentName { get; }

    /// <summary>
    /// Gets the property name that failed validation.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemorySegmentException"/> class.
    /// </summary>
    /// <param name="segmentName">The name of the segment that caused the exception.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    public InvalidMemorySegmentException(string segmentName, string reason)
        : base($"Invalid memory segment '{segmentName}': {reason}")
    {
        SegmentName = segmentName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemorySegmentException"/> class.
    /// </summary>
    /// <param name="segmentName">The name of the segment that caused the exception.</param>
    /// <param name="propertyName">The property name that failed validation.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    public InvalidMemorySegmentException(string segmentName, string propertyName, string reason)
        : base($"Invalid memory segment '{segmentName}' property '{propertyName}': {reason}")
    {
        SegmentName = segmentName;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemorySegmentException"/> class.
    /// </summary>
    /// <param name="segmentName">The name of the segment that caused the exception.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InvalidMemorySegmentException(string segmentName, string reason, Exception innerException)
        : base($"Invalid memory segment '{segmentName}': {reason}", innerException)
    {
        SegmentName = segmentName;
    }
}

/// <summary>
/// Exception thrown when memory segments have overlapping address ranges.
/// </summary>
/// <remarks>
/// This exception is thrown when two or more memory segments in a profile have
/// overlapping address ranges, which is not allowed in memory region profiles.
/// </remarks>
public class OverlappingMemorySegmentsException : MemoryRegionException
{
    /// <summary>
    /// Gets the name of the first overlapping segment.
    /// </summary>
    public string FirstSegmentName { get; }

    /// <summary>
    /// Gets the name of the second overlapping segment.
    /// </summary>
    public string SecondSegmentName { get; }

    /// <summary>
    /// Gets the start address of the overlap region.
    /// </summary>
    public string? OverlapStart { get; }

    /// <summary>
    /// Gets the end address of the overlap region.
    /// </summary>
    public string? OverlapEnd { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OverlappingMemorySegmentsException"/> class.
    /// </summary>
    /// <param name="firstSegmentName">The name of the first overlapping segment.</param>
    /// <param name="secondSegmentName">The name of the second overlapping segment.</param>
    public OverlappingMemorySegmentsException(string firstSegmentName, string secondSegmentName)
        : base($"Memory segments '{firstSegmentName}' and '{secondSegmentName}' have overlapping address ranges")
    {
        FirstSegmentName = firstSegmentName;
        SecondSegmentName = secondSegmentName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OverlappingMemorySegmentsException"/> class.
    /// </summary>
    /// <param name="firstSegmentName">The name of the first overlapping segment.</param>
    /// <param name="secondSegmentName">The name of the second overlapping segment.</param>
    /// <param name="overlapStart">The start address of the overlap region.</param>
    /// <param name="overlapEnd">The end address of the overlap region.</param>
    public OverlappingMemorySegmentsException(string firstSegmentName, string secondSegmentName,
        string overlapStart, string overlapEnd)
        : base($"Memory segments '{firstSegmentName}' and '{secondSegmentName}' overlap in range {overlapStart} - {overlapEnd}")
    {
        FirstSegmentName = firstSegmentName;
        SecondSegmentName = secondSegmentName;
        OverlapStart = overlapStart;
        OverlapEnd = overlapEnd;
    }
}

/// <summary>
/// Exception thrown when a memory address format is invalid.
/// </summary>
/// <remarks>
/// This exception is thrown when parsing a hexadecimal memory address fails
/// due to invalid format or characters.
/// </remarks>
public class InvalidMemoryAddressException : MemoryRegionException
{
    /// <summary>
    /// Gets the invalid address string.
    /// </summary>
    public string InvalidAddress { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemoryAddressException"/> class.
    /// </summary>
    /// <param name="invalidAddress">The invalid address string.</param>
    public InvalidMemoryAddressException(string invalidAddress)
        : base($"Invalid memory address format: '{invalidAddress}'. Expected hexadecimal format (e.g., '0x08000000' or '08000000').")
    {
        InvalidAddress = invalidAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemoryAddressException"/> class.
    /// </summary>
    /// <param name="invalidAddress">The invalid address string.</param>
    /// <param name="reason">The specific reason for the validation failure.</param>
    public InvalidMemoryAddressException(string invalidAddress, string reason)
        : base($"Invalid memory address '{invalidAddress}': {reason}")
    {
        InvalidAddress = invalidAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMemoryAddressException"/> class.
    /// </summary>
    /// <param name="invalidAddress">The invalid address string.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InvalidMemoryAddressException(string invalidAddress, Exception innerException)
        : base($"Invalid memory address format: '{invalidAddress}'. Expected hexadecimal format.", innerException)
    {
        InvalidAddress = invalidAddress;
    }
}

/// <summary>
/// Exception thrown when a memory mapping profile is not found.
/// </summary>
/// <remarks>
/// This exception is thrown when attempting to access a memory mapping profile
/// by ID or name that does not exist in the profile collection.
/// </remarks>
public class MemoryRegionProfileNotFoundException : MemoryRegionException
{
    /// <summary>
    /// Gets the profile identifier that was not found.
    /// </summary>
    public object ProfileIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionProfileNotFoundException"/> class.
    /// </summary>
    /// <param name="profileId">The profile ID that was not found.</param>
    public MemoryRegionProfileNotFoundException(int profileId)
        : base($"Memory mapping profile with ID {profileId} not found")
    {
        ProfileIdentifier = profileId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionProfileNotFoundException"/> class.
    /// </summary>
    /// <param name="profileName">The profile name that was not found.</param>
    public MemoryRegionProfileNotFoundException(string profileName)
        : base($"Memory mapping profile with name '{profileName}' not found")
    {
        ProfileIdentifier = profileName;
    }
}

/// <summary>
/// Exception thrown when attempting to create a memory mapping profile with a duplicate name.
/// </summary>
/// <remarks>
/// This exception is thrown when trying to create or rename a profile to a name
/// that already exists within the same profile collection.
/// </remarks>
public class DuplicateMemoryRegionProfileNameException : MemoryRegionException
{
    /// <summary>
    /// Gets the duplicate profile name.
    /// </summary>
    public string DuplicateName { get; }

    /// <summary>
    /// Gets the existing profile name that conflicts.
    /// </summary>
    public string? ExistingName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateMemoryRegionProfileNameException"/> class.
    /// </summary>
    /// <param name="duplicateName">The duplicate profile name.</param>
    public DuplicateMemoryRegionProfileNameException(string duplicateName)
        : base($"Memory mapping profile with name '{duplicateName}' already exists")
    {
        DuplicateName = duplicateName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateMemoryRegionProfileNameException"/> class.
    /// </summary>
    /// <param name="duplicateName">The duplicate profile name.</param>
    /// <param name="existingName">The existing profile name that conflicts.</param>
    public DuplicateMemoryRegionProfileNameException(string duplicateName, string existingName)
        : base($"Memory mapping profile with name '{duplicateName}' already exists (conflicts with '{existingName}')")
    {
        DuplicateName = duplicateName;
        ExistingName = existingName;
    }
}
