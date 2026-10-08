namespace BoardLens.Core.Models;

public sealed class StorageInfo
{
    public DetectedValue<string> Model { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<long> SizeBytes { get; init; } = DetectedValue<long>.Missing();
    public DetectedValue<string> MediaType { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> InterfaceType { get; init; } = DetectedValue<string>.Missing();
}
