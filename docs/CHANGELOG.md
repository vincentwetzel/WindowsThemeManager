# Changelog

## Unreleased

- Fixed monitor preview interactions so clicking a wallpaper opens it in the default photo viewer.
- Fixed the red X action on monitor previews so it confirms deletion and moves the wallpaper to the Recycle Bin.
- Hardened dialog presentation so confirmation and error dialogs work even when no owner window has been assigned.
- Added clearer runtime feedback for monitor-layout clicks and wallpaper open/delete failures.
- Fixed monitor refreshes so wallpaper updates are matched by monitor device path instead of enumeration order.
- Preserved existing monitor previews when Windows transiently reports an empty wallpaper path during slideshow transitions.
- Improved wallpaper preview compatibility by avoiding reduced-dimension decoding for Windows-transcoded and slideshow cache images.
