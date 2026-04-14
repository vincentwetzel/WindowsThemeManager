# Windows Theme Manager

A modern Windows desktop application for managing themes and monitor wallpapers with a visual, intuitive interface.

## Overview

Windows Theme Manager provides an enhanced theme management experience by:
- Discovering themes from common Windows theme folders
- Applying themes with a single click
- Showing a visual monitor layout with live wallpaper previews
- Opening a monitor wallpaper in the system default image viewer
- Confirming deletions before moving a wallpaper to the Recycle Bin

## Features

- Discover and manage themes from multiple Windows theme directories
- Visual representation of multi-monitor setups with current wallpapers
- Click-to-apply theme selection
- Click-to-open wallpaper files for quick viewing or editing
- Red X delete action on each monitor preview with recycle-bin confirmation
- Light/Dark/System theme selector in the status bar for app UI theming
- Persistent settings for theme preference, window size, and panel width
- Async startup so settings load without blocking the UI thread
- Resizable monitor canvas with Viewbox scaling for accurate multi-monitor visualization
- Native Windows integration
- Real-time wallpaper updates through Windows COM events

### Wallpaper Change Detection Requirement

**CRITICAL**: The application uses the `IDesktopWallpaper` COM event stream to receive wallpaper change events from Windows.

Polling is not used for wallpaper change detection. If the COM subscription fails, fix the COM event path instead of introducing timers, file watchers, or refresh loops.

### Debugging Requirement

- When a feature is stuck, add targeted debug prints or structured logs before continuing to guess at fixes.
- Diagnostics should make it clear which stage is succeeding or failing, especially around COM hookup, callback delivery, UI refresh, image loading, and file launch/delete actions.
- Remove or downgrade temporary diagnostics once the issue is resolved.

## Tech Stack

- Language: C#
- Framework: .NET (WPF)
- Platform: Windows 10/11

## Project Structure

```
WindowsThemeManager/
├── src/
│   ├── WindowsThemeManager/
│   ├── WindowsThemeManager.Core/
│   └── WindowsThemeManager.Tests/
├── README.md
├── ARCHITECTURE.md
├── CONTRIBUTING.md
├── CHANGELOG.md
└── IMPLEMENTATION_PLAN.md
```

## Getting Started

### Prerequisites

- Windows 10/11
- Visual Studio 2022 or later
- .NET 8.0 SDK or later

### Building the Project

```bash
dotnet restore
dotnet build
dotnet run --project src/WindowsThemeManager
```

## Documentation

- [Architecture](ARCHITECTURE.md) - Technical design and system architecture
- [Implementation Plan](IMPLEMENTATION_PLAN.md) - Completed roadmap and implementation notes
- [Changelog](CHANGELOG.md) - User-facing changes by release
- [Contributing](CONTRIBUTING.md) - Development guidelines

## License

TBD
