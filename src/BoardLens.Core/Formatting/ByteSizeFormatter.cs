using System.Globalization;

namespace BoardLens.Core.Formatting;

public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            return "Unknown";
        }

        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {Units[unit]}"
            : string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", value, Units[unit]);
    }
}
