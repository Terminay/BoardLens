using BoardLens.Platform.MacOS;

namespace BoardLens.Tests;

public class MacParserTests
{
    [Fact]
    public void ParseHardware_prefers_chip()
    {
        const string hardware = """
            Model Name: MacBook Pro
            Chip: Apple M3
            Total Number of Cores: 8 (4 performance and 4 efficiency)
            Memory: 16 GB
            Boot ROM Version: 11881.1.1
            """;

        var cpu = MacHardwareParsers.ParseHardware(hardware);
        Assert.Equal("Apple M3", cpu.Name.Value);
        Assert.Equal(8, cpu.PhysicalCores.Value);
        Assert.Equal(16L * 1024 * 1024 * 1024, MacHardwareParsers.ParseMemoryBytes(hardware).Value);
    }

    [Fact]
    public void ParseDisplays_reads_chipset()
    {
        var gpus = MacHardwareParsers.ParseDisplays("Chipset Model: Apple M3");
        Assert.Single(gpus);
        Assert.Equal("Apple M3", gpus[0].Name.Value);
    }

    [Fact]
    public void ParseStorage_reads_ssd()
    {
        const string storage = """
            Physical Drive: APPLE SSD
                Disk Size: 512 GB
                Solid State: Yes
            """;
        var disks = MacHardwareParsers.ParseStorage(storage);
        Assert.Single(disks);
        Assert.Equal("APPLE SSD", disks[0].Model.Value);
        Assert.Equal("SSD", disks[0].MediaType.Value);
    }
}
