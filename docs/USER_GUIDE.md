# User Guide

## Start the application

Build and run from the repository root:

```bash
dotnet run --project src/WindowsThemeManager
```

The application is designed for Windows 10/11 and requires the .NET 10 desktop runtime/SDK for development runs.

## Themes and monitors

The **Themes & Monitors** tab contains the available Windows themes on the left and a relative monitor layout on the right.

### Apply a theme

1. Select a theme in the list.
2. Click the theme item to apply it.
3. Use **Refresh** if a newly installed theme is not listed.

Themes are discovered from these Windows locations and their immediate subdirectories:

- `%LocalAppData%\Microsoft\Windows\Themes`
- `%AppData%\Microsoft\Windows\Themes`
- `%WINDIR%\Resources\Themes`
- `%ProgramData%\Microsoft\Windows\Themes`

### Work with monitor wallpapers

- Click a monitor preview to open its wallpaper in the default Windows image viewer.
- Click the red X to confirm moving that wallpaper to the Recycle Bin.
- A monitor displays **No Wallpaper** when Windows does not report a file or the reported file is no longer available.
- Wallpaper previews refresh when the application detects a change through the Windows desktop wallpaper API.

Use the selector in the status bar to choose **System**, **Light**, or **Dark** colors for the application interface.

## Desktop icon backup and restore

The **Desktop Icons** tab can protect icon positions before display, DPI, or Windows changes rearrange them.

### Create a backup

1. Click **Capture Current Layout**.
2. Review the captured icon count and display information.
3. Click **Save Backup As...** and choose a JSON file location. The Desktop is the default location.

The application also keeps a copy in `%LocalAppData%\WindowsThemeManager\IconLayouts` so it appears in the **Saved Layouts** list.

### Restore a layout

- Use **Restore Captured** for the layout currently held in memory.
- Use **Load File...**, select a JSON backup, and click **Restore Loaded**.
- Select an item in **Saved Layouts** and click **Restore Selected**.

Every restore asks for confirmation before moving desktop icons. The backup stores positions and display metadata; it does not copy the icon files or desktop shortcuts themselves.

## Application data

The application stores settings and diagnostic data under:

```text
%LocalAppData%\WindowsThemeManager\
├── settings.json
├── IconLayouts\
└── Logs\
```

Do not edit these files while the application is running.
