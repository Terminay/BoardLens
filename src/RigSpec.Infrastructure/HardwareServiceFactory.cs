using RigSpec.Core.Interfaces;
using RigSpec.Platform.Linux;
using RigSpec.Platform.MacOS;
using RigSpec.Platform.Windows;
using Microsoft.Extensions.Logging;

namespace RigSpec.Infrastructure;

public static class HardwareServiceFactory
{
    public static IHardwareService Create(ILoggerFactory loggerFactory)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsHardwareService(loggerFactory.CreateLogger<WindowsHardwareService>());
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxHardwareService(loggerFactory.CreateLogger<LinuxHardwareService>());
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOSHardwareService(loggerFactory.CreateLogger<MacOSHardwareService>());
        }

        var platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        return new UnsupportedHardwareService(loggerFactory.CreateLogger<UnsupportedHardwareService>(), platform);
    }
}
