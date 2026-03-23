using S7Tools.Core.Models;
using S7Tools.Core.Validation;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Service for validating memory segments and detecting conflicts.
/// </summary>
/// <remarks>
/// This service provides validation logic for memory segments including overlap detection,
/// address validation, and size constraints. It uses the existing validation infrastructure.
/// </remarks>
public interface IMemorySegmentValidator
{
    /// <summary>
    /// Validates a collection of memory segments for overlaps and constraints.
    /// </summary>
    /// <param name="segments">The segments to validate.</param>
    /// <returns>A validation result with detailed information about any issues.</returns>
    ValidationResult ValidateSegments(IEnumerable<MemorySegment> segments);

    /// <summary>
    /// Checks if two memory segments overlap.
    /// </summary>
    /// <param name="segment1">The first segment to check.</param>
    /// <param name="segment2">The second segment to check.</param>
    /// <returns>True if the segments overlap, false otherwise.</returns>
    bool DoSegmentsOverlap(MemorySegment segment1, MemorySegment segment2);

    /// <summary>
    /// Validates a single memory segment.
    /// </summary>
    /// <param name="segment">The segment to validate.</param>
    /// <returns>A validation result for the segment.</returns>
    ValidationResult ValidateSegment(MemorySegment segment);

    /// <summary>
    /// Validates hexadecimal address format.
    /// </summary>
    /// <param name="address">The address string to validate.</param>
    /// <returns>True if the address format is valid, false otherwise.</returns>
    bool IsValidAddress(string address);

    /// <summary>
    /// Validates memory size constraints.
    /// </summary>
    /// <param name="size">The size in bytes to validate.</param>
    /// <param name="segmentType">The type of memory segment.</param>
    /// <returns>True if the size is valid for the segment type, false otherwise.</returns>
    bool IsValidSize(long size, MemorySegmentType segmentType);

    /// <summary>
    /// Validates that a segment name is unique within a collection.
    /// </summary>
    /// <param name="segmentName">The name to validate.</param>
    /// <param name="existingSegments">The existing segments in the profile.</param>
    /// <param name="excludeSegment">Optional segment to exclude from the check (for editing scenarios).</param>
    /// <returns>True if the name is unique, false otherwise.</returns>
    bool IsSegmentNameUnique(string segmentName, IEnumerable<MemorySegment> existingSegments, MemorySegment? excludeSegment = null);

    /// <summary>
    /// Finds all overlapping segment pairs in a collection.
    /// </summary>
    /// <param name="segments">The segments to check for overlaps.</param>
    /// <returns>Pairs of overlapping segments.</returns>
    IEnumerable<(MemorySegment First, MemorySegment Second)> FindOverlappingSegments(IEnumerable<MemorySegment> segments);
}
