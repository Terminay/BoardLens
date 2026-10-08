namespace BoardLens.Core.Models;

public sealed class MemoryInfo
{
    public DetectedValue<long> InstalledBytes { get; init; } = DetectedValue<long>.Missing();
    public DetectedValue<string> Type { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<int> SpeedMtPerSecond { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<int> ModuleCount { get; init; } = DetectedValue<int>.Missing();
}
