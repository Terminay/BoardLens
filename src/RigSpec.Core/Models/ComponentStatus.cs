namespace RigSpec.Core.Models;

public sealed class ComponentStatus
{
    public required string Component { get; init; }
    public required Availability Availability { get; init; }
    public string? Detail { get; init; }
}
