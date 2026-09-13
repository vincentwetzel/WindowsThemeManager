# Windows Theme Manager

A Windows desktop application for browsing and applying Windows themes, viewing wallpapers across multiple monitors, and backing up desktop icon positions.

## Features

- Discover `.theme` files from common Windows theme directories
- Apply a valid `.theme` file with one click through Windows
- Visualize the relative layout of connected monitors
- Open a monitor's wallpaper in the default Windows image viewer
- Move a wallpaper to the Recycle Bin after confirmation
- Refresh wallpaper previews when Windows wallpaper state changes
- Choose System, Light, or Dark colors for the application UI
- Capture, save, load, restore, and delete desktop icon layout backups

Theme discovery reads the display name and wallpaper metadata from `.theme` files;
Windows applies the complete theme file, including any components it supports.

## Tech Stack

- C# and WPF
- .NET 10 (`net10.0-windows`)
- Windows 10/11

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 or later, or VS Code with C# Dev Kit

### Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/WindowsThemeManager
```

For user-facing instructions, see the [User Guide](docs/USER_GUIDE.md). For development setup and conventions, see [Contributing](docs/CONTRIBUTING.md) and the mandatory [Coding Standards](CODING_STANDARDS.md).

## Project Structure

```
WindowsThemeManager/
├── src/
│   ├── WindowsThemeManager/       # WPF application
│   ├── WindowsThemeManager.Core/  # Models, services, and Windows interop
│   └── WindowsThemeManager.Tests/ # Unit tests
├── docs/                          # Project documentation
├── README.md
├── WindowsThemeManager.slnx
└── windows-theme-manager.code-workspace
```

## Documentation

- [User Guide](docs/USER_GUIDE.md) - Install, use, and manage themes, wallpapers, and icon layouts
- [Troubleshooting](docs/TROUBLESHOOTING.md) - Common issues, logs, and recovery steps
- [Architecture](docs/ARCHITECTURE.md) - Technical design and system architecture
- [Contributing](docs/CONTRIBUTING.md) - Development guidelines and testing expectations
- [Coding Standards](CODING_STANDARDS.md) - Mandatory coding, architecture, testing, and agent guidelines
- [Changelog](docs/CHANGELOG.md) - User-facing changes by release
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md) - Completed roadmap and implementation record

## License

TBD
