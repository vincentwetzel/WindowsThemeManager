# Contributing Guidelines

## Getting Started

### Development Environment

1. Visual Studio 2022 or later
   - Workload: .NET desktop development
   - Component: .NET 8.0 SDK

2. Alternative: VS Code with C# Dev Kit

### Setup

```bash
git clone <repository-url>
cd WindowsThemeManager
dotnet restore
dotnet build
dotnet run --project src/WindowsThemeManager
```

## Code Standards

### C# Conventions

- Use C# 12+ features where appropriate
- Follow Microsoft's C# coding conventions
- Use `var` when the type is obvious
- Prefer expression-bodied members for simple methods
- Keep nullable reference types enabled

### Architecture Guidelines

- Use MVVM for views and view models
- Keep logic out of XAML code-behind unless it is input routing or view-specific plumbing
- Use dependency injection for services
- Keep services focused and testable

### Logging Policy

- Mirror important logging to file and console/debug output
- Include diagnostics for COM hookup, callback delivery, image loading, and UI refresh stages when debugging monitor or wallpaper issues
- Remove temporary diagnostics after the bug is resolved

### Wallpaper Change Detection

- Use the existing `IDesktopWallpaper` COM event stream for wallpaper change detection
- Do not introduce polling-based wallpaper tracking
- If the wallpaper refresh flow breaks, fix the event path rather than adding timers or file watchers

## Testing

- Add or update tests for behavior changes when practical
- Verify monitor interactions manually after changes to the preview or delete flow
- Check that dialogs still work when no owner window is set

## Documentation

- Update the relevant MD files for significant changes
- Add a changelog entry for user-visible behavior changes
- Update `ARCHITECTURE.md` when changing system design

## Git Workflow

- Create a feature branch from the active branch when needed
- Make focused commits with descriptive messages
- Push branch and open a PR if the repository uses that flow
