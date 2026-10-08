# BoardLens

A local-first, cross-platform hardware inspection tool for Windows, macOS, and Linux.

BoardLens 2.0 is a C# / .NET / Avalonia desktop application. The GUI and CLI share the same hardware service layer. Detection does not require Python, the network, or telemetry.

The original Python/Tkinter app (`main.py`, v0.5.4) is still in the repository as a functional reference until the .NET release fully replaces it.

## What it reports

Operating system, CPU, memory, motherboard, BIOS, GPU, storage, network adapters, and displays. Fields the OS does not expose are marked unavailable rather than guessed.

## Build

Requires the .NET 10 SDK.

```bash
dotnet build BoardLens.slnx
dotnet test BoardLens.slnx
```

GUI:

```bash
dotnet run --project src/BoardLens.App
```

CLI (same data as the GUI):

```bash
dotnet run --project src/BoardLens.Cli
dotnet run --project src/BoardLens.Cli -- --json
dotnet run --project src/BoardLens.Cli -- --diagnostics
```

Self-contained publish:

```bash
dotnet publish src/BoardLens.App/BoardLens.App.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/BoardLens.Cli/BoardLens.Cli.csproj -c Release -r linux-x64 --self-contained true
```

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Privacy

BoardLens runs entirely on the local machine. Hardware detection does not upload data and does not require a network connection.

## License

See `LICENSE`.
