using RigSpec.Core.Models;

namespace RigSpec.Core;

public sealed class HardwareSnapshot
{
    public required string Platform { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public required OperatingSystemInfo OperatingSystem { get; init; }
    public required CpuInfo Cpu { get; init; }
    public required MemoryInfo Memory { get; init; }
    public required MotherboardInfo Motherboard { get; init; }
    public required BiosInfo Bios { get; init; }
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = [];
    public IReadOnlyList<StorageInfo> Storage { get; init; } = [];
    public IReadOnlyList<NetworkAdapterInfo> NetworkAdapters { get; init; } = [];
    public IReadOnlyList<DisplayInfo> Displays { get; init; } = [];
    public IReadOnlyList<ComponentStatus> ComponentStatuses { get; init; } = [];
}
