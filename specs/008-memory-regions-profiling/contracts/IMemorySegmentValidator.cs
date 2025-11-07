using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces
{
    /// <summary>
    /// Service for validating memory segments and detecting conflicts
    /// </summary>
    public interface IMemorySegmentValidator
    {
        /// <summary>
        /// Validates a collection of memory segments for overlaps and constraints
        /// </summary>
        /// <param name="segments">Segments to validate</param>
        /// <returns>Validation result with detailed information</returns>
        ValidationResult ValidateSegments(IEnumerable<MemorySegment> segments);

        /// <summary>
        /// Checks if two memory segments overlap
        /// </summary>
        /// <param name="segment1">First segment</param>
        /// <param name="segment2">Second segment</param>
        /// <returns>True if segments overlap</returns>
        bool DoSegmentsOverlap(MemorySegment segment1, MemorySegment segment2);

        /// <summary>
        /// Validates a single memory segment
        /// </summary>
        /// <param name="segment">Segment to validate</param>
        /// <returns>Validation result for the segment</returns>
        ValidationResult ValidateSegment(MemorySegment segment);

        /// <summary>
        /// Validates hexadecimal address format
        /// </summary>
        /// <param name="address">Address string to validate</param>
        /// <returns>True if address format is valid</returns>
        bool IsValidAddress(string address);

        /// <summary>
        /// Parses hexadecimal address string to numeric value
        /// </summary>
        /// <param name="address">Address string (with or without 0x prefix)</param>
        /// <returns>Parsed address value</returns>
        long ParseAddress(string address);

        /// <summary>
        /// Formats address value to hexadecimal string
        /// </summary>
        /// <param name="address">Address value</param>
        /// <param name="includePrefix">Whether to include 0x prefix</param>
        /// <returns>Formatted address string</returns>
        string FormatAddress(long address, bool includePrefix = true);

        /// <summary>
        /// Validates memory size constraints
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <param name="segmentType">Type of memory segment</param>
        /// <returns>True if size is valid for segment type</returns>
        bool IsValidSize(long size, MemorySegmentType segmentType);

        /// <summary>
        /// Gets maximum allowed size for a memory segment type
        /// </summary>
        /// <param name="segmentType">Memory segment type</param>
        /// <returns>Maximum size in bytes</returns>
        long GetMaximumSize(MemorySegmentType segmentType);

        /// <summary>
        /// Sorts segments by start address for overlap detection
        /// </summary>
        /// <param name="segments">Segments to sort</param>
        /// <returns>Sorted segments by start address</returns>
        IEnumerable<MemorySegment> SortSegmentsByAddress(IEnumerable<MemorySegment> segments);

        /// <summary>
        /// Finds all overlapping segment pairs in a collection
        /// </summary>
        /// <param name="segments">Segments to check</param>
        /// <returns>Pairs of overlapping segments</returns>
        IEnumerable<(MemorySegment First, MemorySegment Second)> FindOverlappingSegments(IEnumerable<MemorySegment> segments);

        /// <summary>
        /// Suggests non-overlapping address for a new segment
        /// </summary>
        /// <param name="existingSegments">Existing segments</param>
        /// <param name="newSegmentSize">Size of new segment</param>
        /// <param name="segmentType">Type of new segment</param>
        /// <returns>Suggested start address for new segment</returns>
        long SuggestNonOverlappingAddress(IEnumerable<MemorySegment> existingSegments, long newSegmentSize, MemorySegmentType segmentType);

        /// <summary>
        /// Validates that segment name is unique within profile
        /// </summary>
        /// <param name="segmentName">Name to validate</param>
        /// <param name="existingSegments">Existing segments in profile</param>
        /// <param name="excludeSegment">Segment to exclude from check (for editing)</param>
        /// <returns>True if name is unique</returns>
        bool IsSegmentNameUnique(string segmentName, IEnumerable<MemorySegment> existingSegments, MemorySegment? excludeSegment = null);
    }

    /// <summary>
    /// Validation result with detailed error and warning information
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();

        public void AddError(string property, string message, object? value = null)
        {
            Errors.Add(new ValidationError { Property = property, Message = message, Value = value });
        }

        public void AddWarning(string property, string message, object? value = null)
        {
            Warnings.Add(new ValidationWarning { Property = property, Message = message, Value = value });
        }
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string Property { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string Property { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
