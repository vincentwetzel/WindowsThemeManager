# Architecture

## System overview

Windows Theme Manager is a Windows WPF application that combines theme discovery and application, multi-monitor wallpaper inspection, and desktop icon layout backup/restore.

## High-level architecture

The application is organized into three projects:

- `WindowsThemeManager`: WPF views, application startup, UI resources, and view models.
- `WindowsThemeManager.Core`: models, interfaces, theme and monitor services, settings, and Windows interop.
- `WindowsThemeManager.Tests`: unit tests for core services and models.

The UI follows MVVM. View models coordinate user actions while services own theme, monitor, wallpaper-image, dialog, settings, and desktop-icon operations.

## Core components

### Theme discovery and application

`ThemeDirectoryScanner` synchronously searches the standard Windows theme locations and their immediate subdirectories for `.theme` files. `ThemeFileParser` reads the display name and wallpaper metadata needed by the UI. `ThemeService` caches discovered themes for five minutes, sorts them by display name, validates the selected theme file, and coordinates application through `ThemeApplier`.

`ThemeApplier` delegates complete-theme application to the Windows shell through the native theme helper. It broadcasts a settings change and waits briefly for Windows to process the update. There is no separate application path for individual visual styles, cursors, sounds, or wallpapers.

### Monitor and wallpaper management

`MonitorService` uses `IDesktopWallpaper` to enumerate monitor device paths and per-monitor wallpaper paths, and `Screen.AllScreens` to provide bounds and primary status. It preserves the ordering reported by Windows so monitor numbers and wallpaper paths remain associated with the same display. `MainViewModel` normalizes those bounds onto an 800 × 600 canvas hosted by a WPF `Viewbox`.

Wallpaper state is currently detected by querying `IDesktopWallpaper` on a background polling loop and comparing results with an in-memory cache. The current interval is two seconds. When a change is found, the view model clears image caches and refreshes the affected monitor by device path. A transient empty path reported while Windows swaps a slideshow image does not overwrite a usable path or preview; a later non-empty result completes the update.

Wallpaper change notifications use the polling path exclusively; no second event-driven detection mechanism is registered.

### Wallpaper actions

`MonitorItemViewModel` exposes commands to open an existing wallpaper with `UseShellExecute` and to move it to the Recycle Bin through `SHFileOperation` with undo enabled. Both actions validate that the wallpaper path exists before acting.

`WallpaperImageService` keeps the original decoded image dimensions for previews and lets the WPF `Image` control scale the result. This avoids decode failures for some valid Windows-transcoded or slideshow cache images.

### Desktop icon layouts

`DesktopIconService` reads and writes desktop icon positions through the Windows Explorer desktop ListView API. Layouts are serialized as JSON. The view model supports in-memory capture/restore, file save/load, and a managed list of layouts in the application data directory.

### Settings and diagnostics

The supported settings are window dimensions, maximized state, theme panel width, and application color mode. They are serialized as JSON under `%LocalAppData%\WindowsThemeManager\settings.json`. Startup diagnostics are written to `%LocalAppData%\WindowsThemeManager\Logs`; the application keeps the ten most recent debug logs.

## Data flow

1. Application startup creates the dependency-injection container and loads settings.
2. `MainViewModel` starts wallpaper monitoring and loads themes and monitor layout in parallel.
3. The view binds to theme and monitor collections and loads monitor thumbnails asynchronously.
4. Selecting a theme invokes the theme service and then reloads monitor state.
5. Wallpaper changes invalidate the image cache and refresh the affected monitor preview.
6. Desktop icon commands capture or restore Explorer ListView coordinates and serialize backups when requested.

## Design patterns

- MVVM for UI structure and state binding.
- Service interfaces for testable platform and application operations.
- `INotifyPropertyChanged` for observable state.
- Dependency injection through `Microsoft.Extensions.DependencyInjection`.
- Centralized WPF resource brushes for application color themes.

## Platform dependencies

- .NET 10 for Windows
- WPF
- Windows `IDesktopWallpaper` COM API
- Win32 APIs for wallpaper deletion and desktop icon layout operations
