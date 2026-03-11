# UI Test Guide

This repository includes a headless Avalonia UI test harness for the current cross-platform desktop app.

## Scope

The headless UI tests are meant to close the feedback loop for Avalonia UI work without requiring a human to open and inspect windows manually.

Current framework capabilities:

- open Avalonia windows in a headless environment
- wait for async UI loading to settle
- assert against named controls
- switch tabs and observe lazy-loaded content
- emit screenshot artifacts
- emit structured JSON artifacts

Current framework location:

- `Cbdb.App.Avalonia.Tests`

Current first scenario:

- `PersonBrowserWindow_LoadsFixtureData_AndRendersLazyTabs`

## Run

Use:

```bash
DOTNET_CLI_HOME=/tmp dotnet test ./Cbdb.App.Avalonia.Tests/Cbdb.App.Avalonia.Tests.csproj -c Debug -p:UseSharedCompilation=false -maxcpucount:1 -nodeReuse:false
```

If you are running in an environment with stricter sandboxing, you may need elevated permission for `dotnet test` so NuGet restore and test execution can complete.

## Artifacts

Artifacts are written to:

- `artifacts/ui-tests/<test-name>/`

Current outputs include:

- `summary.json`
- `person-browser.png`

These artifacts are useful for:

- checking rendered layout after a UI change
- comparing structure-oriented data across runs
- keeping machine-readable records of what the UI actually rendered

## Current Design

The current framework is intentionally deterministic.

For UI-shape and layout tests, it uses fixture-backed fake services rather than the production SQLite service. This avoids test flakiness caused by local database differences.

The main pieces are:

- `HeadlessTestApp.cs`
  - configures Avalonia headless mode with Skia rendering
- `TestInfrastructure/AvaloniaUiTestHelper.cs`
  - provides wait helpers and artifact writing helpers
- `TestDoubles/FakePersonBrowserService.cs`
  - provides stable fixture data for person-browser UI tests
- `PersonBrowserWindowTests.cs`
  - demonstrates the current end-to-end pattern

## Writing More Tests

Preferred pattern:

1. Create or extend a fake service with deterministic fixture data.
2. Open the target window directly.
3. Wait for UI state to settle.
4. Assert against named controls first.
5. Capture screenshot artifacts when layout matters.
6. Save a small JSON summary when useful.

For example, future tests should cover:

- additional person-browser tabs
- language switching
- long-text wrapping and truncation risks
- split-layout behavior after UI changes

## Real Database Scenarios

Use real SQLite-backed tests only when you specifically need integration coverage.

When doing that:

- provide a known test database path
- avoid relying on the per-user remembered SQLite path
- keep assertions focused on integration behavior rather than exact layout text where data may change

Note that `PersonBrowserWindow` still checks whether `sqlitePath` exists before it starts searching. If a test uses a fake service, it still needs a real placeholder file path unless that production guard is refactored later.

## Maintenance Notes

- Prefer adding named controls in XAML if a UI region needs test coverage.
- Keep headless tests focused and small; one scenario per major user flow is usually enough.
- Do not treat screenshots alone as sufficient coverage; pair them with structural assertions.
