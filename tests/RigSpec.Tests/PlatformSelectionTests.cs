using RigSpec.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace RigSpec.Tests;

public class PlatformSelectionTests
{
    [Fact]
    public void Factory_selects_the_running_os()
    {
        var service = HardwareServiceFactory.Create(NullLoggerFactory.Instance);
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("Windows", service.PlatformName);
        }
        else if (OperatingSystem.IsLinux())
        {
            Assert.Equal("Linux", service.PlatformName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Assert.Equal("macOS", service.PlatformName);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(service.PlatformName));
        }
    }
}
