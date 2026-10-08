namespace BoardLens.Core.Models;

public sealed class OperatingSystemInfo
{
    public DetectedValue<string> Name { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Version { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Build { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Kernel { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Architecture { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<string> Hostname { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<TimeSpan> Uptime { get; init; } = DetectedValue<TimeSpan>.Missing();
    public DetectedValue<string> DesktopEnvironment { get; init; } = DetectedValue<string>.Missing();
}
