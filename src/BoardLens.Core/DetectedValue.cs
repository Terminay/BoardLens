namespace BoardLens.Core;

public sealed record DetectedValue<T>(
    T? Value,
    Availability Availability,
    string? Reason = null)
{
    public bool HasValue => Availability == Availability.Detected && Value is not null;

    public static DetectedValue<T> Detected(T value) =>
        new(value, Availability.Detected);

    public static DetectedValue<T> Missing(
        Availability availability = Availability.Unknown,
        string? reason = null) =>
        new(default, availability, reason);

    public static DetectedValue<T> FromString(string? value, Availability ifMissing = Availability.Unknown)
    {
        if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
        {
            return Missing(ifMissing);
        }

        if (typeof(T) == typeof(string))
        {
            return Detected((T)(object)value.Trim());
        }

        return Missing(ifMissing);
    }

    public static DetectedValue<int> FromInt(string? value, Availability ifMissing = Availability.Unknown)
    {
        if (int.TryParse(value?.Trim(), out var parsed))
        {
            return DetectedValue<int>.Detected(parsed);
        }

        return DetectedValue<int>.Missing(ifMissing);
    }

    public static DetectedValue<long> FromLong(string? value, Availability ifMissing = Availability.Unknown)
    {
        if (long.TryParse(value?.Trim(), out var parsed))
        {
            return DetectedValue<long>.Detected(parsed);
        }

        return DetectedValue<long>.Missing(ifMissing);
    }

    public static DetectedValue<double> FromDouble(string? value, Availability ifMissing = Availability.Unknown)
    {
        if (double.TryParse(value?.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return DetectedValue<double>.Detected(parsed);
        }

        return DetectedValue<double>.Missing(ifMissing);
    }

    private static bool IsPlaceholder(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("Default string", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("None", StringComparison.OrdinalIgnoreCase);
    }
}
