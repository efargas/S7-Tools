using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Bootloader;
using Xunit;

namespace S7Tools.Tests.Services.Bootloader;

public class OptimizationVerificationTests
{
    private class TestBootloaderService : BaseBootloaderService
    {
        public TestBootloaderService(ITimeProvider timeProvider) : base(timeProvider) { }

        public new Task<List<byte[]>> PerformDumpProcessAsync(
            IPlcClient client,
            JobProfileSet profiles,
            IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
            ILogger logger,
            double startPercent,
            double weight,
            CancellationToken cancellationToken)
        {
            return base.PerformDumpProcessAsync(client, profiles, progress, logger, startPercent, weight, cancellationToken);
        }

        public new Task<BootloaderResult> PerformDumpProcessStreamingAsync(
            IPlcClient client,
            JobProfileSet profiles,
            IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
            ILogger logger,
            double startPercent,
            double weight,
            Guid? taskId,
            CancellationToken cancellationToken)
        {
            return base.PerformDumpProcessStreamingAsync(client, profiles, progress, logger, startPercent, weight, taskId, cancellationToken);
        }
    }

    [Fact]
    public async Task PerformDumpProcessAsync_Segmented_ReturnsCorrectData()
    {
        // Arrange
        var timeProvider = Substitute.For<ITimeProvider>();
        timeProvider.GetUtcNow().Returns(DateTime.UtcNow);
        var service = new TestBootloaderService(timeProvider);
        var client = Substitute.For<IPlcClient>();

        var segments = new List<MemorySegment>
        {
            new MemorySegment { Name = "Seg1", StartAddress = "0x100", Size = 10, IsSelected = true },
            new MemorySegment { Name = "Seg2", StartAddress = "0x200", Size = 20, IsSelected = true }
        };
        var mapping = new MemoryMappingProfile { Segments = segments };

        var profiles = new JobProfileSet(
            new SerialProfileRef("COM1", 115200, "None", 8, "One", new SerialPortConfiguration(), "", ""),
            new SocatProfileRef(1234, true, new SocatConfiguration()),
            new PowerProfileRef("127.0.0.1", 502, 1, 1, new ModbusTcpConfiguration(), 1, ModbusAddressingMode.Base0),
            new MemoryRegionProfile("0x100", 10),
            new PayloadSetProfile { BasePath = "path" },
            "output",
            5000, 2000,
            mapping,
            1
        );

        byte[] data1 = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
        byte[] data2 = Enumerable.Range(10, 20).Select(i => (byte)i).ToArray();

        client.InvokeDumperAsync(0x100, 10, Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(data1);
        client.InvokeDumperAsync(0x200, 20, Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(data2);

        var progress = Substitute.For<IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)>>();

        // Act
        var result = await service.PerformDumpProcessAsync(client, profiles, progress, NullLogger.Instance, 0, 100, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(30, result[0].Length);
        Assert.Equal(data1.Concat(data2), result[0]);
    }

    [Fact]
    public async Task PerformDumpProcessStreamingAsync_Segmented_WritesCorrectFiles()
    {
        // Arrange
        var timeProvider = Substitute.For<ITimeProvider>();
        DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
        timeProvider.GetUtcNow().Returns(now);
        timeProvider.GetLocalNow().Returns(now);
        var service = new TestBootloaderService(timeProvider);
        var client = Substitute.For<IPlcClient>();

        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var segments = new List<MemorySegment>
            {
                new MemorySegment { Name = "Seg1", StartAddress = "0x100", Size = 10, IsSelected = true },
                new MemorySegment { Name = "Seg2", StartAddress = "0x200", Size = 20, IsSelected = true }
            };
            var mapping = new MemoryMappingProfile { Segments = segments };

            var profiles = new JobProfileSet(
                new SerialProfileRef("COM1", 115200, "None", 8, "One", new SerialPortConfiguration(), "", ""),
                new SocatProfileRef(1234, true, new SocatConfiguration()),
                new PowerProfileRef("127.0.0.1", 502, 1, 1, new ModbusTcpConfiguration(), 1, ModbusAddressingMode.Base0),
                new MemoryRegionProfile("0x100", 10),
                new PayloadSetProfile { BasePath = "path" },
                tempPath,
                5000, 2000,
                mapping,
                1
            );

            byte[] data1 = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
            byte[] data2 = Enumerable.Range(10, 20).Select(i => (byte)i).ToArray();

            client.InvokeDumperStreamAsync(0x100, 10, Arg.Any<Func<ReadOnlyMemory<byte>, ValueTask>>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<ILogger>())
                .Returns(async x => {
                    await ((Func<ReadOnlyMemory<byte>, ValueTask>)x[2])(data1);
                    ((IProgress<long>)x[3]).Report(10);
                });
            client.InvokeDumperStreamAsync(0x200, 20, Arg.Any<Func<ReadOnlyMemory<byte>, ValueTask>>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<ILogger>())
                .Returns(async x => {
                    await ((Func<ReadOnlyMemory<byte>, ValueTask>)x[2])(data2);
                    ((IProgress<long>)x[3]).Report(20);
                });

            var progress = Substitute.For<IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)>>();

            // Act
            var result = await service.PerformDumpProcessStreamingAsync(client, profiles, progress, NullLogger.Instance, 0, 100, null, CancellationToken.None);

            // Assert
            Assert.Single(result.SavedFiles);
            byte[] writtenData = File.ReadAllBytes(result.SavedFiles[0]);
            Assert.Equal(30, writtenData.Length);
            Assert.Equal(data1.Concat(data2), writtenData);
        }
        finally
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }
    }
}
