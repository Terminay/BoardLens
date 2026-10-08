using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RigSpec.Core;
using RigSpec.Core.Formatting;
using RigSpec.Core.Interfaces;
using RigSpec.Core.Models;

namespace RigSpec.Infrastructure;

public sealed class JsonExportService : IExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson(HardwareSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, JsonOptions);

    public string ToText(HardwareSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RigSpec Hardware Report");
        sb.AppendLine($"Platform: {snapshot.Platform}");
        sb.AppendLine($"Captured: {snapshot.CapturedAt:O}");
        sb.AppendLine($"Duration: {snapshot.Duration.TotalMilliseconds:0} ms");
        sb.AppendLine();
        AppendOs(sb, snapshot.OperatingSystem);
        AppendCpu(sb, snapshot.Cpu);
        AppendMemory(sb, snapshot.Memory);
        AppendMotherboard(sb, snapshot.Motherboard);
        AppendBios(sb, snapshot.Bios);
        sb.AppendLine("GPU");
        if (snapshot.Gpus.Count == 0)
        {
            sb.AppendLine("  (none)");
        }

        foreach (var gpu in snapshot.Gpus)
        {
            sb.AppendLine($"  {AvailabilityFormatter.Format(gpu.Name)}  {FormatBytes(gpu.MemoryBytes)}");
        }

        sb.AppendLine("Storage");
        foreach (var disk in snapshot.Storage)
        {
            sb.AppendLine($"  {AvailabilityFormatter.Format(disk.Model)}  {FormatBytes(disk.SizeBytes)}  {AvailabilityFormatter.Format(disk.MediaType)}  {AvailabilityFormatter.Format(disk.InterfaceType)}");
        }

        sb.AppendLine("Network");
        foreach (var nic in snapshot.NetworkAdapters)
        {
            sb.AppendLine($"  {AvailabilityFormatter.Format(nic.Name)}  {AvailabilityFormatter.Format(nic.MacAddress)}  {(nic.IsUp.HasValue && nic.IsUp.Value ? "Up" : "Down")}");
        }

        sb.AppendLine("Diagnostics");
        foreach (var status in snapshot.ComponentStatuses)
        {
            sb.AppendLine($"  {status.Component,-14} {AvailabilityFormatter.Describe(status.Availability)}{(status.Detail is null ? "" : "  " + status.Detail)}");
        }

        return sb.ToString();
    }

    private static void AppendOs(StringBuilder sb, OperatingSystemInfo os)
    {
        sb.AppendLine("OS");
        Line(sb, "Name", AvailabilityFormatter.Format(os.Name));
        Line(sb, "Version", AvailabilityFormatter.Format(os.Version));
        Line(sb, "Build", AvailabilityFormatter.Format(os.Build));
        Line(sb, "Kernel", AvailabilityFormatter.Format(os.Kernel));
        Line(sb, "Architecture", AvailabilityFormatter.Format(os.Architecture));
        Line(sb, "Hostname", AvailabilityFormatter.Format(os.Hostname));
        Line(sb, "Uptime", AvailabilityFormatter.Format(os.Uptime, t => t.ToString(@"d\.hh\:mm\:ss")));
        sb.AppendLine();
    }

    private static void AppendCpu(StringBuilder sb, CpuInfo cpu)
    {
        sb.AppendLine("CPU");
        Line(sb, "Name", AvailabilityFormatter.Format(cpu.Name));
        Line(sb, "Cores", AvailabilityFormatter.Format(cpu.PhysicalCores));
        Line(sb, "Threads", AvailabilityFormatter.Format(cpu.LogicalProcessors));
        Line(sb, "Architecture", AvailabilityFormatter.Format(cpu.Architecture));
        Line(sb, "Max clock (MHz)", AvailabilityFormatter.Format(cpu.MaxClockMhz, v => v.ToString("0.##")));
        sb.AppendLine();
    }

    private static void AppendMemory(StringBuilder sb, MemoryInfo memory)
    {
        sb.AppendLine("Memory");
        Line(sb, "Installed", FormatBytes(memory.InstalledBytes));
        Line(sb, "Type", AvailabilityFormatter.Format(memory.Type));
        Line(sb, "Speed (MT/s)", AvailabilityFormatter.Format(memory.SpeedMtPerSecond));
        Line(sb, "Modules", AvailabilityFormatter.Format(memory.ModuleCount));
        sb.AppendLine();
    }

    private static void AppendMotherboard(StringBuilder sb, MotherboardInfo board)
    {
        sb.AppendLine("Motherboard");
        Line(sb, "Manufacturer", AvailabilityFormatter.Format(board.Manufacturer));
        Line(sb, "Model", AvailabilityFormatter.Format(board.Product));
        Line(sb, "Revision", AvailabilityFormatter.Format(board.Version));
        Line(sb, "Serial", AvailabilityFormatter.Format(board.SerialNumber));
        sb.AppendLine();
    }

    private static void AppendBios(StringBuilder sb, BiosInfo bios)
    {
        sb.AppendLine("BIOS");
        Line(sb, "Manufacturer", AvailabilityFormatter.Format(bios.Manufacturer));
        Line(sb, "Version", AvailabilityFormatter.Format(bios.Version));
        Line(sb, "Release date", AvailabilityFormatter.Format(bios.ReleaseDate));
        sb.AppendLine();
    }

    private static void Line(StringBuilder sb, string key, string value) =>
        sb.AppendLine($"  {key,-16} {value}");

    private static string FormatBytes(DetectedValue<long> value) =>
        AvailabilityFormatter.Format(value, ByteSizeFormatter.Format);
}
