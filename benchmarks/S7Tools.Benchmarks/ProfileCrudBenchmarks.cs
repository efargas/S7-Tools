using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using S7Tools.Core.Models;
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
        // Create profile manager with null logger for benchmarking
        _profileManager = new SerialPortProfileService(
            NullLogger<SerialPortProfileService>.Instance);

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
            var profiles = await _profileManager.GetAllAsync();
            foreach (var profile in profiles)
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
        var profile = await _profileManager.CreateAsync(_testProfile);
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
        var profile = await _profileManager.GetByIdAsync(_createdProfileId);
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
}
