using BoardLens.Core;
using BoardLens.Core.Formatting;

namespace BoardLens.Tests;

public class DetectedValueTests
{
    [Fact]
    public void FromString_rejects_placeholders()
    {
        var value = DetectedValue<string>.FromString("To Be Filled By O.E.M.");
        Assert.False(value.HasValue);
        Assert.Equal("Unknown", AvailabilityFormatter.Format(value));
    }

    [Fact]
    public void FromString_keeps_real_values()
    {
        var value = DetectedValue<string>.FromString(" AMD Ryzen 7 ");
        Assert.True(value.HasValue);
        Assert.Equal("AMD Ryzen 7", value.Value);
    }

    [Fact]
    public void Missing_includes_reason()
    {
        var value = DetectedValue<int>.Missing(Availability.PermissionRequired, "/sys/firmware/dmi");
        Assert.Contains("Permission required", AvailabilityFormatter.Format(value), StringComparison.Ordinal);
    }
}
