namespace RigSpec.Core.Models;

public sealed class NetworkAdapterInfo
{
    public DetectedValue<string> Name { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Description { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> MacAddress { get; init; } = DetectedValue<string>.Missing(Availability.NotReported);
    public DetectedValue<bool> IsUp { get; init; } = DetectedValue<bool>.Missing();
}
