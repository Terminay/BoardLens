namespace BoardLens.Core.Formatting;

public static class AvailabilityFormatter
{
    public static string Describe(Availability availability) => availability switch
    {
        Availability.Detected => "Detected",
        Availability.Unknown => "Unknown",
        Availability.Unavailable => "Unavailable",
        Availability.PermissionRequired => "Permission required",
        Availability.UnsupportedOnPlatform => "Unsupported on this platform",
        Availability.NotReported => "Not reported by operating system",
        _ => availability.ToString()
    };

    public static string Format<T>(DetectedValue<T> value, Func<T, string>? format = null)
    {
        if (value.HasValue)
        {
            return format is null ? Convert.ToString(value.Value) ?? Describe(value.Availability) : format(value.Value!);
        }

        return value.Reason is { Length: > 0 }
            ? $"{Describe(value.Availability)} ({value.Reason})"
            : Describe(value.Availability);
    }
}
