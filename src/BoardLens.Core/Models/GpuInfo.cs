namespace BoardLens.Core.Models;

public sealed class GpuInfo
{
    public DetectedValue<string> Name { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<long> MemoryBytes { get; init; } = DetectedValue<long>.Missing(Availability.NotReported);
}
