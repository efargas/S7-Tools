using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Services.Bootloader;
using Xunit;

namespace S7Tools.Tests.Services.Bootloader;

public class OptimizationVerificationTests
{
    private BootloaderService CreateService(ITimeProvider timeProvider)
    {
        return new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            Substitute.For<IPayloadProvider>(),
            Substitute.For<ISocatService>(),
            Substitute.For<IPowerSupplyService>(),
            Substitute.For<ISerialPortService>(),
            (profiles) => Substitute.For<IPlcClient>(),
            Substitute.For<IResourceCoordinator>(),
            timeProvider,
            null
        );
    }

    private async Task<List<byte[]>> InvokePerformDumpProcessAsync(
        BootloaderService service,
        IPlcClient client,
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger logger,
        double startPercent,
        double weight,
        CancellationToken cancellationToken)
    {
        MethodInfo? method = typeof(BootloaderService).GetMethod("PerformDumpProcessAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<List<byte[]>>)method!.Invoke(service, new object[] { client, profiles, progress, logger, startPercent, weight, cancellationToken })!;
        return await task;
    }

    private async Task<BootloaderResult> InvokePerformDumpProcessStreamingAsync(
        BootloaderService service,
        IPlcClient client,
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger logger,
        ILogger? processLogger,
        double startPercent,
        double weight,
        Guid? taskId,
        CancellationToken cancellationToken)
    {
        MethodInfo? method = typeof(BootloaderService).GetMethod("PerformDumpProcessStreamingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<BootloaderResult>)method!.Invoke(service, new object[] { client, profiles, progress, logger, processLogger, startPercent, weight, taskId, cancellationToken })!;
        return await task;
    }

    [Fact]
    public async Task PerformDumpProcessAsync_Segmented_ReturnsCorrectData()
    {
        // Arrange
        var timeProvider = Substitute.For<ITimeProvider>();
        timeProvider.GetUtcNow().Returns(DateTime.UtcNow);
        var service = CreateService(timeProvider);
        var client = Substitute.For<IPlcClient>();

        var segments = new List<MemorySegment>
        {
            new MemorySegment { Name = "Seg1", StartAddress = "0x100", Size = 10, IsSelected = true },
            new MemorySegment { Name = "Seg2", StartAddress = "0x200", Size = 20, IsSelected = true }
        };
        var mapping = new MemoryMappingProfile { Segments = segments };

        var profiles = new JobProfileSet(
            new SerialProfileRef("COM1", 115200, "None", 8, "One", new SerialPortConfiguration()),
            new SocatProfileRef(1234, true, new SocatConfiguration()),
            new PowerProfileRef("127.0.0.1", 502, 1, 1, new S7Tools.Core.Models.ModbusTcpConfiguration()),
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
        var result = await InvokePerformDumpProcessAsync(service, client, profiles, progress, NullLogger.Instance, 0, 100, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Length.Should().Be(30);
        result[0].Should().BeEquivalentTo(data1.Concat(data2));
    }

    [Fact]
    public async Task PerformDumpProcessStreamingAsync_Segmented_WritesCorrectFiles()
    {
        // Arrange
        var timeProvider = Substitute.For<ITimeProvider>();
        DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
        timeProvider.GetUtcNow().Returns(now);
        timeProvider.GetLocalNow().Returns(now);
        var service = CreateService(timeProvider);
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
                new SerialProfileRef("COM1", 115200, "None", 8, "One", new SerialPortConfiguration()),
                new SocatProfileRef(1234, true, new SocatConfiguration()),
                new PowerProfileRef("127.0.0.1", 502, 1, 1, new S7Tools.Core.Models.ModbusTcpConfiguration()),
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
            var result = await InvokePerformDumpProcessStreamingAsync(service, client, profiles, progress, NullLogger.Instance, null, 0, 100, null, CancellationToken.None);

            // Assert
            result.SavedFiles.Should().ContainSingle();
            byte[] writtenData = File.ReadAllBytes(result.SavedFiles[0]);
            writtenData.Length.Should().Be(30);
            writtenData.Should().BeEquivalentTo(data1.Concat(data2));
        }
        finally
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task PerformDumpProcessStreamingAsync_WhenIterationFails_RenamesFileToPartialBinAndThrowsPartialDumpException()
    {
        // Arrange
        var timeProvider = Substitute.For<ITimeProvider>();
        DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
        timeProvider.GetUtcNow().Returns(now);
        timeProvider.GetLocalNow().Returns(now);
        var service = CreateService(timeProvider);
        var client = Substitute.For<IPlcClient>();

        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var segments = new List<MemorySegment>
            {
                new MemorySegment { Name = "Seg1", StartAddress = "0x100", Size = 10, IsSelected = true }
            };
            var mapping = new MemoryMappingProfile { Segments = segments };

            var profiles = new JobProfileSet(
                new SerialProfileRef("COM1", 115200, "None", 8, "One", new SerialPortConfiguration()),
                new SocatProfileRef(1234, true, new SocatConfiguration()),
                new PowerProfileRef("127.0.0.1", 502, 1, 1, new S7Tools.Core.Models.ModbusTcpConfiguration()),
                new MemoryRegionProfile("0x100", 10),
                new PayloadSetProfile { BasePath = "path" },
                tempPath,
                5000, 2000,
                mapping,
                1
            );

            // Write 5 bytes then throw to simulate a partial dump
            byte[] partialData = Enumerable.Range(0, 5).Select(i => (byte)i).ToArray();
            var dumpError = new InvalidOperationException("Simulated connection loss mid-dump");

            client.InvokeDumperStreamAsync(
                    0x100, 10,
                    Arg.Any<Func<ReadOnlyMemory<byte>, ValueTask>>(),
                    Arg.Any<IProgress<long>>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<bool>(),
                    Arg.Any<ILogger>())
                .Returns(async x =>
                {
                    var callback = (Func<ReadOnlyMemory<byte>, ValueTask>)x[2];
                    await callback(partialData);         // Write 5 bytes
                    ((IProgress<long>)x[3]).Report(5);
                    throw dumpError;                     // Then fail
                });

            var progress = Substitute.For<IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)>>();

            // Act
            PartialDumpException? caughtEx = null;
            try
            {
                await InvokePerformDumpProcessStreamingAsync(
                    service, client, profiles, progress, NullLogger.Instance, null, 0, 100, null, CancellationToken.None);
            }
            catch (PartialDumpException pde)
            {
                caughtEx = pde;
            }

            // Assert: PartialDumpException was thrown
            caughtEx.Should().NotBeNull("PerformDumpProcessStreamingAsync must throw PartialDumpException when iteration fails with partial data");
            caughtEx!.PartialResult.Should().NotBeNull();
            caughtEx.PartialResult.SavedFiles.Should().ContainSingle("exactly one partial file should be recorded");

            // The file must have been renamed to .partial.bin
            string? savedPath = caughtEx.PartialResult.SavedFiles[0];
            savedPath.Should().EndWith(".partial.bin", "partial file must use .partial.bin extension");
            File.Exists(savedPath).Should().BeTrue("partial dump file must exist on disk");

            // The partial file must contain the bytes written before the failure
            byte[] savedBytes = File.ReadAllBytes(savedPath!);
            savedBytes.Should().BeEquivalentTo(partialData, "partial file must contain the bytes streamed before the error");

            // Inner exception must be the original error (not OperationCanceledException)
            caughtEx.InnerException.Should().Be(dumpError);
        }
        finally
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }
    }
}
