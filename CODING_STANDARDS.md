# Coding Standards

This document is mandatory for all code changes in Windows Theme Manager. It applies to human contributors and coding agents. When existing code conflicts with a rule here, follow this document for new or modified code and call out the inconsistency in the change description.

## Before changing code

1. Read this file, `README.md`, and the relevant document under `docs/`.
2. Inspect the affected service, view model, interface, and tests before editing.
3. Check `git status` and preserve unrelated worktree changes.
4. Keep the change focused. Do not reformat or rewrite unrelated files.

## Project conventions

- Use C# with nullable reference types enabled and implicit usings enabled.
- Target `net10.0-windows`; use APIs supported by the target framework and Windows 10/11.
- Follow standard Microsoft C# naming: PascalCase for types, methods, properties, and public members; camelCase for parameters and local variables; `_camelCase` for private fields.
- Prefer one public type per file. Name the file after the primary type.
- Use explicit, descriptive names. Avoid unexplained abbreviations and numeric or boolean "magic" values.
- Keep methods small and cohesive. Prefer guard clauses and early returns over deeply nested conditionals.
- Use `var` when the type is clear from the expression; use an explicit type when it improves clarity or the type is not obvious.
- Use expression-bodied members only when they remain easy to read.
- Do not suppress nullable warnings without a documented, verified reason.
- Add comments only for non-obvious rationale, platform behavior, or important invariants; do not narrate straightforward code.

## Architecture and boundaries

- Keep the WPF application in `src/WindowsThemeManager`, reusable application and platform logic in `src/WindowsThemeManager.Core`, and tests in `src/WindowsThemeManager.Tests`.
- Follow MVVM. View models coordinate state and commands; services own operations and external effects; XAML code-behind is limited to view-specific input or lifecycle wiring that cannot reasonably belong elsewhere.
- Introduce or update an interface when a service is consumed through dependency injection or needs a test seam.
- Keep services focused, injectable, and testable. Do not hide significant work in static global state.
- Keep UI updates on the UI thread. Run genuinely expensive or blocking work asynchronously and avoid blocking the dispatcher.
- Pass `CancellationToken` through new cancellable asynchronous operations where practical.
- Do not add a second mechanism for wallpaper-change detection. The authoritative path is the `IDesktopWallpaper` polling loop described in `docs/ARCHITECTURE.md`.
- Isolate Win32, COM, registry, filesystem, and shell integration behind focused helpers or services. Validate external data and handle expected platform failures with useful diagnostics.

## WPF and XAML

- Prefer bindings, commands, converters, and resources over event handlers and duplicated presentation logic.
- Use `INotifyPropertyChanged` and observable collections consistently with the existing CommunityToolkit.Mvvm patterns.
- Keep colors and reusable visual values in the application resource dictionaries or theme resources; do not duplicate literals across views.
- Preserve accessibility: controls need meaningful labels, keyboard interaction, sensible focus behavior, and adequate contrast.
- Keep long-running or file/COM operations out of constructors and property getters.

## Error handling, logging, and data

- Validate paths, files, COM results, and user input at the boundary where they enter the application.
- Catch exceptions only when the code can recover, add context, or translate the failure into an expected user-facing result. Never silently swallow failures.
- Use the configured logger for important diagnostics. Include the failing stage and relevant non-sensitive context; never log secrets or unnecessary personal data.
- Preserve user data by preferring undoable or recoverable operations. Destructive actions require explicit user confirmation and must use the existing Recycle Bin/undo behavior where applicable.
- Keep settings and layout serialization backward-compatible when practical. Treat malformed persisted data as recoverable input and provide a safe fallback.

## Testing

- Add or update unit tests for behavior changes, especially models, parsers, scanners, settings, monitor logic, and service coordination.
- Name tests for the behavior and expected result, for example `Parse_ThemeFileWithMissingWallpaper_ReturnsThemeWithoutWallpaper`.
- Keep tests deterministic and isolated. Do not depend on the developer's desktop, monitor arrangement, wallpaper, user profile, or network.
- Prefer testing through public behavior and injected interfaces. Avoid tests that require real Win32/COM state when a seam can be used.
- Before handing off a change, run:

  ```powershell
  dotnet restore
  dotnet build
  dotnet test
  ```

- When platform behavior changes, also perform the relevant manual Windows verification and document what was checked.

## Documentation and change hygiene

- Update `docs/ARCHITECTURE.md` when boundaries, platform integration, or data flow changes.
- Update `docs/USER_GUIDE.md` or `docs/TROUBLESHOOTING.md` for user-facing behavior or recovery changes.
- Add a `docs/CHANGELOG.md` entry for user-visible changes.
- Keep README links, commands, and project structure current.
- Do not commit build output, local settings, logs, secrets, or machine-specific paths.
- Use focused commits and describe behavior changes, testing performed, and any Windows-only limitations.

## Rules for coding agents

Coding agents must read this file before inspecting or editing implementation files. Agents must:

- follow all applicable rules above;
- inspect existing code and tests before proposing or applying a change;
- preserve unrelated user changes;
- ask for clarification when a required design choice materially changes scope or user-visible behavior;
- run appropriate validation after edits and report failures honestly;
- update related documentation when the change requires it; and
- never bypass tests, disable safety checks, or make destructive changes merely to force a build or test to pass.

If a task explicitly conflicts with a rule, the task instruction wins for that task; the agent must identify the exception and limit it to the smallest necessary scope.
