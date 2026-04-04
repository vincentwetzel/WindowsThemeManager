# Windows Theme Manager

A modern Windows desktop application for managing themes and monitor wallpapers with a visual, intuitive interface.

## Overview

This application provides an enhanced theme management experience by:
- **Theme Browser**: Lists all available themes from common Windows theme directories
- **One-Click Activation**: Apply any theme with a single click
- **Visual Monitor Layout**: Displays your monitors in their actual relative positions (like Windows Display Settings)
- **Live Wallpaper Preview**: Shows the current wallpaper displayed on each monitor
- **Quick Access**: Click any monitor preview to open the wallpaper in your default image viewer

## Features

- Discover and manage themes from multiple Windows theme directories
- Visual representation of multi-monitor setups with current wallpapers
- Click-to-apply theme selection
- Click-to-open wallpaper files for quick editing/viewing
- **Light/Dark/System theme selector** in the status bar for app UI theming
- **Persistent settings** — theme preference, window size, and panel width are saved between sessions
- **Async startup** — non-blocking settings load prevents UI thread deadlocks
- **Resizable monitor canvas** with Viewbox scaling for accurate multi-monitor visualization
- Native Windows integration
- **Real-time wallpaper updates**: Automatically detects and displays wallpaper changes via Windows COM events (no polling allowed)

### Wallpaper Change Detection Requirement

**CRITICAL**: The application MUST use the `IDesktopWallpaper.Advise()` COM interface to receive wallpaper change events from Windows. 

**Polling is strictly forbidden** for detecting wallpaper changes. The COM callback approach provides instant, zero-latency notifications when wallpapers change on any monitor. This is a hard architectural requirement that all agents must follow.

### No Polling Rule

- Wallpaper changes must be detected only through the `IDesktopWallpaper` COM event stream.
- Do not add timers, background refresh loops, file watchers, registry polling, or any other polling-based fallback for wallpaper change detection.
- If the COM subscription fails, fix the COM event path rather than introducing periodic checks.

### Debugging Requirement

- When a feature is stuck in debugging, add targeted debug prints or structured logs before continuing to guess at fixes.
- Diagnostics must make it clear which stage is succeeding or failing, especially around COM subscription, callback delivery, UI refresh, and image loading.
- Remove or downgrade temporary diagnostics once the issue is resolved, but always prefer observable debugging over speculative changes while a bug is still unresolved.

## Tech Stack

- **Language**: C#
- **Framework**: .NET (WPF/WinUI 3 - TBD)
- **Platform**: Windows 10/11

## Project Structure

```
WindowsThemeManager/
├── src/                    # Source code
│   ├── WindowsThemeManager/           # Main application
│   ├── WindowsThemeManager.Core/      # Core logic and models
│   └── WindowsThemeManager.Tests/     # Unit tests
├── docs/                     # Documentation
├── README.md
├── ARCHITECTURE.md
├── CONTRIBUTING.md
└── IMPLEMENTATION_PLAN.md
```

## Getting Started

### Prerequisites

- Windows 10/11
- Visual Studio 2022 or later
- .NET 8.0 SDK or later

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src/WindowsThemeManager
```

## Documentation

- [Architecture](ARCHITECTURE.md) - Technical design and system architecture
- [Implementation Plan](IMPLEMENTATION_PLAN.md) - Detailed TODO tasks and development roadmap
- [Contributing](CONTRIBUTING.md) - Development guidelines

## License

TBD
