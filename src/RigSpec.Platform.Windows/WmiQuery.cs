using System.Management;
using Microsoft.Extensions.Logging;

namespace RigSpec.Platform.Windows;

internal static class WmiQuery
{
    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(
        ILogger logger,
        string className,
        params string[] properties)
    {
        try
        {
            var select = properties.Length == 0 ? "*" : string.Join(",", properties);
            using var searcher = new ManagementObjectSearcher($"SELECT {select} FROM {className}");
            using var results = searcher.Get();
            var rows = new List<IReadOnlyDictionary<string, object?>>();

            foreach (ManagementBaseObject item in results)
            {
                using (item)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in item.Properties)
                    {
                        row[property.Name] = property.Value;
                    }

                    rows.Add(row);
                }
            }

            logger.LogDebug("WMI {Class} returned {Count} row(s)", className, rows.Count);
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WMI query failed for {Class}", className);
            return [];
        }
    }

    public static string? String(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) ? value?.ToString() : null;

    public static int? Int(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            uint u => (int)u,
            ushort us => us,
            short s => s,
            long l => (int)l,
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
        };
    }

    public static long? Long(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            long l => l,
            ulong ul => (long)ul,
            int i => i,
            uint u => u,
            _ => long.TryParse(value.ToString(), out var parsed) ? parsed : null
        };
    }

    public static string? CimDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 8)
        {
            return null;
        }

        try
        {
            var dt = ManagementDateTimeConverter.ToDateTime(raw);
            return dt.ToString("yyyy-MM-dd");
        }
        catch (Exception)
        {
            return raw.Length >= 8 ? $"{raw[..4]}-{raw[4..6]}-{raw[6..8]}" : raw;
        }
    }
}
