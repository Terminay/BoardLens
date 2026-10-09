# rigspec 2.0 architecture

rigspec is a local-first hardware inspection utility. The GUI and CLI share one OS-agnostic core. Platform code never lives in the UI.

```
RigSpec.App (Avalonia)     RigSpec.Cli
              \               /
               RigSpec.Core
                      |
           RigSpec.Infrastructure
                      |
     Windows / Linux / macOS providers
```

## Layers

- **Core** — models (`CpuInfo`, `MemoryInfo`, …), `DetectedValue<T>` + `Availability`, `IHardwareService`, export contracts, formatting.
- **Infrastructure** — provider selection, JSON/text export. No WMI/`sysfs`/IOKit here.
- **Platform.*** — Windows CIM/WMI, Linux procfs/sysfs (commands only as fallback), macOS `system_profiler`/`sysctl` where needed.
- **App** — dense MVVM UI over `HardwareSnapshot`.
- **Cli** — `rigspec`, `--json`, `--summary`, `--diagnostics`.

## Missing data

Providers never invent values (including the old Ryzen 5500 / RTX 5050 hard-coded guesses). Unavailable fields use `Availability` (`Unknown`, `Unavailable`, `PermissionRequired`, `UnsupportedOnPlatform`, `NotReported`).

A failed GPU query does not block CPU, memory, or other sections.

## Privacy

Detection is offline. There is no telemetry, analytics, or automatic network use.
