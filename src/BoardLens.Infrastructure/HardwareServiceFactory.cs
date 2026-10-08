using BoardLens.Core.Interfaces;
using BoardLens.Platform.Linux;
using BoardLens.Platform.MacOS;
using BoardLens.Platform.Windows;
using Microsoft.Extensions.Logging;

namespace BoardLens.Infrastructure;

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
