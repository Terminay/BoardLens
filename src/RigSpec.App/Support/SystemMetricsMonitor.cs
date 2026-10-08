using System.Runtime.InteropServices;

namespace RigSpec.App.Support;

public sealed class SystemMetricsMonitor
{
    private long _prevIdleTime;
    private long _prevTotalTime;

    public double GetCpuUsagePercentage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsCpuUsage();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxCpuUsage();
        }

        return 0;
    }

    public (long TotalBytes, long UsedBytes, double Percentage) GetMemoryUsage(long fallbackTotal = 0)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var mem = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(mem))
            {
                var total = (long)mem.ullTotalPhys;
                var used = (long)(mem.ullTotalPhys - mem.ullAvailPhys);
                var pct = total > 0 ? (double)used / total * 100.0 : 0.0;
                return (total, used, pct);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                long total = 0, avail = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var kb)) total = kb * 1024;
                    }
                    else if (line.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var kb)) avail = kb * 1024;
                    }
                }
                if (total > 0)
                {
                    var used = total - avail;
                    return (total, used, (double)used / total * 100.0);
                }
            }
            catch { }
        }

        var procUsed = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
        return (fallbackTotal > 0 ? fallbackTotal : procUsed * 4, procUsed, 25.0);
    }

    private double GetWindowsCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0;
        }

        var idle = ToLong(idleTime);
        var kernel = ToLong(kernelTime);
        var user = ToLong(userTime);
        var total = kernel + user;

        if (_prevTotalTime == 0)
        {
            _prevIdleTime = idle;
            _prevTotalTime = total;
            return 0;
        }

        var diffIdle = idle - _prevIdleTime;
        var diffTotal = total - _prevTotalTime;

        _prevIdleTime = idle;
        _prevTotalTime = total;

        if (diffTotal <= 0) return 0;
        var usage = (1.0 - (double)diffIdle / diffTotal) * 100.0;
        return Math.Clamp(usage, 0.0, 100.0);
    }

    private double GetLinuxCpuUsage()
    {
        try
        {
            var firstLine = File.ReadLines("/proc/stat").FirstOrDefault();
            if (firstLine != null && firstLine.StartsWith("cpu "))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    long total = 0;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (long.TryParse(parts[i], out var val)) total += val;
                    }
                    long.TryParse(parts[4], out var idle);

                    if (_prevTotalTime == 0)
                    {
                        _prevIdleTime = idle;
                        _prevTotalTime = total;
                        return 0;
                    }

                    var diffIdle = idle - _prevIdleTime;
                    var diffTotal = total - _prevTotalTime;
                    _prevIdleTime = idle;
                    _prevTotalTime = total;

                    if (diffTotal <= 0) return 0;
                    return Math.Clamp((1.0 - (double)diffIdle / diffTotal) * 100.0, 0.0, 100.0);
                }
            }
        }
        catch { }

        return 0;
    }

    private static long ToLong(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        return ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}

