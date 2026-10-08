using System.Net.NetworkInformation;
using RigSpec.Core;
using RigSpec.Core.Interfaces;
using RigSpec.Core.Models;
using RigSpec.Core.Support;
using Microsoft.Extensions.Logging;

namespace RigSpec.Platform.Windows;

public sealed class WindowsHardwareService(ILogger<WindowsHardwareService> logger) : IHardwareService
{
    public string PlatformName => "Windows";

    public Task<HardwareSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var started = DateTime.UtcNow;
        logger.LogInformation("Starting Windows hardware detection");

        var os = QueryOs();
        var cpu = QueryCpu();
        var memory = QueryMemory();
        var motherboard = QueryMotherboard();
        var bios = QueryBios();
        var gpus = QueryGpus();
        var storage = QueryStorage();
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

        logger.LogInformation("Windows detection completed in {Ms} ms", snapshot.Duration.TotalMilliseconds);
        return Task.FromResult(snapshot);
    }

    private OperatingSystemInfo QueryOs()
    {
        try
        {
            var row = WmiQuery.Query(logger, "Win32_OperatingSystem",
                "Caption", "Version", "BuildNumber", "LastBootUpTime").FirstOrDefault();

            TimeSpan? uptime = null;
            var boot = WmiQuery.CimDate(row is null ? null : WmiQuery.String(row, "LastBootUpTime"));
            if (DateTime.TryParse(boot, out var bootDate))
            {
                uptime = DateTime.Now - bootDate;
            }
            else if (row is not null)
            {
                try
                {
                    var raw = WmiQuery.String(row, "LastBootUpTime");
                    if (raw is not null)
                    {
                        var dt = System.Management.ManagementDateTimeConverter.ToDateTime(raw);
                        uptime = DateTime.Now - dt;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not parse LastBootUpTime");
                }
            }

            return new OperatingSystemInfo
            {
                Name = DetectedValue<string>.FromString(row is null ? Environment.OSVersion.VersionString : WmiQuery.String(row, "Caption")),
                Version = DetectedValue<string>.FromString(row is null ? null : WmiQuery.String(row, "Version")),
                Build = DetectedValue<string>.FromString(row is null ? null : WmiQuery.String(row, "BuildNumber")),
                Kernel = DetectedValue<string>.FromString(Environment.OSVersion.Version.ToString()),
                Architecture = DetectedValue<string>.Detected(Environment.Is64BitOperatingSystem ? "x64" : "x86"),
                Hostname = DetectedValue<string>.FromString(Environment.MachineName),
                Uptime = uptime is null ? DetectedValue<TimeSpan>.Missing() : DetectedValue<TimeSpan>.Detected(uptime.Value),
                DesktopEnvironment = DetectedValue<string>.Detected("Windows")
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OS query failed");
            return new OperatingSystemInfo
            {
                Name = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message),
                Architecture = DetectedValue<string>.Detected(Environment.Is64BitOperatingSystem ? "x64" : "x86"),
                Hostname = DetectedValue<string>.FromString(Environment.MachineName)
            };
        }
    }

    private CpuInfo QueryCpu()
    {
        try
        {
            var row = WmiQuery.Query(logger, "Win32_Processor",
                "Name", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed", "Architecture").FirstOrDefault();

            if (row is null)
            {
                return new CpuInfo
                {
                    Name = DetectedValue<string>.Missing(Availability.Unavailable, "Win32_Processor returned no data"),
                    Architecture = DetectedValue<string>.FromString(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"))
                };
            }

            return new CpuInfo
            {
                Name = DetectedValue<string>.FromString(WmiQuery.String(row, "Name")),
                PhysicalCores = ToInt(WmiQuery.Int(row, "NumberOfCores")),
                LogicalProcessors = ToInt(WmiQuery.Int(row, "NumberOfLogicalProcessors")),
                Architecture = DetectedValue<string>.FromString(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")),
                MaxClockMhz = WmiQuery.Int(row, "MaxClockSpeed") is { } mhz
                    ? DetectedValue<double>.Detected(mhz)
                    : DetectedValue<double>.Missing(Availability.NotReported)
            };
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
            var system = WmiQuery.Query(logger, "Win32_ComputerSystem", "TotalPhysicalMemory").FirstOrDefault();
            var modules = WmiQuery.Query(logger, "Win32_PhysicalMemory", "Speed", "SMBIOSMemoryType", "ConfiguredClockSpeed");

            var type = DetectedValue<string>.Missing(Availability.NotReported);
            var speed = DetectedValue<int>.Missing(Availability.NotReported);
            if (modules.Count > 0)
            {
                var first = modules[0];
                speed = ToInt(WmiQuery.Int(first, "ConfiguredClockSpeed") ?? WmiQuery.Int(first, "Speed"));
                type = DetectedValue<string>.FromString(SmbiosMemoryType(WmiQuery.Int(first, "SMBIOSMemoryType")), Availability.NotReported);
            }

            return new MemoryInfo
            {
                InstalledBytes = system is null
                    ? DetectedValue<long>.Missing(Availability.Unavailable)
                    : ToLong(WmiQuery.Long(system, "TotalPhysicalMemory")),
                Type = type,
                SpeedMtPerSecond = speed,
                ModuleCount = DetectedValue<int>.Detected(modules.Count)
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
            var row = WmiQuery.Query(logger, "Win32_BaseBoard",
                "Manufacturer", "Product", "Version", "SerialNumber").FirstOrDefault();

            if (row is null)
            {
                return new MotherboardInfo
                {
                    Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, "Win32_BaseBoard returned no data")
                };
            }

            return new MotherboardInfo
            {
                Manufacturer = DetectedValue<string>.FromString(WmiQuery.String(row, "Manufacturer")),
                Product = DetectedValue<string>.FromString(WmiQuery.String(row, "Product")),
                Version = DetectedValue<string>.FromString(WmiQuery.String(row, "Version")),
                SerialNumber = DetectedValue<string>.FromString(WmiQuery.String(row, "SerialNumber"), Availability.NotReported)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Motherboard query failed");
            return new MotherboardInfo
            {
                Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message)
            };
        }
    }

    private BiosInfo QueryBios()
    {
        try
        {
            var row = WmiQuery.Query(logger, "Win32_BIOS",
                "Manufacturer", "SMBIOSBIOSVersion", "ReleaseDate").FirstOrDefault();

            if (row is null)
            {
                return new BiosInfo
                {
                    Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, "Win32_BIOS returned no data")
                };
            }

            return new BiosInfo
            {
                Manufacturer = DetectedValue<string>.FromString(WmiQuery.String(row, "Manufacturer")),
                Version = DetectedValue<string>.FromString(WmiQuery.String(row, "SMBIOSBIOSVersion")),
                ReleaseDate = DetectedValue<string>.FromString(WmiQuery.CimDate(WmiQuery.String(row, "ReleaseDate")))
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BIOS query failed");
            return new BiosInfo { Manufacturer = DetectedValue<string>.Missing(Availability.Unavailable, ex.Message) };
        }
    }

    private IReadOnlyList<GpuInfo> QueryGpus()
    {
        try
        {
            var rows = WmiQuery.Query(logger, "Win32_VideoController", "Name", "AdapterRAM");
            return rows.Select(row =>
            {
                var ram = WmiQuery.Long(row, "AdapterRAM");
                var memory = ram is > 0
                    ? DetectedValue<long>.Detected(ram.Value)
                    : DetectedValue<long>.Missing(Availability.NotReported, "AdapterRAM not reported or unreliable");

                return new GpuInfo
                {
                    Name = DetectedValue<string>.FromString(WmiQuery.String(row, "Name")),
                    MemoryBytes = memory
                };
            }).Where(g => g.Name.HasValue).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GPU query failed");
            return [];
        }
    }

    private IReadOnlyList<StorageInfo> QueryStorage()
    {
        try
        {
            var rows = WmiQuery.Query(logger, "Win32_DiskDrive", "Model", "MediaType", "Size", "InterfaceType");
            return rows.Select(row => new StorageInfo
            {
                Model = DetectedValue<string>.FromString(WmiQuery.String(row, "Model")),
                SizeBytes = ToLong(WmiQuery.Long(row, "Size")),
                MediaType = DetectedValue<string>.FromString(WmiQuery.String(row, "MediaType")),
                InterfaceType = DetectedValue<string>.FromString(WmiQuery.String(row, "InterfaceType"))
            }).ToList();
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
                .Where(n => n.NetworkInterfaceType is not NetworkInterfaceType.Loopback 
                            and not NetworkInterfaceType.Tunnel 
                            && !IsFilterDriver(n))
                .Select(n => new NetworkAdapterInfo
                {
                    Name = DetectedValue<string>.Detected(n.Name),
                    Description = DetectedValue<string>.FromString(n.Description),
                    MacAddress = DetectedValue<string>.FromString(FormatMac(n.GetPhysicalAddress()), Availability.NotReported),
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
            var rows = WmiQuery.Query(logger, "Win32_VideoController",
                "CurrentHorizontalResolution", "CurrentVerticalResolution", "CurrentRefreshRate", "Name");

            return rows.Select(row => new DisplayInfo
            {
                Name = DetectedValue<string>.FromString(WmiQuery.String(row, "Name")),
                Width = ToInt(WmiQuery.Int(row, "CurrentHorizontalResolution")),
                Height = ToInt(WmiQuery.Int(row, "CurrentVerticalResolution")),
                RefreshRateHz = ToInt(WmiQuery.Int(row, "CurrentRefreshRate"))
            }).Where(d => d.Width.HasValue && d.Height.HasValue).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Display query failed");
            return [];
        }
    }

    private static DetectedValue<int> ToInt(int? value) =>
        value is null ? DetectedValue<int>.Missing() : DetectedValue<int>.Detected(value.Value);

    private static DetectedValue<long> ToLong(long? value) =>
        value is null ? DetectedValue<long>.Missing() : DetectedValue<long>.Detected(value.Value);

    private static string? FormatMac(PhysicalAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 0 ? null : BitConverter.ToString(bytes);
    }

    private static string? SmbiosMemoryType(int? type) => type switch
    {
        20 => "DDR",
        21 => "DDR2",
        24 => "DDR3",
        26 => "DDR4",
        34 => "DDR5",
        _ => null
    };

    private static bool IsFilterDriver(NetworkInterface n)
    {
        var desc = n.Description;
        var name = n.Name;
        return desc.Contains("LightWeight Filter", StringComparison.OrdinalIgnoreCase)
            || desc.Contains("Packet Scheduler", StringComparison.OrdinalIgnoreCase)
            || desc.Contains("Filter Driver", StringComparison.OrdinalIgnoreCase)
            || desc.Contains("Kernel Debugger", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Kernel Debugger", StringComparison.OrdinalIgnoreCase)
            || desc.Contains("NDIS Light-Weight", StringComparison.OrdinalIgnoreCase);
    }
}
