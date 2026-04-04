# Contributing Guidelines

## Getting Started

### Development Environment

1. **Visual Studio 2022** (Community edition or higher)
   - Workload: .NET desktop development
   - Component: .NET 8.0 SDK

2. **Alternative**: VS Code with C# Dev Kit extension

### Setup

```bash
# Clone the repository
git clone <repository-url>
cd WindowsThemeManager

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/WindowsThemeManager
```

## Code Standards

### C# Conventions

- Use **C# 12+** features where appropriate
- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` when the type is obvious
- Prefer expression-bodied members for simple methods
- Use nullable reference types (enabled by default in .NET 8)

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `ThemeService` |
| Methods | PascalCase | `ApplyThemeAsync` |
| Properties | PascalCase | `ThemeName` |
| Private fields | `_camelCase` | `_themeRepository` |
| Parameters | camelCase | `themePath` |
| Interfaces | Prefix with `I` | `IThemeService` |
| Async methods | Suffix with `Async` | `LoadThemesAsync` |

### File Organization

```
/src
  /WindowsThemeManager           # UI project (WPF/WinUI)
    /ViewModels                  # ViewModels following MVVM
    /Views                       # XAML views
    /Converters                  # Value converters
    /Services                    # UI-specific services
    App.xaml.cs
    MainWindow.xaml.cs

  /WindowsThemeManager.Core      # Core library
    /Models                      # Data models
    /Services                    # Business logic services
    /Interfaces                  # Service interfaces
    /Extensions                  # Extension methods
    /Helpers                     # Utility classes

  /WindowsThemeManager.Tests     # Test project
    /Unit                        # Unit tests
    /Integration                 # Integration tests (if any)
```

### Code Style

```csharp
// ✅ DO: Use async/await for I/O operations
public async Task<Theme> LoadThemeAsync(string themePath)
{
    var content = await File.ReadAllTextAsync(themePath);
    return ParseTheme(content);
}

// ✅ DO: Validate inputs
public void ApplyTheme(Theme theme)
{
    ArgumentNullException.ThrowIfNull(theme);
    // ...
}

// ✅ DO: Use pattern matching
var result = theme switch
{
    { IsValid: true } => ApplyInternal(theme),
    _ => throw new InvalidOperationException("Invalid theme")
};

// ❌ DON'T: Use magic numbers
// Use constants or enums instead
```

## Architecture Guidelines

### MVVM Pattern

- ViewModels must implement `INotifyPropertyChanged`
- Use `ICommand` for user actions
- Keep Views dumb - logic belongs in ViewModels
- Use data binding, avoid code-behind

### Logging Policy

- **All logging output MUST be mirrored to both the debug log file AND console output**
- This ensures developers can see real-time diagnostic information during development and debugging
- Use `Console.WriteLine` and `System.Diagnostics.Debug.WriteLine` in parallel with logger calls for critical diagnostic messages
- The `debug.log` file is the persistent record, but console output provides immediate visibility during active debugging
- When adding new logging statements, always ensure they appear in both destinations
- This applies to:
  - Service operations (theme discovery, monitor detection, wallpaper changes)
  - COM callback invocations
  - Error conditions and exceptions
  - State changes (wallpaper updates, theme applications)
  - Event subscriptions and unsubscriptions

### Log File Management

- **Each application run creates a new log file** with timestamp in the format: `debug_YYYYMMDD_HHMMSS.log`
- Log files are stored at: `%LOCALAPPDATA%\WindowsThemeManager\Logs\`
- **Automatic cleanup**: Only the 10 most recent log files are kept; older files are deleted on startup
- This prevents log accumulation while maintaining history for troubleshooting
- Each run's log includes the Process ID for easy correlation with debugger sessions

### Service Design

- Define interfaces for all services
- Use dependency injection
- Services should be testable (no direct UI dependencies)
- Keep services focused and single-responsibility
- If a feature is stuck in debugging, add targeted debug prints or structured logs that show which stage is succeeding or failing before continuing with more speculative code changes

### Wallpaper Change Detection

**The only accepted approach is polling `IDesktopWallpaper.GetWallpaper()` for each monitor.**

After extensive research, all event-driven approaches have been ruled out:
- `SystemEvents.UserPreferenceChanged` only fires on manual user changes, not automatic slideshow advances
- `IDesktopWallpaper` does NOT implement `IConnectionPointContainer`, so COM callbacks via `Advise()` are impossible (`E_NOINTERFACE`)
- `SHChangeNotifyRegister` does not reliably deliver wallpaper change events
- `FileSystemWatcher` on `TranscodedWallpaper` is unreliable for multi-monitor setups

**Polling rules:**
- Poll interval: **2 seconds** — responsive to wallpaper advances while keeping COM overhead negligible
- `IDesktopWallpaper.GetWallpaper()` is a cheap COM call — no file I/O, no registry scanning
- Compare cached wallpaper paths per monitor; only fire events when a change is detected
- Use `CancellationToken` to support clean shutdown
- This is the **only** accepted use of polling in this codebase; all other change detection must remain event-driven

### Error Handling

```csharp
// Use specific exception types
try
{
    await _themeService.ApplyThemeAsync(theme);
}
catch (ThemeApplyException ex)
{
    _logger.LogError(ex, "Failed to apply theme: {ThemeName}", theme.Name);
    _dialogService.ShowError($"Failed to apply theme: {ex.Message}");
}

// Never swallow exceptions without logging
```

## Git Workflow

### Branch Strategy

- `main` - Stable, release-ready code
- `develop` - Active development branch
- `feature/*` - New features (e.g., `feature/monitor-detection`)
- `bugfix/*` - Bug fixes (e.g., `bugfix/wallpaper-path-parsing`)
- `hotfix/*` - Urgent fixes for production

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add monitor position detection
fix: handle missing wallpaper files gracefully
docs: update architecture diagram
test: add unit tests for theme parser
refactor: extract wallpaper service from theme service
```

### Pull Requests

1. Create feature branch from `develop`
2. Make changes with descriptive commits
3. Push branch and create PR to `develop`
4. Ensure CI passes
5. Request review
6. Merge after approval

## Testing

### Requirements

- All services must have unit tests
- Aim for >80% code coverage on Core library
- Test edge cases (missing files, invalid formats, etc.)
- Verify wallpaper change behavior through COM event subscription tests or manual integration checks, not polling-based scaffolds
- When investigating an unresolved bug, include diagnostics that make callback delivery, refresh execution, and image loading observable in debug output or logs

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

### Writing Tests

```csharp
[TestClass]
public class ThemeParserTests
{
    [TestMethod]
    public async Task ParseThemeAsync_ValidThemeFile_ReturnsTheme()
    {
        // Arrange
        var themePath = "test.theme";
        File.WriteAllText(themePath, ValidThemeContent);
        
        // Act
        var theme = await _parser.ParseThemeAsync(themePath);
        
        // Assert
        Assert.AreEqual("Test Theme", theme.DisplayName);
        Assert.IsNotNull(theme.WallpaperPath);
    }
    
    [TestMethod]
    public async Task ParseThemeAsync_MissingFile_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _parser.ParseThemeAsync("nonexistent.theme"));
    }
}
```

## Documentation

- Update relevant MD files for significant changes
- Comment complex algorithms or non-obvious code
- XML documentation required for public APIs
- Update ARCHITECTURE.md when changing system design

## Code Review Checklist

- [ ] Code follows project conventions
- [ ] No unnecessary complexity added
- [ ] Error cases handled appropriately
- [ ] Tests included and passing
- [ ] No hardcoded values or TODO comments left in code
- [ ] Sensitive data not committed
- [ ] Documentation updated if needed

## Questions?

- Check ARCHITECTURE.md for system design details
- Check IMPLEMENTATION_PLAN.md for current development tasks
- Open an issue for clarification on behavior
