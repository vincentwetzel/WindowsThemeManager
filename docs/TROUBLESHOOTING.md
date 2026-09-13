# Troubleshooting

## No themes are listed

1. Confirm that Windows theme files exist in one of the locations listed in the [User Guide](USER_GUIDE.md).
2. Click **Refresh** in the Themes & Monitors tab.
3. Confirm the files have a `.theme` extension and are readable by the current Windows user. Packaged theme files such as `.deskthemepack` are not scanned.
4. Check the latest log under `%LocalAppData%\WindowsThemeManager\Logs` for scan or parse warnings.

The scanner checks each standard directory and its immediate child directories. It does not recursively scan every nested folder.

## A theme cannot be applied

The selected file must still exist and be a valid `.theme` file. The application delegates complete-theme application to Windows; it does not apply visual styles, cursor schemes, sounds, or wallpapers as separate operations. If Windows rejects the file, try opening it from File Explorer or select another theme.

## A monitor shows “No Wallpaper”

The Windows desktop wallpaper API may return no path, or the file may have been moved or deleted. Try **Refresh**, then verify that the wallpaper file still exists and can be opened from File Explorer.

For changes made outside the application, allow up to a few seconds for the background state check to notice the new wallpaper. The monitor service polls the Windows desktop wallpaper API every two seconds.

During a slideshow transition Windows may briefly report an empty wallpaper path. The application preserves the last usable preview until a non-empty path is available.

## Clicking a preview does nothing or shows an error

The preview action requires a valid wallpaper file and a Windows application associated with its file type. Verify the file exists and opens normally from File Explorer. If the association is missing, choose a default app in Windows Settings.

## Deleting a wallpaper fails

The delete action first checks that the reported file exists and then asks for confirmation. If the file was moved, is read-only or locked by another process, or the Recycle Bin operation fails, the application reports an error. Close applications using the image and try again.

Successful deletion moves the file to the Recycle Bin, where it can normally be restored through Windows.

## Desktop icon capture or restore fails

- Keep Windows Explorer running; the feature communicates with the Explorer desktop ListView.
- Run the application in the same user session as the desktop whose icons are being managed.
- Capture a fresh layout after changing monitor connections or DPI scaling.
- Restore only a backup created for a compatible display arrangement.
- Check the status message and the application log for Explorer access or platform-operation errors.

## The application starts with default settings

Settings are loaded from `%LocalAppData%\WindowsThemeManager\settings.json`. If the file is missing, invalid, or inaccessible, the application falls back to defaults. Close the application before repairing or removing the file, then restart it.

## Collect useful diagnostics

The application creates timestamped `debug_*.log` files in `%LocalAppData%\WindowsThemeManager\Logs` and keeps the ten most recent files. When reporting a problem, include:

- Windows version and display configuration
- The action that failed and the visible error/status message
- The approximate time of the failure
- The relevant log file, after removing any sensitive file paths if needed
