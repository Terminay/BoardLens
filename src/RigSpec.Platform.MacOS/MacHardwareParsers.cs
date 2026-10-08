using System.Text.RegularExpressions;
using RigSpec.Core;
using RigSpec.Core.Models;

namespace RigSpec.Platform.MacOS;

public static class MacHardwareParsers
{
    public static string Value(string text, string label)
    {
        var match = Regex.Match(text, $@"{Regex.Escape(label)}:\s*(.+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    public static CpuInfo ParseHardware(string hardware)
    {
        var chip = Value(hardware, "Chip");
        var processor = Value(hardware, "Processor Name");
        var cores = Value(hardware, "Total Number of Cores");
        var coreNumber = Regex.Match(cores, @"\d+");

        return new CpuInfo
        {
            Name = DetectedValue<string>.FromString(string.IsNullOrWhiteSpace(chip) ? processor : chip),
            PhysicalCores = coreNumber.Success
                ? DetectedValue<int>.Detected(int.Parse(coreNumber.Value))
                : DetectedValue<int>.Missing(),
            Architecture = DetectedValue<string>.FromString(Value(hardware, "Chip") is { Length: > 0 } ? "arm64" : null)
        };
    }

    public static DetectedValue<long> ParseMemoryBytes(string hardware)
    {
        var memory = Value(hardware, "Memory");
        var match = Regex.Match(memory, @"([\d.]+)\s*(GB|MB|TB)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return DetectedValue<long>.Missing();
        }

        var amount = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var multiplier = match.Groups[2].Value.ToUpperInvariant() switch
        {
            "MB" => 1024L * 1024,
            "GB" => 1024L * 1024 * 1024,
            "TB" => 1024L * 1024 * 1024 * 1024,
            _ => 1
        };

        return DetectedValue<long>.Detected((long)(amount * multiplier));
    }

    public static IReadOnlyList<GpuInfo> ParseDisplays(string displays)
    {
        var names = Regex.Matches(displays, @"Chipset Model:\s*(.+)", RegexOptions.IgnoreCase);
        if (names.Count == 0)
        {
            return [];
        }

        return names.Select(m => new GpuInfo
        {
            Name = DetectedValue<string>.FromString(m.Groups[1].Value),
            MemoryBytes = DetectedValue<long>.Missing(Availability.NotReported)
        }).ToList();
    }

    public static IReadOnlyList<StorageInfo> ParseStorage(string storage)
    {
        var results = new List<StorageInfo>();
        string? name = null;
        string? size = null;
        string? type = null;

        void Flush()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            results.Add(new StorageInfo
            {
                Model = DetectedValue<string>.FromString(name),
                SizeBytes = ParseMemoryBytes($"Memory: {size}"),
                MediaType = DetectedValue<string>.FromString(type)
            });
        }

        foreach (var raw in storage.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("Physical Drive:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Device / Media Name:", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                name = line.Split(':', 2)[1].Trim();
                size = null;
                type = null;
            }
            else if (line.StartsWith("Disk Size:", StringComparison.OrdinalIgnoreCase))
            {
                size = line.Split(':', 2)[1].Trim();
            }
            else if (line.StartsWith("Solid State:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line.Split(':', 2)[1].Trim();
                type = value.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "SSD"
                    : value.Equals("no", StringComparison.OrdinalIgnoreCase) ? "HDD" : null;
            }
        }

        Flush();
        return results;
    }
}
