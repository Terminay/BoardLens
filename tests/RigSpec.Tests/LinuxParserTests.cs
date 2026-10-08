using RigSpec.Core;
using RigSpec.Platform.Linux;

namespace RigSpec.Tests;

public class LinuxParserTests
{
    [Fact]
    public void ParseCpu_uses_lscpu_and_cpuinfo()
    {
        const string cpuinfo = """
            processor	: 0
            model name	: Test CPU
            cpu MHz		: 2400.000
            processor	: 1
            """;
        const string lscpu = """
            Architecture: x86_64
            CPU(s): 16
            Thread(s) per core: 2
            Core(s) per socket: 8
            Socket(s): 1
            Model name: AMD Ryzen 7 5800X
            CPU max MHz: 4850.0000
            """;

        var cpu = LinuxParsers.ParseCpu(cpuinfo, lscpu);
        Assert.Equal("AMD Ryzen 7 5800X", cpu.Name.Value);
        Assert.Equal(8, cpu.PhysicalCores.Value);
        Assert.Equal(16, cpu.LogicalProcessors.Value);
        Assert.Equal(4850.0, cpu.MaxClockMhz.Value);
    }

    [Fact]
    public void ParseMemTotalBytes_reads_kb()
    {
        var value = LinuxParsers.ParseMemTotalBytes("MemTotal:       16384000 kB\nMemFree: 1000 kB");
        Assert.True(value.HasValue);
        Assert.Equal(16384000L * 1024, value.Value);
    }

    [Fact]
    public void ParseLspci_finds_vga()
    {
        const string lspci = "01:00.0 VGA compatible controller: NVIDIA Corporation Device 1234\n00:1f.3 Audio device: Intel";
        var gpus = LinuxParsers.ParseLspci(lspci);
        Assert.Single(gpus);
        Assert.Contains("NVIDIA", gpus[0].Name.Value, StringComparison.Ordinal);
        Assert.Equal(Availability.NotReported, gpus[0].MemoryBytes.Availability);
    }

    [Fact]
    public void ParseNvidiaSmi_reads_memory()
    {
        var gpus = LinuxParsers.ParseNvidiaSmi("NVIDIA GeForce RTX 3060, 12288 MiB");
        Assert.Single(gpus);
        Assert.True(gpus[0].MemoryBytes.HasValue);
        Assert.Equal(12288L * 1024 * 1024, gpus[0].MemoryBytes.Value);
    }

    [Fact]
    public void ParseLsblkJson_skips_non_disks()
    {
        const string json = """
            {"blockdevices":[{"name":"nvme0n1","model":"Samsung SSD","size":"1000204886016","type":"disk","tran":"nvme","rota":"0"},{"name":"nvme0n1p1","type":"part","size":"1000"}]}
            """;
        var disks = LinuxParsers.ParseLsblkJson(json);
        Assert.Single(disks);
        Assert.Equal("Samsung SSD", disks[0].Model.Value);
        Assert.Equal("NVMe SSD", disks[0].MediaType.Value);
        Assert.Equal(1000204886016, disks[0].SizeBytes.Value);
    }

    [Fact]
    public void ParseUptime_seconds()
    {
        var uptime = LinuxParsers.ParseUptime("12345.67 88888.00");
        Assert.True(uptime.HasValue);
        Assert.Equal(12345, (int)uptime.Value.TotalSeconds);
    }
}
