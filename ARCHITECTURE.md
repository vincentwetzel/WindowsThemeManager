# Architecture

## System Overview

Windows Theme Manager is a C# desktop application that provides enhanced theme management capabilities with visual monitor layout, live wallpaper previews, and direct wallpaper actions from each monitor preview.

## High-Level Architecture

The app is organized around a WPF presentation layer, MVVM view models, and service classes that handle theme discovery, monitor layout, wallpaper image loading, and user dialogs.

## Core Components

### Theme Discovery

Scans the standard Windows theme directories and parses `.theme` files into app models.

### Theme Application

Applies the selected theme by updating Windows settings and refreshing the UI state.

### Monitor Detection

Detects connected monitors, their bounds, primary status, and current wallpaper source.

### Wallpaper Management

Tracks current wallpaper files per monitor, loads preview thumbnails, and exposes open/delete actions for the UI.

### Dialog Service

Provides a WPF message-box wrapper for information, warning, error, and confirmation dialogs. The service now falls back to the ownerless `MessageBox.Show(...)` overload when no owner window has been assigned, which prevents null-owner crashes during monitor deletion.

## Monitor Layout UI

- The monitor area uses a Canvas-based layout inside a Viewbox so monitor positions stay proportional.
- Clicking the wallpaper area opens that monitor's wallpaper in the default image viewer.
- Clicking the red X confirms deletion and moves the wallpaper to the Recycle Bin.
- The monitor-area root handles click routing so the interaction remains reliable inside the scaled layout.

## Wallpaper Change Detection

- Wallpaper updates are handled through the existing `IDesktopWallpaper` COM event stream.
- No timers, polling loops, file watchers, or registry refresh jobs are used for wallpaper detection.
- Debugging should focus on COM hookup, callback delivery, UI refresh, and thumbnail loading.

## Data Flow

1. App startup loads themes and monitor layout.
2. View models populate the theme list and monitor canvas.
3. The UI binds to those collections.
4. Theme clicks apply the selected theme.
5. Monitor clicks open the wallpaper or confirm deletion.

## Logging and Diagnostics

- Structured logging is mirrored to file and console/debug output.
- Temporary diagnostics should clearly identify the stage that is failing.
- Keep the output focused on wallpaper loading, monitor routing, and dialog presentation when troubleshooting.

## Design Patterns

- MVVM for UI structure.
- Service layer for theme, monitor, wallpaper, and dialog operations.
- Observer pattern through `INotifyPropertyChanged`.
- Centralized theme resources for all app colors.

## Dependencies

- .NET 8.0+
- WPF
- Windows SDK for Win32 interop
