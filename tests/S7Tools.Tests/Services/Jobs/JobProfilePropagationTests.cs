using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Jobs;

namespace S7Tools.Tests.Services.Jobs;

/// <summary>
/// Tests to verify that task job profiles are correctly propagated to bootloader services
/// without being overwritten by default values.
/// </summary>
public sealed class JobProfilePropagationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testProfilesPath;
    private readonly JobManager _jobManager;
    private readonly ISerialPortProfileService _serialProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly IMemoryRegionProfileService _memoryRegionProfileService;
    private readonly IPayloadSetProfileService _payloadSetProfileService;

    public JobProfilePropagationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"s7tools_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _testProfilesPath = Path.Combine(_testDirectory, "job_profiles.json");

        // Create mock services with test profiles
        _serialProfileService = CreateMockSerialProfileService();
        _socatProfileService = CreateMockSocatProfileService();
        _powerSupplyProfileService = CreateMockPowerSupplyProfileService();
        _memoryRegionProfileService = CreateMockMemoryRegionProfileService();
        _payloadSetProfileService = CreateMockPayloadSetProfileService();

        var options = Options.Create(new JobManagerOptions { ProfilesPath = _testProfilesPath });
        var resourceCoordinator = Substitute.For<IResourceCoordinator>();

        _jobManager = new JobManager(
            options,
            NullLogger<JobManager>.Instance,
            resourceCoordinator,
            _serialProfileService,
            _socatProfileService,
            _powerSupplyProfileService,
            _memoryRegionProfileService
        );
    }

    [Fact]
    public async Task CreateExecutionJobAsync_ShouldPreserveCustomPowerTimings_FromJobProfile()
    {
        // Arrange - Create a job profile with custom power timing values
        const int customPowerOnTimeMs = 8000; // Non-default value (default is 5000)
        const int customPowerOffDelayMs = 3500; // Non-default value (default is 2000)

        var jobProfile = new JobProfile
        {
            Id = 100,
            Name = "Custom Timing Test Job",
            Description = "Test job with custom power timing parameters",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps",
            PowerOnTimeMs = customPowerOnTimeMs,    // Custom value
            PowerOffDelayMs = customPowerOffDelayMs, // Custom value
            IsDefault = false,
            IsReadOnly = false
        };

        await _jobManager.CreateAsync(jobProfile);

        // Act - Create execution job from the profile
        Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify that the JobProfileSet contains the custom timing values
        Assert.NotNull(executionJob.ProfileSet);
        Assert.Equal(customPowerOnTimeMs, executionJob.ProfileSet.PowerOnTimeMs);
        Assert.Equal(customPowerOffDelayMs, executionJob.ProfileSet.PowerOffDelayMs);
    }

    [Fact]
    public async Task CreateExecutionJobAsync_ShouldPreserveSerialConfiguration_FromProfile()
    {
        // Arrange - Create a job profile referencing a specific serial profile
        var jobProfile = new JobProfile
        {
            Id = 101,
            Name = "Serial Config Test Job",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps",
            PowerOnTimeMs = 5000,
            PowerOffDelayMs = 2000
        };

        await _jobManager.CreateAsync(jobProfile);

        // Act
        Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify serial configuration is from the profile, not defaults
        Assert.NotNull(executionJob.ProfileSet.Serial.Configuration);
        Assert.Equal(115200, executionJob.ProfileSet.Serial.Configuration.BaudRate);
        Assert.Equal(ParityMode.None, executionJob.ProfileSet.Serial.Configuration.Parity);
        Assert.Equal(8, executionJob.ProfileSet.Serial.Configuration.CharacterSize);
        Assert.Equal(StopBits.One, executionJob.ProfileSet.Serial.Configuration.StopBits);
    }

    [Fact]
    public async Task CreateExecutionJobAsync_ShouldPreserveSocatConfiguration_FromProfile()
    {
        // Arrange
        var jobProfile = new JobProfile
        {
            Id = 102,
            Name = "Socat Config Test Job",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps"
        };

        await _jobManager.CreateAsync(jobProfile);

        // Act
        Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify socat configuration is from the profile
        Assert.NotNull(executionJob.ProfileSet.Socat.Configuration);
        Assert.Equal(8080, executionJob.ProfileSet.Socat.Configuration.TcpPort);
        Assert.True(executionJob.ProfileSet.Socat.Configuration.EnableFork);
        Assert.True(executionJob.ProfileSet.Socat.Configuration.EnableReuseAddr);
    }

    [Fact]
    public async Task CreateExecutionJobAsync_ShouldPreservePowerConfiguration_FromProfile()
    {
        // Arrange
        var jobProfile = new JobProfile
        {
            Id = 103,
            Name = "Power Config Test Job",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps"
        };

        await _jobManager.CreateAsync(jobProfile);

        // Act
        Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify power configuration is from the profile
        Assert.NotNull(executionJob.ProfileSet.Power.Configuration);
        var modbusTcp = executionJob.ProfileSet.Power.Configuration as ModbusTcpConfiguration;
        Assert.NotNull(modbusTcp);
        Assert.Equal("192.168.1.100", modbusTcp.Host);
        Assert.Equal(502, modbusTcp.Port);
        Assert.Equal((ushort)1, modbusTcp.DeviceId);
        Assert.Equal((ushort)0, modbusTcp.OnOffCoil);
    }

    [Fact]
    public async Task CreateExecutionJobAsync_WithDefaultValues_ShouldNotOverwriteWithHardcodedDefaults()
    {
        // Arrange - Create a job with default values explicitly set
        var jobProfile = new JobProfile
        {
            Id = 105,
            Name = "Default Values Test Job",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps",
            PowerOnTimeMs = 5000,  // Default value
            PowerOffDelayMs = 2000  // Default value
        };
        await _jobManager.CreateAsync(jobProfile);

        // Act
        Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify that default values are preserved
        Assert.Equal(5000, executionJob.ProfileSet.PowerOnTimeMs);
        Assert.Equal(2000, executionJob.ProfileSet.PowerOffDelayMs);
    }

    [Fact]
    public async Task CreateExecutionJobAsync_ShouldNotModifyOriginalJobProfile()
    {
        // Arrange
        const int originalPowerOnTime = 7000;
        const int originalPowerOffDelay = 3000;

        var jobProfile = new JobProfile
        {
            Id = 104,
            Name = "Immutability Test Job",
            SerialProfileId = 1,
            SerialDevice = "/dev/ttyUSB0",
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegionProfileId = 1,
            Payloads = PayloadSetProfile.CreateDefault(),
            OutputPath = "/tmp/dumps",
            PowerOnTimeMs = originalPowerOnTime,
            PowerOffDelayMs = originalPowerOffDelay
        };

        await _jobManager.CreateAsync(jobProfile);

        // Act
        _ = await _jobManager.CreateExecutionJobAsync(jobProfile);

        // Assert - Verify original profile is unchanged
        Assert.Equal(originalPowerOnTime, jobProfile.PowerOnTimeMs);
        Assert.Equal(originalPowerOffDelay, jobProfile.PowerOffDelayMs);
    }

    public void Dispose()
    {
        _jobManager?.Dispose();

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region Mock Service Helpers

    private static ISerialPortProfileService CreateMockSerialProfileService()
    {
        var service = Substitute.For<ISerialPortProfileService>();
        var serialProfile = new SerialPortProfile
        {
            Id = 1,
            Name = "Test Serial Profile",
            Configuration = new SerialPortConfiguration
            {
                BaudRate = 115200,
                Parity = ParityMode.None,
                CharacterSize = 8,
                StopBits = StopBits.One,
                RawMode = true,
                DisableEcho = true
            }
        };

        service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(serialProfile);
        return service;
    }

    private static ISocatProfileService CreateMockSocatProfileService()
    {
        var service = Substitute.For<ISocatProfileService>();
        var socatProfile = new SocatProfile
        {
            Id = 1,
            Name = "Test Socat Profile",
            Configuration = new SocatConfiguration
            {
                TcpPort = 8080,
                Verbose = false,
                EnableFork = true,
                EnableReuseAddr = true,
                BaudRate = 115200
            }
        };

        service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(socatProfile);
        return service;
    }

    private static IPowerSupplyProfileService CreateMockPowerSupplyProfileService()
    {
        var service = Substitute.For<IPowerSupplyProfileService>();
        var powerProfile = new PowerSupplyProfile
        {
            Id = 1,
            Name = "Test Power Profile",
            Configuration = new ModbusTcpConfiguration
            {
                Host = "192.168.1.100",
                Port = 502,
                DeviceId = 1,
                OnOffCoil = 0,
                AddressingMode = ModbusAddressingMode.Base0
            }
        };

        service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(powerProfile);
        return service;
    }

    private static IMemoryRegionProfileService CreateMockMemoryRegionProfileService()
    {
        var service = Substitute.For<IMemoryRegionProfileService>();
        var memoryProfile = new MemoryMappingProfile
        {
            Id = 1,
            Name = "Test Memory Profile",
            Segments = new List<MemorySegment>
            {
                new MemorySegment
                {
                    Name = "User Memory",
                    StartAddress = $"0x{MemoryConstants.DefaultUserMemoryStart:X8}",
                    Size = MemoryConstants.DefaultDumpSize,
                    IsSelected = true
                }
            }
        };

        service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(memoryProfile);
        return service;
    }

    private static IPayloadSetProfileService CreateMockPayloadSetProfileService()
    {
        var service = Substitute.For<IPayloadSetProfileService>();
        var payloadProfile = PayloadSetProfile.CreateDefault();
        payloadProfile.Id = 1;

        service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(payloadProfile);
        return service;
    }

    #endregion
}
