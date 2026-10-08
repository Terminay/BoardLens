using RigSpec.Core.Interfaces;
using RigSpec.Infrastructure;
using Microsoft.Extensions.Logging;

var json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
var diagnostics = args.Contains("--diagnostics", StringComparer.OrdinalIgnoreCase);
var summary = args.Contains("--summary", StringComparer.OrdinalIgnoreCase) || args.Length == 0 || (!json && !diagnostics);

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(diagnostics ? LogLevel.Debug : LogLevel.Warning);
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

IHardwareService hardware = HardwareServiceFactory.Create(loggerFactory);
IExportService exporter = new JsonExportService();
var snapshot = await hardware.GetSnapshotAsync();

if (json)
{
    Console.WriteLine(exporter.ToJson(snapshot));
}

if (summary || diagnostics)
{
    if (json)
    {
        Console.WriteLine();
    }

    Console.Write(exporter.ToText(snapshot));
}

return snapshot.ComponentStatuses.All(s => s.Availability == RigSpec.Core.Availability.Detected) ? 0 : 0;
