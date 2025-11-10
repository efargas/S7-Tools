using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing memory region profiles with CRUD operations
    /// Extends unified profile management with memory-specific functionality
    /// </summary>
    public interface IMemoryRegionProfileService : IProfileService<MemoryRegionProfile>
    {
        /// <summary>
        /// Creates a default memory region profile with standard firmware segments
        /// </summary>
        /// <returns>Default profile with .text, .data, .bss segments</returns>
        Task<MemoryRegionProfile> CreateDefaultProfileAsync();

        /// <summary>
        /// Validates memory segments for overlaps and address conflicts
        /// </summary>
        /// <param name="profile">Profile to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        Task<ValidationResult> ValidateProfileAsync(MemoryRegionProfile profile);

        /// <summary>
        /// Gets all segments marked as selected across all profiles
        /// </summary>
        /// <returns>Collection of selected memory segments</returns>
        Task<IEnumerable<MemorySegment>> GetSelectedSegmentsAsync();

        /// <summary>
        /// Updates segment selection state for a specific profile
        /// </summary>
        /// <param name="profileId">Profile identifier</param>
        /// <param name="segmentName">Segment name to update</param>
        /// <param name="isSelected">New selection state</param>
        /// <returns>Updated profile</returns>
        Task<MemoryRegionProfile> UpdateSegmentSelectionAsync(Guid profileId, string segmentName, bool isSelected);

        /// <summary>
        /// Finds profiles that contain segments overlapping with specified address range
        /// </summary>
        /// <param name="startAddress">Start address in hex format</param>
        /// <param name="size">Size in bytes</param>
        /// <returns>Profiles with overlapping segments</returns>
        Task<IEnumerable<MemoryRegionProfile>> FindProfilesWithOverlapAsync(string startAddress, long size);

        /// <summary>
        /// Creates a profile from predefined template
        /// </summary>
        /// <param name="templateName">Template name (e.g., "STM32F4", "Arduino", "Custom")</param>
        /// <param name="profileName">Name for new profile</param>
        /// <returns>Profile created from template</returns>
        Task<MemoryRegionProfile> CreateFromTemplateAsync(string templateName, string profileName);

        /// <summary>
        /// Gets available memory region templates
        /// </summary>
        /// <returns>Collection of template names and descriptions</returns>
        Task<IEnumerable<MemoryRegionTemplate>> GetAvailableTemplatesAsync();

        /// <summary>
        /// Exports profile to external format for backup or sharing
        /// </summary>
        /// <param name="profileId">Profile to export</param>
        /// <param name="filePath">Export file path</param>
        /// <param name="format">Export format (JSON, XML, CSV)</param>
        /// <returns>Export operation result</returns>
        Task<ExportResult> ExportProfileAsync(Guid profileId, string filePath, ExportFormat format);

        /// <summary>
        /// Imports profile from external file
        /// </summary>
        /// <param name="filePath">Import file path</param>
        /// <param name="format">Import format</param>
        /// <returns>Imported profile</returns>
        Task<MemoryRegionProfile> ImportProfileAsync(string filePath, ExportFormat format);
    }

    /// <summary>
    /// Memory region template definition
    /// </summary>
    public class MemoryRegionTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetDevice { get; set; } = string.Empty;
        public List<MemorySegment> DefaultSegments { get; set; } = new();
    }

    /// <summary>
    /// Export operation result
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ExportPath { get; set; }
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Export/import format enumeration
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Xml,
        Csv
    }
}
