# Contributing Guidelines

## Development environment

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 with the .NET desktop development workload, or VS Code with C# Dev Kit

## Setup

```bash
git clone <repository-url>
cd WindowsThemeManager
dotnet restore
dotnet build
dotnet run --project src/WindowsThemeManager
```

## Code standards

- Follow Microsoft's C# conventions.
- Keep nullable reference types enabled.
- Use C# features appropriate for the project's target framework.
- Keep platform and business logic in services rather than XAML code-behind. View-specific input routing is an acceptable exception.
- Keep services focused, injectable, and testable.

## Architecture and wallpaper detection

- Follow the MVVM and service boundaries described in [Architecture](ARCHITECTURE.md).
- Theme discovery scans only the four standard Windows theme locations and their immediate child directories for `.theme` files. Keep parsing limited to metadata the UI or theme workflow consumes.
- Theme application is delegated to Windows through the complete `.theme` file. Do not add parallel per-component application paths unless the architecture, tests, and user documentation are updated together.
- The current wallpaper-change implementation polls `IDesktopWallpaper` every two seconds. Keep one authoritative detection path; do not add a second timer, file watcher, or registry loop without updating the architecture and tests.
- Changes to monitor refresh, COM interop, or Windows shell integration should include targeted diagnostics and manual verification.

## Logging policy

Important diagnostics should be available through the configured logger and the application debug log. When debugging monitor or wallpaper issues, identify the failing stage: platform hookup, state query, change detection, UI refresh, image loading, or file action. Remove temporary high-volume diagnostics when the issue is resolved.

## Testing

Run the test suite before submitting a change:

```bash
dotnet test
```

Add or update tests for behavior changes when practical. Manually verify monitor interactions, wallpaper open/delete behavior, theme application, and desktop icon restoration when those areas change.

The unit-test project covers core parsing, scanning, settings, layout, and service coordination. Platform-dependent COM, shell, Explorer, and WPF behavior still requires Windows manual verification.

## Documentation

- Update the relevant document under `docs/` for significant changes.
- Add a changelog entry for user-visible behavior changes.
- Update [Architecture](ARCHITECTURE.md) when system design or platform integration changes.
- Keep README links and examples valid.

## Git workflow

Create focused changes with descriptive commits. Use a feature branch and open a pull request when the repository workflow requires it.
