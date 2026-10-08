namespace BoardLens.Core.Models;

public sealed class BiosInfo
{
    public DetectedValue<string> Manufacturer { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Version { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> ReleaseDate { get; init; } = DetectedValue<string>.Missing();
}
