namespace BoardLens.Core.Models;

public sealed class MotherboardInfo
{
    public DetectedValue<string> Manufacturer { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Product { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Version { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> SerialNumber { get; init; } = DetectedValue<string>.Missing(Availability.NotReported);
}
