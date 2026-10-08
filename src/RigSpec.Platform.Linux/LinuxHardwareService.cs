using System.Net.NetworkInformation;
using RigSpec.Core;
using RigSpec.Core.Interfaces;
using RigSpec.Core.Models;
using RigSpec.Core.Support;
using Microsoft.Extensions.Logging;

namespace RigSpec.Platform.Linux;

public sealed class LinuxHardwareService(ILogger<LinuxHardwareService> logger) : IHardwareService
{
    public string PlatformName => "Linux";

    public async Task<HardwareSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        logger.LogInformation("Starting Linux hardware detection");

        var os = QueryOs();
        var cpu = await QueryCpuAsync(cancellationToken).ConfigureAwait(false);
        var memory = QueryMemory();
        var motherboard = QueryMotherboard();
        var bios = QueryBios();
        var gpus = await QueryGpusAsync(cancellationToken).ConfigureAwait(false);
        var storage = await QueryStorageAsync(cancellationToken).ConfigureAwait(false);
        var network = QueryNetwork();
        var displays = QueryDisplays();

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
            Displays = displays,
            ComponentStatuses = SnapshotBuilder.BuildStatuses(os, cpu, memory, motherboard, bios, gpus, storage, network, displays)
        };

        logger.LogInformation("Linux detection completed in {Ms} ms", snapshot.Duration.TotalMilliseconds);
        return snapshot;
    }

    private OperatingSystemInfo QueryOs()
    {
        try
        {
            var osRelease = LinuxParsers.ParseKeyValues(SafeTextFile.Read("/etc/os-release"));
            osRelease.TryGetValue("PRETTY_NAME", out var pretty);
            osRelease.TryGetValue("VERSION_ID", out var version);
            var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")
                          ?? Environment.GetEnvironmentVariable("DESKTOP_SESSION");

            return new OperatingSystemInfo
            {
                Name = DetectedValue<string>.FromString(pretty ?? "Linux"),
                Version = DetectedValue<string>.FromString(version),
                Kernel = DetectedValue<string>.FromString(Environment.OSVersion.VersionString),
                Architecture = DetectedValue<string>.FromString(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()),
                Hostname = DetectedValue<string>.FromString(Environment.MachineName),
                Uptime = LinuxParsers.ParseUptime(SafeTextFile.Read("/proc/uptime")),
                DesktopEnvironment = DetectedValue<string>.FromString(desktop)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OS query failed");
            return new OperatingSystemInfo { Name = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private async Task<CpuInfo> QueryCpuAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cpuinfo = SafeTextFile.Read("/proc/cpuinfo");
            var lscpu = await ExternalProcess.RunAsync("lscpu", [], cancellationToken: cancellationToken).ConfigureAwait(false);
            var cpu = LinuxParsers.ParseCpu(cpuinfo, lscpu);
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

            return cpu;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CPU query failed");
            return new CpuInfo { Name = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private MemoryInfo QueryMemory()
    {
        try
        {
            return new MemoryInfo
            {
                InstalledBytes = LinuxParsers.ParseMemTotalBytes(SafeTextFile.Read("/proc/meminfo")),
                Type = DetectedValue<string>.Missing(Availability.NotReported, "not exposed in /proc/meminfo"),
                SpeedMtPerSecond = DetectedValue<int>.Missing(Availability.NotReported)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Memory query failed");
            return new MemoryInfo { InstalledBytes = DetectedValue<long>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private MotherboardInfo QueryMotherboard()
    {
        try
        {
            return new MotherboardInfo
            {
                Manufacturer = ReadDmi("board_vendor"),
                Product = ReadDmi("board_name"),
                Version = ReadDmi("board_version"),
                SerialNumber = ReadDmi("board_serial", Availability.PermissionRequired)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Motherboard query failed");
            return new MotherboardInfo { Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private BiosInfo QueryBios()
    {
        try
        {
            return new BiosInfo
            {
                Manufacturer = ReadDmi("bios_vendor"),
                Version = ReadDmi("bios_version"),
                ReleaseDate = ReadDmi("bios_date")
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BIOS query failed");
            return new BiosInfo { Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private async Task<IReadOnlyList<GpuInfo>> QueryGpusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var nvidia = await ExternalProcess.RunAsync(
                "nvidia-smi",
                ["--query-gpu=name,memory.total", "--format=csv,noheader"],
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var nvidiaGpus = LinuxParsers.ParseNvidiaSmi(nvidia);
            if (nvidiaGpus.Count > 0)
            {
                return nvidiaGpus;
            }

            var lspci = await ExternalProcess.RunAsync("lspci", [], cancellationToken: cancellationToken).ConfigureAwait(false);
            return LinuxParsers.ParseLspci(lspci);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GPU query failed");
            return [];
        }
    }

    private async Task<IReadOnlyList<StorageInfo>> QueryStorageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = await ExternalProcess.RunAsync(
                "lsblk",
                ["-J", "-b", "-o", "NAME,MODEL,SIZE,TYPE,TRAN,ROTA"],
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var parsed = LinuxParsers.ParseLsblkJson(json);
            if (parsed.Count > 0)
            {
                return parsed;
            }

            return ReadSysBlock();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Storage query failed");
            return [];
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

    private IReadOnlyList<DisplayInfo> QueryDisplays()
    {
        try
        {
            var drm = "/sys/class/drm";
            if (!Directory.Exists(drm))
            {
                return [];
            }

            var displays = new List<DisplayInfo>();
            foreach (var dir in Directory.GetDirectories(drm))
            {
                var name = Path.GetFileName(dir);
                if (!name.Contains("HDMI", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("DP", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("eDP", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("DVI", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("VGA", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var status = SafeTextFile.Read(Path.Combine(dir, "status"));
                var modes = SafeTextFile.ReadLines(Path.Combine(dir, "modes")).FirstOrDefault();
                int? w = null, h = null;
                if (modes is not null)
                {
                    var parts = modes.Split('x');
                    if (parts.Length == 2)
                    {
                        w = int.TryParse(parts[0], out var pw) ? pw : null;
                        h = int.TryParse(parts[1], out var ph) ? ph : null;
                    }
                }

                displays.Add(new DisplayInfo
                {
                    Name = DetectedValue<string>.Detected(name),
                    Width = w is null ? DetectedValue<int>.Missing() : DetectedValue<int>.Detected(w.Value),
                    Height = h is null ? DetectedValue<int>.Missing() : DetectedValue<int>.Detected(h.Value),
                    RefreshRateHz = DetectedValue<int>.Missing(Availability.NotReported, status)
                });
            }

            return displays;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Display query failed");
            return [];
        }
    }

    private static DetectedValue<string> ReadDmi(string file, Availability ifMissing = Availability.Unknown)
    {
        var path = $"/sys/devices/virtual/dmi/id/{file}";
        try
        {
            if (!File.Exists(path))
            {
                return DetectedValue<string>.Missing(Availability.UnsupportedOnPlatform);
            }

            var value = SafeTextFile.Read(path);
            return DetectedValue<string>.FromString(value, ifMissing);
        }
        catch (UnauthorizedAccessException)
        {
            return DetectedValue<string>.Missing(Availability.PermissionRequired, path);
        }
    }

    private static IReadOnlyList<StorageInfo> ReadSysBlock()
    {
        const string root = "/sys/block";
        if (!Directory.Exists(root))
        {
            return [];
        }

        var disks = new List<StorageInfo>();
        foreach (var dir in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith("loop", StringComparison.Ordinal) ||
                name.StartsWith("ram", StringComparison.Ordinal) ||
                name.StartsWith("sr", StringComparison.Ordinal))
            {
                continue;
            }

            var sectors = SafeTextFile.Read(Path.Combine(dir, "size"));
            var model = SafeTextFile.Read(Path.Combine(dir, "device", "model"));
            disks.Add(new StorageInfo
            {
                Model = DetectedValue<string>.FromString(string.IsNullOrWhiteSpace(model) ? name : model),
                SizeBytes = long.TryParse(sectors, out var s)
                    ? DetectedValue<long>.Detected(s * 512)
                    : DetectedValue<long>.Missing(),
                MediaType = DetectedValue<string>.Missing(Availability.NotReported),
                InterfaceType = DetectedValue<string>.Missing(Availability.NotReported)
            });
        }

        return disks;
    }
}
