using RigSpec.Core.Models;

namespace RigSpec.Core.Support;

public static class SnapshotBuilder
{
    public static IReadOnlyList<ComponentStatus> BuildStatuses(
        OperatingSystemInfo os,
        CpuInfo cpu,
        MemoryInfo memory,
        MotherboardInfo motherboard,
        BiosInfo bios,
        IReadOnlyList<GpuInfo> gpus,
        IReadOnlyList<StorageInfo> storage,
        IReadOnlyList<NetworkAdapterInfo> network,
        IReadOnlyList<DisplayInfo> displays)
    {
        return
        [
            Status("OS", os.Name.HasValue, os.Name.Reason),
            Status("CPU", cpu.Name.HasValue, cpu.Name.Reason),
            Status("Memory", memory.InstalledBytes.HasValue, memory.InstalledBytes.Reason),
            Status("Motherboard", motherboard.Product.HasValue || motherboard.Manufacturer.HasValue, motherboard.Product.Reason),
            Status("BIOS", bios.Version.HasValue || bios.Manufacturer.HasValue, bios.Version.Reason),
            Status("GPU", gpus.Count > 0 && gpus.Any(g => g.Name.HasValue), gpus.Count == 0 ? "No adapters reported" : null),
            Status("Storage", storage.Count > 0 && storage.Any(s => s.Model.HasValue || s.SizeBytes.HasValue), storage.Count == 0 ? "No disks reported" : null),
            Status("Network", network.Count > 0, network.Count == 0 ? "No adapters reported" : null),
            Status("Display", displays.Count > 0, displays.Count == 0 ? "No displays reported" : null)
        ];
    }

    private static ComponentStatus Status(string component, bool detected, string? detail) => new()
    {
        Component = component,
        Availability = detected ? Availability.Detected : Availability.Unavailable,
        Detail = detected ? null : detail
    };
}
