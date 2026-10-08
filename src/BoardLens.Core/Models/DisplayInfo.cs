namespace BoardLens.Core.Models;

public sealed class DisplayInfo
{
    public DetectedValue<string> Name { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<int> Width { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<int> Height { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<int> RefreshRateHz { get; init; } = DetectedValue<int>.Missing();
}
