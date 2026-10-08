using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using BoardLens.App.Support;
using BoardLens.Core;
using BoardLens.Core.Formatting;
using BoardLens.Core.Interfaces;
using BoardLens.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardLens.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IHardwareService _hardware;
    private readonly IExportService _export;
    private readonly SystemMetricsMonitor _metrics = new();
    private readonly DispatcherTimer _metricsTimer;
    private readonly List<double> _cpuHistory = Enumerable.Repeat(0.0, 30).ToList();
    private readonly List<double> _memoryHistory = Enumerable.Repeat(0.0, 30).ToList();

    private HardwareSnapshot? _snapshot;

    public MainViewModel(IHardwareService hardware, IExportService export)
    {
        _hardware = hardware;
        _export = export;

        NavItems = ["Overview", "Performance", "Hardware", "Diagnostics", "Settings", "About"];
        SelectedNav = "Overview";
        StatusText = "Ready";

        _metricsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _metricsTimer.Tick += (_, _) => UpdateMetrics();
        _metricsTimer.Start();

        _ = RefreshAsync();
    }

    public ObservableCollection<string> NavItems { get; }
    public ObservableCollection<SectionViewModel> Sections { get; } = [];
    public ObservableCollection<PropertyRow> Diagnostics { get; } = [];

    [ObservableProperty] private string _selectedNav = "Overview";
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _uiFontSize = 13.5;

    // View Switching
    [ObservableProperty] private bool _isSectionsView = true;
    [ObservableProperty] private bool _isPerformanceView;
    [ObservableProperty] private bool _isSettingsView;

    // Performance Metrics
    [ObservableProperty] private double _cpuUsagePercentage;
    [ObservableProperty] private string _cpuUsageText = "0%";
    [ObservableProperty] private AvaloniaList<Point> _cpuPoints = new() { new Point(0, 80), new Point(300, 80) };

    [ObservableProperty] private double _memoryUsagePercentage;
    [ObservableProperty] private string _memoryUsageText = "0 GB / 0 GB (0%)";
    [ObservableProperty] private AvaloniaList<Point> _memoryPoints = new() { new Point(0, 80), new Point(300, 80) };

    // Settings
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _defaultExportFormat = "JSON";

    partial void OnSelectedNavChanged(string value)
    {
        IsPerformanceView = value == "Performance";
        IsSettingsView = value == "Settings";
        IsSectionsView = !IsPerformanceView && !IsSettingsView;

        if (IsSectionsView)
        {
            RebuildSections();
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Avalonia.Application.Current is { } app)
        {
            app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    [RelayCommand]
    public void IncreaseFontSize() => UiFontSize = Math.Min(22.0, UiFontSize + 1.0);

    [RelayCommand]
    public void DecreaseFontSize() => UiFontSize = Math.Max(11.0, UiFontSize - 1.0);

    [RelayCommand]
    public void ResetFontSize() => UiFontSize = 13.5;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Scanning...";
        try
        {
            _snapshot = await _hardware.GetSnapshotAsync();
            RebuildDiagnostics();
            if (IsSectionsView)
            {
                RebuildSections();
            }
            var missing = _snapshot.ComponentStatuses.Count(s => s.Availability != Availability.Detected);
            StatusText = missing == 0
                ? $"Ready - {_snapshot.Duration.TotalMilliseconds:0} ms"
                : $"Completed in {_snapshot.Duration.TotalMilliseconds:0} ms - {missing} section(s) unavailable";
        }
        catch (Exception ex)
        {
            StatusText = "Scan failed";
            Sections.Clear();
            Sections.Add(new SectionViewModel("Error", [new PropertyRow("Message", ex.Message)]));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyAllAsync()
    {
        if (_snapshot is null)
        {
            return;
        }

        var text = _export.ToText(_snapshot);
        var clipboard = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
            StatusText = "Copied report";
        }
    }

    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        if (_snapshot is null)
        {
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return;
        }

        var isTxt = DefaultExportFormat.Equals("Text", StringComparison.OrdinalIgnoreCase);
        var suggestedExt = isTxt ? "txt" : "json";

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export hardware report",
            SuggestedFileName = $"boardlens.{suggestedExt}",
            FileTypeChoices =
            [
                new FilePickerFileType("JSON") { Patterns = ["*.json"] },
                new FilePickerFileType("Text") { Patterns = ["*.txt"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        var contents = file.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            ? _export.ToText(_snapshot)
            : _export.ToJson(_snapshot);
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(contents);
        StatusText = "Exported";
    }

    private void UpdateMetrics()
    {
        var cpu = _metrics.GetCpuUsagePercentage();
        CpuUsagePercentage = cpu;
        CpuUsageText = $"{cpu:0.0}%";

        _cpuHistory.RemoveAt(0);
        _cpuHistory.Add(cpu);
        CpuPoints = BuildPolylinePoints(_cpuHistory, 320, 80);

        var totalRam = _snapshot?.Memory.InstalledBytes.HasValue == true ? _snapshot.Memory.InstalledBytes.Value : 0;
        var (total, used, pct) = _metrics.GetMemoryUsage(totalRam);
        MemoryUsagePercentage = pct;
        var usedGb = (double)used / (1024 * 1024 * 1024);
        var totalGb = (double)total / (1024 * 1024 * 1024);
        MemoryUsageText = $"{usedGb:0.1} GB / {totalGb:0.1} GB ({pct:0.0}%)";

        _memoryHistory.RemoveAt(0);
        _memoryHistory.Add(pct);
        MemoryPoints = BuildPolylinePoints(_memoryHistory, 320, 80);
    }

    private static AvaloniaList<Point> BuildPolylinePoints(List<double> values, double width, double height)
    {
        var points = new AvaloniaList<Point>();
        if (values.Count < 2)
        {
            points.Add(new Point(0, height));
            points.Add(new Point(width, height));
            return points;
        }

        var step = width / (values.Count - 1);
        for (int i = 0; i < values.Count; i++)
        {
            var x = i * step;
            var clamped = Math.Clamp(values[i], 0.0, 100.0);
            var y = height - (clamped / 100.0 * height);
            points.Add(new Point(x, y));
        }

        return points;
    }

    private void RebuildDiagnostics()
    {
        Diagnostics.Clear();
        if (_snapshot is null)
        {
            return;
        }

        Diagnostics.Add(new PropertyRow("Platform", _snapshot.Platform));
        Diagnostics.Add(new PropertyRow("Duration", $"{_snapshot.Duration.TotalMilliseconds:0} ms"));
        foreach (var status in _snapshot.ComponentStatuses)
        {
            var value = AvailabilityFormatter.Describe(status.Availability);
            if (!string.IsNullOrWhiteSpace(status.Detail))
            {
                value += $" - {status.Detail}";
            }

            Diagnostics.Add(new PropertyRow(status.Component, value));
        }
    }

    private void RebuildSections()
    {
        Sections.Clear();
        if (_snapshot is null)
        {
            return;
        }

        if (SelectedNav == "About")
        {
            Sections.Add(new SectionViewModel("About",
            [
                new PropertyRow("Product", "BoardLens"),
                new PropertyRow("Version", "2.0.0"),
                new PropertyRow("Engine", ".NET 10 & Avalonia UI"),
                new PropertyRow("Privacy", "Local-first. Zero telemetry, completely offline."),
                new PropertyRow("Platform", _snapshot.Platform)
            ]));
            return;
        }

        if (SelectedNav == "Diagnostics")
        {
            Sections.Add(new SectionViewModel("Diagnostics", Diagnostics.ToList()));
            return;
        }

        AddOverview();
        if (SelectedNav == "Hardware")
        {
            AddHardwareDetails();
        }
    }

    private void AddOverview()
    {
        var s = _snapshot!;
        Sections.Add(new SectionViewModel("System",
        [
            Row("OS", s.OperatingSystem.Name),
            Row("Architecture", s.OperatingSystem.Architecture),
            Row("Hostname", s.OperatingSystem.Hostname),
            Row("Uptime", s.OperatingSystem.Uptime, t => t.ToString(@"d\.hh\:mm\:ss"))
        ]));
        Sections.Add(new SectionViewModel("CPU",
        [
            Row("Name", s.Cpu.Name),
            Row("Cores", s.Cpu.PhysicalCores),
            Row("Threads", s.Cpu.LogicalProcessors),
            Row("Architecture", s.Cpu.Architecture)
        ]));
        Sections.Add(new SectionViewModel("Memory",
        [
            Row("Installed", s.Memory.InstalledBytes, ByteSizeFormatter.Format),
            Row("Type", s.Memory.Type),
            Row("Speed", s.Memory.SpeedMtPerSecond, v => $"{v} MT/s")
        ]));
        Sections.Add(new SectionViewModel("Motherboard",
        [
            Row("Manufacturer", s.Motherboard.Manufacturer),
            Row("Model", s.Motherboard.Product),
            Row("BIOS", s.Bios.Version),
            Row("BIOS date", s.Bios.ReleaseDate)
        ]));
        if (s.Gpus.Count == 0)
        {
            Sections.Add(new SectionViewModel("GPU", [new PropertyRow("GPU", AvailabilityFormatter.Describe(Availability.Unavailable))]));
        }
        else
        {
            Sections.Add(new SectionViewModel("GPU", s.Gpus.Select(g =>
                new PropertyRow(AvailabilityFormatter.Format(g.Name), AvailabilityFormatter.Format(g.MemoryBytes, ByteSizeFormatter.Format)))));
        }
    }

    private void AddHardwareDetails()
    {
        var s = _snapshot!;
        Sections.Add(new SectionViewModel("Storage", s.Storage.Count == 0
            ? [new PropertyRow("Drive", AvailabilityFormatter.Describe(Availability.Unavailable))]
            : s.Storage.Select(d => new PropertyRow(
                AvailabilityFormatter.Format(d.Model),
                $"{AvailabilityFormatter.Format(d.SizeBytes, ByteSizeFormatter.Format)}  {AvailabilityFormatter.Format(d.MediaType)}  {AvailabilityFormatter.Format(d.InterfaceType)}"))));

        Sections.Add(new SectionViewModel("Network", s.NetworkAdapters.Count == 0
            ? [new PropertyRow("Adapter", AvailabilityFormatter.Describe(Availability.Unavailable))]
            : s.NetworkAdapters.Select(n => NetworkAdapterRow(n))));

        Sections.Add(new SectionViewModel("Displays", s.Displays.Count == 0
            ? [new PropertyRow("Display", AvailabilityFormatter.Describe(Availability.Unavailable))]
            : s.Displays.Select(d => new PropertyRow(
                AvailabilityFormatter.Format(d.Name),
                d.Width.HasValue && d.Height.HasValue
                    ? $"{d.Width.Value}×{d.Height.Value}{(d.RefreshRateHz.HasValue ? $" @ {d.RefreshRateHz.Value} Hz" : "")}"
                    : AvailabilityFormatter.Describe(Availability.NotReported)))));
    }

    private static PropertyRow Row<T>(string key, DetectedValue<T> value, Func<T, string>? format = null) =>
        new(key, AvailabilityFormatter.Format(value, format));

    private static PropertyRow NetworkAdapterRow(NetworkAdapterInfo n)
    {
        var status = n.IsUp.HasValue && n.IsUp.Value ? "Up" : "Down";
        var mac = AvailabilityFormatter.Format(n.MacAddress);
        return new PropertyRow(AvailabilityFormatter.Format(n.Name), $"{mac}  [{status}]");
    }
}
