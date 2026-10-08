using BoardLens.Core;
using BoardLens.Core.Models;
using BoardLens.Infrastructure;

namespace BoardLens.Tests;

public class ExportTests
{
    [Fact]
    public void Json_and_text_use_the_same_snapshot()
    {
        var snapshot = new HardwareSnapshot
        {
            Platform = "Test",
            CapturedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromMilliseconds(12),
            OperatingSystem = new OperatingSystemInfo { Name = DetectedValue<string>.Detected("TestOS") },
            Cpu = new CpuInfo { Name = DetectedValue<string>.Detected("Test CPU"), PhysicalCores = DetectedValue<int>.Detected(8) },
            Memory = new MemoryInfo { InstalledBytes = DetectedValue<long>.Detected(8L * 1024 * 1024 * 1024) },
            Motherboard = new MotherboardInfo { Product = DetectedValue<string>.Detected("Board") },
            Bios = new BiosInfo { Version = DetectedValue<string>.Detected("1.0") },
            Gpus = [new GpuInfo { Name = DetectedValue<string>.Detected("GPU") }],
            ComponentStatuses = [new ComponentStatus { Component = "CPU", Availability = Availability.Detected }]
        };

        var exporter = new JsonExportService();
        var json = exporter.ToJson(snapshot);
        var text = exporter.ToText(snapshot);

        Assert.Contains("Test CPU", json, StringComparison.Ordinal);
        Assert.Contains("Test CPU", text, StringComparison.Ordinal);
        Assert.Contains("\"availability\": \"Detected\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("stack trace", text, StringComparison.OrdinalIgnoreCase);
    }
}
