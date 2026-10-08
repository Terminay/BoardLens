using System.Net.NetworkInformation;
using RigSpec.Core;
using RigSpec.Core.Interfaces;
using RigSpec.Core.Models;
using RigSpec.Core.Support;
using Microsoft.Extensions.Logging;

namespace RigSpec.Platform.MacOS;

public sealed class MacOSHardwareService(ILogger<MacOSHardwareService> logger) : IHardwareService
{
    public string PlatformName => "macOS";

    public async Task<HardwareSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        logger.LogInformation("Starting macOS hardware detection");

        var software = await ExternalProcess.RunAsync("system_profiler", ["SPSoftwareDataType"], cancellationToken: cancellationToken).ConfigureAwait(false);
        var hardware = await ExternalProcess.RunAsync("system_profiler", ["SPHardwareDataType"], cancellationToken: cancellationToken).ConfigureAwait(false);
        var displays = await ExternalProcess.RunAsync("system_profiler", ["SPDisplaysDataType"], cancellationToken: cancellationToken).ConfigureAwait(false);
        var storageText = await ExternalProcess.RunAsync("system_profiler", ["SPStorageDataType"], cancellationToken: cancellationToken).ConfigureAwait(false);

        var os = QueryOs(software);
        var cpu = MacHardwareParsers.ParseHardware(hardware);
        if (!cpu.Architecture.HasValue)
        {
            cpu = new CpuInfo
            {
                Name = cpu.Name,
                PhysicalCores = cpu.PhysicalCores,
                LogicalProcessors = cpu.LogicalProcessors,
                Architecture = DetectedValue<string>.FromString(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()),
                MaxClockMhz = cpu.MaxClockMhz,
                Sockets = cpu.Sockets,
                ThreadsPerCore = cpu.ThreadsPerCore
            };
        }

        var memory = new MemoryInfo
        {
            InstalledBytes = MacHardwareParsers.ParseMemoryBytes(hardware),
            Type = DetectedValue<string>.Missing(Availability.NotReported)
        };

        var motherboard = new MotherboardInfo
        {
            Product = DetectedValue<string>.FromString(MacHardwareParsers.Value(hardware, "Model Name")),
            Manufacturer = DetectedValue<string>.Detected("Apple"),
            Version = DetectedValue<string>.FromString(MacHardwareParsers.Value(hardware, "Model Identifier"))
        };

        var bios = new BiosInfo
        {
            Manufacturer = DetectedValue<string>.Detected("Apple"),
            Version = DetectedValue<string>.FromString(MacHardwareParsers.Value(hardware, "Boot ROM Version"), Availability.NotReported)
        };

        var gpus = MacHardwareParsers.ParseDisplays(displays);
        var storage = MacHardwareParsers.ParseStorage(storageText);
        var network = QueryNetwork();
        var displayInfo = QueryDisplays(displays);

        var snapshot = new HardwareSnapshot
        {
            Platform = PlatformName,
            CapturedAt = DateTimeOffset.Now,
            Duration = DateTime.UtcNow - started,
            OperatingSystem = os,
            Cpu = cpu,
            Memory = memory,
            Motherboard = motherboard,
            Bios = bios,
            Gpus = gpus,
            Storage = storage,
            NetworkAdapters = network,
            Displays = displayInfo,
            ComponentStatuses = SnapshotBuilder.BuildStatuses(os, cpu, memory, motherboard, bios, gpus, storage, network, displayInfo)
        };

        logger.LogInformation("macOS detection completed in {Ms} ms", snapshot.Duration.TotalMilliseconds);
        return snapshot;
    }

    private OperatingSystemInfo QueryOs(string software)
    {
        try
        {
            return new OperatingSystemInfo
            {
                Name = DetectedValue<string>.FromString(MacHardwareParsers.Value(software, "System Version") is { Length: > 0 } v ? v : "macOS"),
                Architecture = DetectedValue<string>.FromString(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()),
                Hostname = DetectedValue<string>.FromString(Environment.MachineName),
                Kernel = DetectedValue<string>.FromString(Environment.OSVersion.VersionString)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OS query failed");
            return new OperatingSystemInfo { Name = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private IReadOnlyList<NetworkAdapterInfo> QueryNetwork()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
                .Select(n => new NetworkAdapterInfo
                {
                    Name = DetectedValue<string>.Detected(n.Name),
                    Description = DetectedValue<string>.FromString(n.Description),
                    MacAddress = DetectedValue<string>.FromString(
                        n.GetPhysicalAddress().GetAddressBytes().Length == 0
                            ? null
                            : BitConverter.ToString(n.GetPhysicalAddress().GetAddressBytes()),
                        Availability.NotReported),
                    IsUp = DetectedValue<bool>.Detected(n.OperationalStatus == OperationalStatus.Up)
                }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Network query failed");
            return [];
        }
    }

    private static IReadOnlyList<DisplayInfo> QueryDisplays(string displays)
    {
        var names = System.Text.RegularExpressions.Regex.Matches(displays, @"Resolution:\s*(\d+)\s*x\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (names.Count == 0)
        {
            return [];
        }

        return names.Select(m => new DisplayInfo
        {
            Name = DetectedValue<string>.Detected("Display"),
            Width = DetectedValue<int>.Detected(int.Parse(m.Groups[1].Value)),
            Height = DetectedValue<int>.Detected(int.Parse(m.Groups[2].Value))
        }).ToList();
    }
}
