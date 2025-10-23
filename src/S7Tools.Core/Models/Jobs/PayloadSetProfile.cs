using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Defines the configuration for bootloader payload files.
/// Specifies the base path where stager and dumper payloads are located.
/// </summary>
/// <param name="BasePath">Base directory path containing payload files.</param>
public sealed record PayloadSetProfile(
    [property: Display(Name = "Payload Base Path", Order = 1)]
    string BasePath
);
