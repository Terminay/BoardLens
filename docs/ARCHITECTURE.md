# BoardLens 2.0 architecture

BoardLens is a local-first hardware inspection utility. The GUI and CLI share one OS-agnostic core. Platform code never lives in the UI.

```
BoardLens.App (Avalonia)     BoardLens.Cli
              \               /
               BoardLens.Core
                      |
           BoardLens.Infrastructure
                      |
     Windows / Linux / macOS providers
```

## Layers

- **Core** — models (`CpuInfo`, `MemoryInfo`, …), `DetectedValue<T>` + `Availability`, `IHardwareService`, export contracts, formatting.
- **Infrastructure** — provider selection, JSON/text export. No WMI/`sysfs`/IOKit here.
- **Platform.*** — Windows CIM/WMI, Linux procfs/sysfs (commands only as fallback), macOS `system_profiler`/`sysctl` where needed.
- **App** — dense MVVM UI over `HardwareSnapshot`.
- **Cli** — `boardlens`, `--json`, `--summary`, `--diagnostics`.

## Missing data

Providers never invent values (including the old Ryzen 5500 / RTX 5050 hard-coded guesses). Unavailable fields use `Availability` (`Unknown`, `Unavailable`, `PermissionRequired`, `UnsupportedOnPlatform`, `NotReported`).

A failed GPU query does not block CPU, memory, or other sections.

## Privacy

Detection is offline. There is no telemetry, analytics, or automatic network use.

## Python app

`main.py` remains until the .NET app has feature parity in a release. It is the functional reference, not the architecture to copy.
