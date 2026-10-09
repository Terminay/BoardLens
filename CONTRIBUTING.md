# Contributing

Thanks for helping improve rigspec. Bug reports, focused fixes, documentation improvements, and carefully scoped features are welcome.

## Before opening a pull request

- For a substantial change, open an issue first to discuss the problem and proposed approach.
- Keep each pull request focused; explain the user-visible change and any design trade-offs.
- Add or update tests for behavior changes, and update documentation when usage or architecture changes.
- Do not commit build outputs, packaged binaries, machine-specific data, secrets, or hardware exports that may contain identifiers.

## Development setup

Install the .NET 10 SDK, clone the repository, then run these commands from the repository root:

```sh
dotnet restore RigSpec.slnx
dotnet build RigSpec.slnx -c Release --no-restore
dotnet test RigSpec.slnx -c Release --no-build --verbosity normal
```

Run the desktop app or CLI during manual testing:

```sh
dotnet run --project src/RigSpec.App
dotnet run --project src/RigSpec.Cli
dotnet run --project src/RigSpec.Cli -- --json
```

## Pull requests and CI

Open a pull request against the default branch. GitHub Actions restores, builds, and tests the solution on Windows, macOS, and Linux. After those checks pass, it publishes self-contained GUI and CLI artifacts for Windows x64, Linux x64, and macOS x64. See the `ci` workflow in `.github/workflows/ci.yml`.

## Code and tests

- Follow the existing C# formatting, naming, nullable-reference, and async patterns.
- Keep platform-specific detection in the matching platform project; keep UI code platform-agnostic.
- Represent unavailable hardware data explicitly rather than guessing or substituting values.
- Prefer focused tests that do not depend on a developer's specific hardware.
- Do not add dependencies unless the change needs them; describe the reason in the pull request.
