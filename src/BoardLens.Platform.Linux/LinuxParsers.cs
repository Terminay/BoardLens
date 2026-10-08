using System.Globalization;
using BoardLens.Core;
using BoardLens.Core.Models;

namespace BoardLens.Platform.Linux;

public static class LinuxParsers
{
    public static Dictionary<string, string> ParseKeyValues(string text, char separator = '=')
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            var index = line.IndexOf(separator);
            if (index <= 0)
            {
                continue;
            }

            var key = line[..index].Trim().Trim('"');
            var value = line[(index + 1)..].Trim().Trim('"');
            if (!map.ContainsKey(key))
            {
                map[key] = value;
            }
        }

        return map;
    }

    public static CpuInfo ParseCpu(string cpuinfo, string lscpu)
    {
        var info = ParseKeyValues(cpuinfo, ':');
        var lscpuMap = ParseKeyValues(lscpu, ':');

        var logical = lscpuMap.TryGetValue("CPU(s)", out var cpuCount)
            ? DetectedValue<int>.FromInt(cpuCount)
            : CountProcessors(cpuinfo);

        int? cores = null;
        if (lscpuMap.TryGetValue("Core(s) per socket", out var cps) &&
            lscpuMap.TryGetValue("Socket(s)", out var sockets) &&
            int.TryParse(cps, out var coresPerSocket) &&
            int.TryParse(sockets, out var socketCount))
        {
            cores = coresPerSocket * socketCount;
        }

        var mhz = First(lscpuMap, "CPU max MHz", "CPU MHz") ?? (info.TryGetValue("cpu MHz", out var fallback) ? fallback : null);

        return new CpuInfo
        {
            Name = DetectedValue<string>.FromString(First(lscpuMap, "Model name") ?? (info.TryGetValue("model name", out var n) ? n : null)),
            PhysicalCores = cores is null ? DetectedValue<int>.Missing() : DetectedValue<int>.Detected(cores.Value),
            LogicalProcessors = logical,
            Architecture = DetectedValue<string>.FromString(First(lscpuMap, "Architecture")),
            MaxClockMhz = DetectedValue<double>.FromDouble(mhz),
            Sockets = DetectedValue<int>.FromInt(First(lscpuMap, "Socket(s)")),
            ThreadsPerCore = DetectedValue<int>.FromInt(First(lscpuMap, "Thread(s) per core"))
        };
    }

    public static DetectedValue<long> ParseMemTotalBytes(string meminfo)
    {
        foreach (var line in meminfo.Split('\n'))
        {
            if (!line.StartsWith("MemTotal:", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
            {
                return DetectedValue<long>.Detected(kb * 1024);
            }
        }

        return DetectedValue<long>.Missing();
    }

    public static IReadOnlyList<GpuInfo> ParseLspci(string lspci)
    {
        var gpus = new List<GpuInfo>();
        foreach (var line in lspci.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("VGA compatible controller", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("3D controller", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Display controller", StringComparison.OrdinalIgnoreCase))
            {
                var name = System.Text.RegularExpressions.Regex.Replace(line, @"^[0-9a-fA-F:.]+\s+", "");
                gpus.Add(new GpuInfo
                {
                    Name = DetectedValue<string>.FromString(name),
                    MemoryBytes = DetectedValue<long>.Missing(Availability.NotReported)
                });
            }
        }

        return gpus;
    }

    public static IReadOnlyList<GpuInfo> ParseNvidiaSmi(string csv)
    {
        var gpus = new List<GpuInfo>();
        foreach (var line in csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            gpus.Add(new GpuInfo
            {
                Name = DetectedValue<string>.FromString(parts[0]),
                MemoryBytes = ParseNvidiaMemory(parts[1])
            });
        }

        return gpus;
    }

    public static IReadOnlyList<StorageInfo> ParseLsblkJson(string json)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("blockdevices", out var devices))
            {
                return [];
            }

            var result = new List<StorageInfo>();
            foreach (var device in devices.EnumerateArray())
            {
                var type = device.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (!string.Equals(type, "disk", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = device.TryGetProperty("name", out var n) ? n.GetString() : null;
                var rota = device.TryGetProperty("rota", out var r) ? r.ToString() : null;
                var media = rota switch
                {
                    "1" or "true" => "HDD",
                    "0" or "false" when name?.StartsWith("nvme", StringComparison.OrdinalIgnoreCase) == true => "NVMe SSD",
                    "0" or "false" => "SSD",
                    _ => null
                };

                result.Add(new StorageInfo
                {
                    Model = DetectedValue<string>.FromString(device.TryGetProperty("model", out var m) ? m.GetString() : name),
                    SizeBytes = ParseLsblkSize(device.TryGetProperty("size", out var s) ? s.ToString().Trim('"') : null),
                    MediaType = DetectedValue<string>.FromString(media),
                    InterfaceType = DetectedValue<string>.FromString(device.TryGetProperty("tran", out var tr) ? tr.GetString() : null)
                });
            }

            return result;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static DetectedValue<TimeSpan> ParseUptime(string uptime)
    {
        var token = uptime.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (token is not null && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return DetectedValue<TimeSpan>.Detected(TimeSpan.FromSeconds(seconds));
        }

        return DetectedValue<TimeSpan>.Missing();
    }

    private static DetectedValue<int> CountProcessors(string cpuinfo)
    {
        var count = cpuinfo.Split('\n').Count(l => l.StartsWith("processor", StringComparison.Ordinal));
        return count == 0 ? DetectedValue<int>.Missing() : DetectedValue<int>.Detected(count);
    }

    private static string? First(Dictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static DetectedValue<long> ParseNvidiaMemory(string text)
    {
        var numeric = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (!double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            return DetectedValue<long>.Missing(Availability.NotReported);
        }

        if (text.Contains("GiB", StringComparison.OrdinalIgnoreCase) || text.Contains("GB", StringComparison.OrdinalIgnoreCase))
        {
            return DetectedValue<long>.Detected((long)(amount * 1024 * 1024 * 1024));
        }

        if (text.Contains("MiB", StringComparison.OrdinalIgnoreCase) || text.Contains("MB", StringComparison.OrdinalIgnoreCase))
        {
            return DetectedValue<long>.Detected((long)(amount * 1024 * 1024));
        }

        return DetectedValue<long>.Missing(Availability.NotReported);
    }

    private static DetectedValue<long> ParseLsblkSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return DetectedValue<long>.Missing();
        }

        if (long.TryParse(size, out var bytes))
        {
            return DetectedValue<long>.Detected(bytes);
        }

        return DetectedValue<long>.Missing(Availability.NotReported, "lsblk size was not bytes");
    }
}
