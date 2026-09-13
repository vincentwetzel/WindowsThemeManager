# Implementation Plan

## Project status: Complete

This document is retained as an implementation record and completed roadmap for Windows Theme Manager.

## Completed work

### Project foundation

- Solution and projects created.
- MVVM and dependency injection configured.
- Core models and services implemented.

### Theme discovery and application

- Theme directory scanning and `.theme` parsing implemented.
- Theme discovery and caching implemented.
- Complete `.theme` application delegated to Windows, with cache invalidation after successful application.

### Monitor detection

- Monitor enumeration implemented.
- Per-monitor wallpaper lookup and preview generation implemented.
- Wallpaper updates are detected by the current `IDesktopWallpaper` polling path.

### UI implementation

- Theme browser and monitor layout implemented.
- Clicking monitor previews opens wallpapers in the default viewer.
- Clicking the red X confirms deletion and moves wallpapers to the Recycle Bin.
- Desktop icon capture, backup, load, restore, and deletion flows implemented.
- Loading states, theme modes, and responsive layout implemented.

### Integration and refinement

- Application lifecycle wiring completed.
- Settings persistence completed for supported settings.
- Refresh and update flows completed.
- Unit coverage added for core parsers, scanners, settings, layout models, and service coordination where platform seams permit.

## Current notes

- `IDesktopWallpaper` is the source of truth for per-monitor wallpaper state.
- The active change-detection implementation polls that API every two seconds.
- Theme application uses the Windows shell's complete `.theme` workflow; individual visual-style, cursor, sound, and wallpaper application paths are intentionally not maintained.
- When debugging, add stage-specific diagnostics for platform queries, refresh execution, image loading, and file actions.

## Success criteria

- Themes are discovered and listed.
- One-click theme application works reliably.
- All monitors are displayed in correct relative positions.
- Current wallpapers are visible when their files are available.
- Monitor previews open wallpapers and the delete action uses the Recycle Bin.
- Desktop icon layouts can be captured, saved, loaded, and restored.
- Edge cases produce useful status messages or dialogs.
- Documentation stays in sync with user-visible behavior.
