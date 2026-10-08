using RigSpec.Core;
using RigSpec.Core.Interfaces;
using RigSpec.Core.Models;
using RigSpec.Core.Support;
using Microsoft.Extensions.Logging;

namespace RigSpec.Infrastructure;

public sealed class UnsupportedHardwareService(ILogger<UnsupportedHardwareService> logger, string platform) : IHardwareService
{
    public string PlatformName => platform;

    public Task<HardwareSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Platform {Platform} is not supported", platform);
        var reason = $"Unsupported on this platform ({platform})";
        var os = new OperatingSystemInfo
        {
            Name = DetectedValue<string>.FromString(platform),
            Hostname = DetectedValue<string>.FromString(Environment.MachineName),
            Architecture = DetectedValue<string>.FromString(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString())
        };
        var cpu = new CpuInfo { Name = DetectedValue<string>.Missing(Availability.UnsupportedOnPlatform, reason) };
        var memory = new MemoryInfo { InstalledBytes = DetectedValue<long>.Missing(Availability.UnsupportedOnPlatform, reason) };
        var motherboard = new MotherboardInfo { Manufacturer = DetectedValue<string>.Missing(Availability.UnsupportedOnPlatform, reason) };
        var bios = new BiosInfo { Manufacturer = DetectedValue<string>.Missing(Availability.UnsupportedOnPlatform, reason) };

        return Task.FromResult(new HardwareSnapshot
        {
            Platform = platform,
            CapturedAt = DateTimeOffset.Now,
            Duration = TimeSpan.Zero,
            OperatingSystem = os,
            Cpu = cpu,
            Memory = memory,
            Motherboard = motherboard,
            Bios = bios,
            ComponentStatuses = SnapshotBuilder.BuildStatuses(os, cpu, memory, motherboard, bios, [], [], [], [])
        });
    }
}
