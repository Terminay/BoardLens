namespace BoardLens.Core.Models;

public sealed class CpuInfo
{
    public DetectedValue<string> Name { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<int> PhysicalCores { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<int> LogicalProcessors { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<string> Architecture { get; init; } = DetectedValue<string>.Missing();
    public DetectedValue<double> MaxClockMhz { get; init; } = DetectedValue<double>.Missing();
    public DetectedValue<int> Sockets { get; init; } = DetectedValue<int>.Missing();
    public DetectedValue<int> ThreadsPerCore { get; init; } = DetectedValue<int>.Missing();
}
