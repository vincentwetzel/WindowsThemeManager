# Architecture

## System Overview

Windows Theme Manager is a C# desktop application that provides enhanced theme management capabilities with visual monitor layout and wallpaper preview functionality.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│  ┌─────────────────┐  ┌──────────────────────────────────┐  │
│  │  Theme Browser  │  │    Monitor Layout Display        │  │
│  │  (Left Panel)   │  │    (Main Area)                   │  │
│  │                 │  │  ┌────┐     ┌────┐              │  │
│  │  - Theme List   │  │  │ M1 │     │ M2 │              │  │
│  │  - Search/Filter│  │  └────┘     └────┘              │  │
│  │  - Preview      │  │                                  │  │
│  └────────┬────────┘  └──────────┬───────────────────────┘  │
└───────────┼──────────────────────┼──────────────────────────┘
            │                      │
┌───────────┼──────────────────────┼──────────────────────────┐
│           ▼         Service      ▼         Layer            │
│  ┌─────────────────┐  ┌──────────────────────────────────┐  │
│  │  Theme Service  │  │    Monitor Service               │  │
│  │                 │  │                                  │  │
│  │  - Scan themes  │  │  - Detect monitors               │  │
│  │  - Parse .theme │  │  - Get positions/resolutions     │  │
│  │  - Apply theme  │  │  - Get current wallpaper per mon │  │
│  └────────┬────────┘  └────────┬─────────────────────────┘  │
└───────────┼────────────────────┼────────────────────────────┘
            │                    │
┌───────────┼────────────────────┼────────────────────────────┐
│           ▼     Data &         ▼       System Layer         │
│  ┌─────────────────┐  ┌──────────────────────────────────┐  │
│  │  Theme Models   │  │    Windows APIs                  │  │
│  │                 │  │                                  │  │
│  │  - Theme        │  │  - Registry access               │  │
│  │  - Wallpaper    │  │  - Win32 Display APIs            │  │
│  │  - Cursor/etc   │  │  - SystemParametersInfo          │  │
│  └─────────────────┘  └──────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Theme Discovery Service

**Responsibility**: Scan Windows theme directories and parse theme files.

**Key Locations**:
- `%LOCALAPPDATA%\Microsoft\Windows\Themes`
- `C:\Windows\Resources\Themes`
- `%APPDATA%\Microsoft\Windows\Themes`

**Theme File Structure** (.theme files are INI-format):
```ini
[Theme]
DisplayName=My Theme

[Control Panel\Desktop]
Wallpaper=C:\path\to\wallpaper.jpg

[VisualStyles]
Path=C:\path\to\visualstyle.msstyles
```

### 2. Theme Application Service

**Responsibility**: Apply themes by updating Windows settings.

**Operations**:
- Set desktop wallpaper
- Apply visual styles (.msstyles)
- Update cursor scheme
- Update color scheme
- Update sound scheme

### 3. Monitor Detection Service

**Responsibility**: Detect connected monitors and their configuration.

**Information Gathered**:
- Monitor count
- Resolution per monitor
- Relative positions (primary/secondary layout)
- Current wallpaper per monitor
- Device names

### 4. Wallpaper Management Service

**Responsibility**: Track and manage wallpaper files per monitor.

**Registry Key**: 
`HKEY_CURRENT_USER\Control Panel\Desktop`
- `WallPaper` - Default wallpaper path
- `TranscodedImageCache` - Current wallpaper (binary)

For per-monitor wallpapers (Windows 10/11):
`HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Wallpapers`

**Wallpaper change detection rule**:
- Poll `IDesktopWallpaper.GetWallpaper()` every 2 seconds to detect wallpaper advances and slideshow transitions.
- `IDesktopWallpaper.GetWallpaper()` is a cheap COM call — no file I/O, no registry scanning.
- Compare cached wallpaper paths per monitor; only fire events when a change is detected.
- This is the only accepted polling mechanism in the codebase.
- `IDesktopWallpaper` is the definitive source of truth for per-monitor wallpaper state.
- Do NOT use `SystemEvents.UserPreferenceChanged`, `SHChangeNotifyRegister`, or `FileSystemWatcher` for wallpaper detection — these have been proven unreliable for multi-monitor slideshow advances.

### 5. UI Components

#### Theme Browser Panel (Left Side)
- ListBox/ListView of available themes
- Theme name and preview thumbnail
- Click to select and apply

#### Monitor LayoutPanel (Main Area)
- Canvas-based rendering of monitor layout
- Each monitor shows:
  - Scaled rectangle matching relative position
  - Current wallpaper as background
  - Monitor number overlay
- **Clicking any monitor** opens that monitor's wallpaper in the default photo viewer (Photos app)
  - Uses `Window.PreviewMouseDown` with coordinate-based hit testing on the Canvas
  - Compares click coordinates against each monitor's canvas bounds
  - `Process.Start` with `UseShellExecute=true` launches the default viewer

## Data Flow

```
1. App Startup
   ↓
2. Theme Service scans directories → Loads Theme objects
   ↓
3. Monitor Service queries Windows APIs → Builds MonitorLayout
   ↓
4. UI binds to collections
   ↓
5. User clicks theme → Theme Service applies → UI updates
6. User clicks monitor → Shell opens wallpaper file
```

## Key Windows APIs

| Purpose | API |
|---------|-----|
| Get monitor info | `EnumDisplayMonitors`, `GetMonitorInfo` |
| Set wallpaper | `SystemParametersInfo(SPI_SETDESKWALLPAPER)` |
| Per-monitor wallpaper | Registry + `IDesktopWallpaper` COM interface |
| Apply theme | `UXTheme.dll` functions or registry + refresh |
| Open file | `Process.Start(filepath)` |

## Design Patterns

- **MVVM**: Standard pattern for WPF/WinUI applications
- **Service Layer**: Separate services for theme, monitor, and wallpaper operations
- **Repository Pattern**: Theme discovery abstracted behind interface
- **Observer Pattern**: INotifyPropertyChanged for reactive UI updates
- **Polling-based wallpaper tracking**: `IDesktopWallpaper.GetWallpaper()` polled every 2 seconds is the only accepted mechanism for wallpaper change detection
- **Observable debugging**: when debugging stalls, add stage-specific diagnostics before attempting more speculative fixes

## Logging and Diagnostics

### Dual-Output Logging Policy

**All diagnostic output MUST be mirrored to both destinations:**
1. **Log files** - Persistent logs at `%LOCALAPPDATA%\WindowsThemeManager\Logs\debug_YYYYMMDD_HHMMSS.log`
   - Each application run creates a new timestamped log file
   - Automatic cleanup keeps only the 10 most recent logs
2. **Console output** - Immediate visibility via `Console.WriteLine` and `System.Diagnostics.Debug.WriteLine`

**Rationale**: Developers need real-time visibility during debugging while maintaining a persistent log for post-mortem analysis. Log cycling prevents disk space issues while preserving recent history.

**Implementation**:
- Use `ILogger<T>` for structured logging (file output)
- Mirror all `ILogger` calls with `Console.WriteLine` and `Debug.WriteLine` for:
  - Service operations (theme discovery, monitor detection, wallpaper changes)
  - COM callback invocations and event delivery
  - Error conditions and exceptions
  - State changes (wallpaper updates, theme applications)
  - Event subscriptions and unsubscriptions

**Example**:
```csharp
// Log to file via ILogger
_logger.LogInformation("Started listening for wallpaper change events");

// Mirror to console and debug output
Console.WriteLine("[MonitorService] Started listening for wallpaper change events");
System.Diagnostics.Debug.WriteLine("[MonitorService] Started listening for wallpaper change events");
```

## Dependencies

- .NET 8.0+
- WPF or WinUI 3 (TBD based on requirements)
- Windows SDK (for Win32 interop)
- No external NuGet packages required initially

## Error Handling

- Graceful degradation if theme directories don't exist
- Fallback for monitors where wallpaper can't be determined
- User-friendly error messages for theme application failures
- Logging for troubleshooting
- Diagnostic logging should clearly identify success/failure boundaries for COM hookup, callback delivery, UI refresh, and image loading when investigating bugs
