using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;

namespace S7Tools.Benchmarks;

/// <summary>
/// Benchmarks for Profile CRUD operations to measure performance of profile management.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class ProfileCrudBenchmarks
{
    private ISerialPortProfileService _profileManager = null!;
    private SerialPortProfile _testProfile = null!;
    private int _createdProfileId;

    /// <summary>
    /// Sets up the benchmark environment before each iteration.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Create mock path service for benchmarking
        var mockPathService = new MockPathService();

        // Create profile manager with null logger for benchmarking
        _profileManager = new SerialPortProfileService(
            NullLogger<SerialPortProfileService>.Instance,
            mockPathService);

        // Create a test profile
        _testProfile = new SerialPortProfile
        {
            Name = "Benchmark Profile",
            Description = "Profile for performance testing",
            Configuration = new SerialPortConfiguration
            {
                BaudRate = 9600,
                CharacterSize = 8,
                ParityEnabled = false
            }
        };
    }

    /// <summary>
    /// Cleans up after benchmarks complete.
    /// </summary>
    [GlobalCleanup]
    public async Task Cleanup()
    {
        // Clean up any created profiles
        try
        {
            IEnumerable<SerialPortProfile> profiles = await _profileManager.GetAllAsync();
            foreach (SerialPortProfile profile in profiles)
            {
                await _profileManager.DeleteAsync(profile.Id);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Benchmarks profile creation performance.
    /// </summary>
    [Benchmark]
    public async Task<SerialPortProfile> CreateProfile()
    {
        SerialPortProfile profile = await _profileManager.CreateAsync(_testProfile);
        _createdProfileId = profile.Id;
        return profile;
    }

    /// <summary>
    /// Benchmarks profile retrieval by ID performance.
    /// </summary>
    [Benchmark]
    public async Task<SerialPortProfile?> GetProfileById()
    {
        return await _profileManager.GetByIdAsync(_createdProfileId);
    }

    /// <summary>
    /// Benchmarks retrieving all profiles performance.
    /// </summary>
    [Benchmark]
    public async Task<IEnumerable<SerialPortProfile>> GetAllProfiles()
    {
        return await _profileManager.GetAllAsync();
    }

    /// <summary>
    /// Benchmarks profile update performance.
    /// </summary>
    [Benchmark]
    public async Task<SerialPortProfile> UpdateProfile()
    {
        SerialPortProfile? profile = await _profileManager.GetByIdAsync(_createdProfileId);
        if (profile != null)
        {
            profile.Description = "Updated description";
            return await _profileManager.UpdateAsync(profile);
        }
        return _testProfile;
    }

    /// <summary>
    /// Benchmarks profile duplication performance.
    /// </summary>
    [Benchmark]
    public async Task<SerialPortProfile> DuplicateProfile()
    {
        return await _profileManager.DuplicateAsync(_createdProfileId, "Duplicated Profile");
    }

    /// <summary>
    /// Mock path service for benchmarking
    /// </summary>
    private class MockPathService : IPathService
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "S7Tools_Benchmarks");

        public string BaseDirectory => _tempDir;
        public string ResourcesDirectory => Path.Combine(_tempDir, "Resources");
        public string AppSettingsPath => Path.Combine(_tempDir, "AppSettings", "AppSettings.json");
        public string ProfilesDirectory => Path.Combine(ResourcesDirectory, "Profiles");
        public string SerialProfilesPath => Path.Combine(ProfilesDirectory, "Serial", "SerialProfiles.json");
        public string SocatProfilesPath => Path.Combine(ProfilesDirectory, "Socat", "SocatProfiles.json");
        public string PowerSupplyProfilesPath => Path.Combine(ProfilesDirectory, "PowerSupply", "PowerSupplyProfiles.json");
        public string MemoryRegionProfilesPath => Path.Combine(ProfilesDirectory, "MemoryRegions", "MemoryRegionProfiles.json");
        public string PayloadSetProfilesPath => Path.Combine(ProfilesDirectory, "PayloadSets", "PayloadSetProfiles.json");
        public string LogsDirectory => Path.Combine(ResourcesDirectory, "Logs");
        public string MainLogsDirectory => Path.Combine(LogsDirectory, "Main");
        public string ExportedLogsDirectory => Path.Combine(LogsDirectory, "Exported");
        public string JobsPath => Path.Combine(ResourcesDirectory, "Jobs", "Jobs.json");
        public string TasksPath => Path.Combine(ResourcesDirectory, "Tasks", "Tasks.json");
        public string PayloadsDirectory => Path.Combine(ResourcesDirectory, "Payloads");
        public string DumpsDirectory => Path.Combine(ResourcesDirectory, "Dumps");

        public Task<PathConfiguration> InitializeAsync() => Task.FromResult(new PathConfiguration { BaseDirectory = _tempDir });

        public Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            return Task.FromResult(true);
        }

        public string ResolvePath(string relativePath) => Path.Combine(_tempDir, relativePath);

        public Task<PathValidationResult> ValidatePathsAsync() => Task.FromResult(new PathValidationResult { IsValid = true });

        public string GetMainLogPath(int rollingNumber = 0) => Path.Combine(MainLogsDirectory, $"main_{rollingNumber}.log");

        public string GetExportedLogPath(string format) => Path.Combine(ExportedLogsDirectory, $"export.{format.ToLowerInvariant()}");

        public string GetResourcePath(params string[] pathComponents) => Path.Combine(new[] { ResourcesDirectory }.Concat(pathComponents).ToArray());
    }
}
