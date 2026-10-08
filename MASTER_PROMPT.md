# rigspec 2.0 — Master Engineering Prompt

You are rebuilding rigspec from the existing repository into a serious, cross-platform hardware information utility.

Repository:
https://github.com/CodeAnd-Copper/BoardLens

## 0. Mission

Turn the existing rigspec fork from a small Python/Tkinter utility into a maintainable, technically rigorous desktop application for Windows, macOS, and Linux.

The existing application is the **functional reference**, not the architectural reference.

Do not blindly preserve the existing implementation.

First understand what the current application does, what information it collects, how it collects it, and where its limitations are. Then rebuild the application around a clean architecture.

The result should feel like a **serious system utility**, not a toy GUI, web dashboard, or AI-generated showcase.

The priorities are:

1. Correctness
2. Hardware detection reliability
3. Cross-platform architecture
4. Maintainability
5. Information density and usability
6. Performance
7. Packaging/distribution
8. Visual polish

Do NOT optimize for visual spectacle.

---

# 1. Technology Direction

Use:

* C#
* .NET
* Avalonia UI
* XAML
* MVVM where appropriate

Target:

* Windows
* macOS
* Linux

Do NOT use:

* Tkinter
* CustomTkinter
* PyQt
* PySide
* Electron
* webview-based UI
* Python GUI frameworks
* a Python subprocess as the primary architecture
* platform-specific UI implementations when an abstraction is appropriate

The application should be a real native desktop executable built from the C#/.NET project.

Use platform-specific native APIs only inside platform-specific infrastructure/services.

---

# 2. Core Architectural Principle

The application MUST be operating-system agnostic at the application/core layer.

The UI must not contain:

* Windows WMI queries
* Linux `/sys` parsing
* macOS shell commands
* registry access
* platform-specific process calls
* hardware-detection logic

The UI consumes normalized models.

Example:

```csharp
var systemInfo = await hardwareService.GetSystemInfoAsync();
```

The UI should not care whether the data came from WMI, sysfs, IOKit, system_profiler, or another source.

Architecture:

```text
rigspec
│
├── RigSpec.App
│   ├── Views
│   ├── ViewModels
│   ├── Assets
│   └── Styles
│
├── RigSpec.Core
│   ├── Models
│   ├── Interfaces
│   ├── Services
│   └── Exceptions
│
├── RigSpec.Infrastructure
│   ├── Hardware
│   ├── Platform
│   └── Serialization
│
├── RigSpec.Platform.Windows
│
├── RigSpec.Platform.Linux
│
└── RigSpec.Platform.MacOS
```

Keep the exact structure flexible if there is a better .NET architecture, but preserve the separation of concerns.

---

# 3. Hardware Service Abstraction

Create interfaces for hardware/system information.

For example:

```csharp
public interface IHardwareService
{
    Task<SystemInfo> GetSystemInfoAsync();
    Task<CpuInfo> GetCpuInfoAsync();
    Task<MemoryInfo> GetMemoryInfoAsync();
    Task<GpuInfo> GetGpuInfoAsync();
    Task<StorageInfo> GetStorageInfoAsync();
    Task<MotherboardInfo> GetMotherboardInfoAsync();
    Task<BiosInfo> GetBiosInfoAsync();
    Task<OperatingSystemInfo> GetOperatingSystemInfoAsync();
}
```

Do not blindly copy this interface if a better decomposition is technically justified.

The architecture should support:

```text
WindowsHardwareService
LinuxHardwareService
MacOSHardwareService
```

or smaller platform-specific services where appropriate.

Use dependency injection or another clean mechanism to select the appropriate implementation.

---

# 4. Platform Detection

Do NOT assume all operating systems expose hardware information in the same way.

Use the best reliable mechanism available on each platform.

Windows may use mechanisms such as:

* WMI/CIM
* Windows APIs
* registry only when appropriate
* PowerShell only as a last resort, not as the core architecture

Linux may use:

* sysfs
* procfs
* udev
* DMI information
* standard system utilities only when appropriate

macOS may use:

* IOKit
* system APIs
* system_profiler where appropriate

Do not shell out to random commands when a stable native API exists.

Do not require Python to be installed.

Do not require users to manually install dependencies just to inspect their hardware.

---

# 5. Normalized Data Models

Create clean models such as:

```text
SystemInfo
CpuInfo
MemoryInfo
GpuInfo
StorageInfo
MotherboardInfo
BiosInfo
OperatingSystemInfo
NetworkAdapterInfo
DisplayInfo
```

Models should represent information rather than presentation.

Avoid putting UI-specific strings inside the core models.

For example, prefer:

```csharp
public int CoreCount { get; }
```

over:

```csharp
public string CoresText { get; }
```

Formatting belongs closer to the presentation layer.

---

# 6. Missing / Unsupported Data

Hardware detection is inherently inconsistent across operating systems and hardware vendors.

Never crash because one field is unavailable.

Represent unavailable information explicitly.

For example:

```text
Unknown
Unavailable
Not reported by operating system
Permission required
Unsupported on this platform
```

Do not fabricate values.

Do not silently substitute unrelated values.

Do not report guessed hardware information as fact.

If detection fails, show a useful diagnostic message.

---

# 7. Error Handling

Hardware detection must be fault tolerant.

A failed GPU query must not prevent CPU information from appearing.

A failed BIOS query must not crash the application.

Prefer:

```text
CPU       ✓ Detected
Memory    ✓ Detected
GPU       ✓ Detected
BIOS      ! Unavailable
```

over:

```text
ERROR: Application failed
```

Capture useful diagnostic information internally.

Do not expose raw stack traces to ordinary users.

Provide a diagnostics/logging mechanism for troubleshooting.

---

# 8. UI Philosophy

This is extremely important.

The UI should be:

**Technical. Practical. Pragmatic. Dense. Trustworthy.**

It is NOT:

* glassmorphism
* futuristic
* "AI dashboard"
* macOS-inspired translucent UI
* excessive rounded cards
* gradient-heavy
* giant typography
* floating blobs
* decorative animations
* excessive whitespace
* emoji-based UI
* fake 3D effects

Do not try to make the application look expensive by adding visual noise.

The visual inspiration should be closer to:

* Windows Terminal
* VS Code
* Process Explorer
* Device Manager
* Wireshark
* serious developer tools
* professional diagnostic utilities

The application should look like a tool made by engineers for people who want information.

---

# 9. Information Architecture

The main screen should make system information immediately scannable.

Possible structure:

```text
rigspec
────────────────────────────────────────────

Overview

SYSTEM
OS              Windows 11 Pro
Architecture    x64
Hostname        DESKTOP-XXXX
Uptime          ...

CPU
Name            ...
Cores           8
Threads         16
Architecture    x64

MEMORY
Installed       32 GB
Type            DDR4
Speed           3200 MT/s

MOTHERBOARD
Manufacturer    ...
Model           ...
BIOS            ...
BIOS Date       ...

GPU
Name            ...
VRAM            ...

────────────────────────────────────────────
Refresh                              Ready
```

Do not blindly reproduce this layout.

Use it as an information-density reference.

---

# 10. Navigation

Use navigation only when it provides real value.

Potential sections:

* Overview
* Hardware
* Motherboard
* CPU
* Memory
* GPU
* Storage
* Network
* Software / OS
* Diagnostics
* About

Do not create pages merely to make the application appear larger.

If a category has little information, keep it compact.

---

# 11. Data Presentation

Prefer:

* key/value rows
* compact tables
* grouped sections
* tabs where appropriate
* selectable text
* copyable values
* sortable lists where useful
* filtering/search for long hardware lists

Avoid:

* huge cards
* decorative charts that convey no useful information
* unnecessary graphs
* fake metrics
* excessive icons
* "100% optimized" style nonsense

If the data is naturally tabular, use a table.

If the data is a property/value pair, use a property grid or compact key/value layout.

---

# 12. Interaction

The application should be keyboard-friendly.

Support:

* Tab navigation
* keyboard activation
* copying values
* refreshing information
* selecting/copying rows
* sensible focus behavior

Buttons should have obvious actions.

Examples:

```text
Refresh
Copy
Copy All
Export
Diagnostics
```

Do not hide useful functionality behind obscure menus.

---

# 13. Refresh Architecture

Hardware information should be refreshable without restarting the application.

Use asynchronous operations.

Do not freeze the UI while querying hardware.

Show appropriate state:

```text
Ready
Scanning...
Refreshing...
Completed
Some information unavailable
```

Do not use fake progress bars.

If an operation completes instantly, do not display a theatrical animation.

---

# 14. Export

Add an export mechanism once the core architecture is stable.

Potential formats:

* JSON
* TXT
* CSV where appropriate

JSON should preserve structured information.

Example:

```json
{
  "system": {},
  "cpu": {},
  "memory": {},
  "motherboard": {},
  "bios": {},
  "gpu": {}
}
```

The export should come from the same normalized models consumed by the UI.

Do not scrape UI text to generate exports.

---

# 15. CLI Compatibility

Consider creating a CLI mode using the same core library.

Example:

```text
rigspec
rigspec --json
rigspec --summary
rigspec --diagnostics
```

The GUI and CLI must use the same hardware service layer.

Architecture:

```text
                 RigSpec.Core
                 /            \
                /              \
        Avalonia GUI           CLI
             │                  │
             └────────┬─────────┘
                      │
              Hardware Services
```

This is desirable because rigspec is fundamentally an information utility.

Do not prioritize the CLI over the GUI, but keep the core reusable.

---

# 16. Logging and Diagnostics

Implement structured logging.

Logs should help answer:

* Which platform was detected?
* Which hardware providers were attempted?
* Which providers succeeded?
* Which properties were unavailable?
* Which exception occurred?
* How long did detection take?

Do not log sensitive hardware identifiers unnecessarily.

Do not log secrets.

Make diagnostic output useful for bug reports.

---

# 17. Privacy

rigspec should be local-first.

Hardware information should NOT be uploaded anywhere unless the user explicitly initiates an action that requires it.

No:

* analytics by default
* telemetry by default
* remote hardware reporting
* unnecessary network requests
* cloud dependency

The application should function completely offline for hardware detection.

Document this clearly.

---

# 18. Performance

The application should start quickly.

Do not:

* spawn unnecessary processes
* repeatedly execute shell commands
* perform hardware queries on the UI thread
* poll hardware constantly
* perform network requests during startup

Cache information only where it makes technical sense.

Hardware information should be refreshed explicitly or through a controlled mechanism.

---

# 19. Security

Treat all external command output as untrusted input.

Avoid shell execution where possible.

Never construct shell commands using unsanitized user input.

Do not request administrator/root privileges unless a specific feature genuinely requires them.

rigspec should normally operate as a regular user.

---

# 20. Cross-Platform Packaging

The final project should support:

### Windows

* self-contained executable
* installer or portable build
* appropriate application icon

### macOS

* `.app`
* `.dmg`
* architecture-aware builds where practical

### Linux

At minimum:

* AppImage or another sensible portable distribution

Potentially:

* `.deb`

Do not make Linux users install Python just to run rigspec.

---

# 21. CI/CD

Set up GitHub Actions for:

* build
* test
* platform compilation
* release artifacts

Where practical, build:

```text
Windows
macOS
Linux
```

Do not rely on manually building releases on one machine.

---

# 22. Testing

Create tests for:

* data models
* formatting
* platform selection
* parsing
* hardware provider behavior
* error handling
* serialization
* export

Platform-specific tests should be executed on their respective CI runners where possible.

Do not write tests that depend on one developer's exact hardware.

---

# 23. Code Quality

Use:

* nullable reference types
* async/await where appropriate
* clear interfaces
* dependency injection where useful
* meaningful names
* small focused classes
* XML documentation for public APIs where useful

Avoid:

* giant classes
* giant methods
* global mutable state
* magic strings
* duplicated platform logic
* UI/business-logic coupling
* copy-pasted platform implementations

Do not over-engineer.

Abstraction should exist because it solves a real problem.

---

# 24. Migration Strategy

Do NOT delete the existing implementation immediately.

First:

1. Inspect the entire repository.
2. Understand `main.py`.
3. Document what it currently detects.
4. Identify bugs and limitations.
5. Identify reusable algorithms or detection logic.
6. Identify functionality that should be preserved.
7. Create the new architecture.
8. Implement the core models.
9. Implement Windows detection.
10. Implement macOS detection.
11. Implement Linux detection.
12. Build the Avalonia UI.
13. Add tests.
14. Add packaging.
15. Remove obsolete Python/Tkinter code only after feature parity is achieved.

Do not perform a giant blind rewrite with no checkpoints.

---

# 25. Git Discipline

Make changes in logical commits.

Examples:

```text
chore: initialize .NET solution
feat: add hardware domain models
feat: add platform abstraction
feat: implement Windows hardware provider
feat: implement macOS hardware provider
feat: implement Linux hardware provider
feat: add Avalonia shell
feat: add system overview
feat: add hardware details
test: add hardware model tests
build: add cross-platform CI
build: add release packaging
```

Do not create one enormous commit containing the entire rewrite.

---

# 26. AI Coding Rules

You are an engineering agent, not a code autocomplete machine.

Before changing architecture:

1. Inspect the repository.
2. Understand existing behavior.
3. Identify dependencies.
4. Identify platform-specific functionality.
5. Explain the proposed change briefly.
6. Implement it.
7. Build.
8. Test.
9. Fix errors.
10. Only then continue.

Never invent APIs.

Never claim something works without compiling/testing it.

If a platform-specific API is uncertain, investigate the correct API before implementing it.

Do not replace working functionality with a mock.

Do not use placeholder hardware data in production code.

Do not silently remove existing features.

If a feature cannot be implemented on one OS, represent that limitation explicitly.

---

# 27. UI Anti-Patterns

The following are explicitly forbidden unless there is a genuine usability reason:

* glassmorphism
* translucent panels
* excessive blur
* gradient backgrounds
* giant hero sections
* oversized titles
* excessive rounded rectangles
* floating dashboard cards everywhere
* decorative charts
* unnecessary animations
* emoji icons
* "AI-looking" interfaces
* excessive shadows
* excessive whitespace
* fake futuristic terminology

Do not interpret "modern" as "visually flashy."

Modern means:

* reliable
* responsive
* accessible
* maintainable
* clear
* technically competent

---

# 28. Product Identity

rigspec should feel like:

> A serious open-source hardware inspection tool.

Not:

> A portfolio project demonstrating how many UI effects the developer knows.

The user should be able to launch it and immediately answer:

* What computer am I using?
* What CPU do I have?
* How much RAM?
* What GPU?
* What motherboard?
* What BIOS?
* What OS?
* What storage?
* What network adapters?
* What information is unavailable and why?

The application should get out of the user's way.

---

# 29. Definition of Done

rigspec 2.0 is not finished because the UI looks good.

It is finished when:

* Windows works
* macOS works
* Linux works
* hardware detection is reliable
* missing data is handled correctly
* the UI remains responsive
* the core is OS-agnostic
* platform services are modular
* the application can run without Python
* the GUI and CLI can share the same core
* tests pass
* builds are reproducible
* release artifacts can be generated
* documentation explains the architecture
* no unnecessary telemetry/network dependency exists

Most importantly:

**Do not sacrifice technical correctness for appearance.**

Build the boring, reliable parts first.

Then make the interface clean.

---

# Final Instruction

Start by auditing the existing rigspec repository.

Do NOT immediately start writing the new UI.

First produce:

1. Current architecture summary
2. Current functionality inventory
3. Current platform-specific behavior
4. Current dependencies
5. Problems with the existing architecture
6. Proposed rigspec 2.0 architecture
7. Migration plan
8. Risks / unknowns

Then wait for the implementation phase or proceed only if explicitly instructed.

The goal is not to make the existing Python app prettier.

The goal is to turn rigspec into a **proper cross-platform hardware utility with a clean, modular, OS-agnostic architecture.**
