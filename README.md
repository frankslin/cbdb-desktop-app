# CBDB Windows App

This repository contains the CBDB desktop app workspace for SQLite browsing.

## Projects

- `Cbdb.App.Desktop`: legacy WPF desktop shell
- `Cbdb.App.Avalonia`: current cross-platform desktop shell for macOS/Windows
- `Cbdb.App.Core`: shared interfaces/models
- `Cbdb.App.Data`: SQLite data access services

## Build Instructions

Detailed cross-platform build and test steps:

- `BUILD_GUIDE.md`
- `UI_TEST_GUIDE.md`

### Prerequisites

- Windows 10/11
- .NET SDK 8.x
- (Optional) Visual Studio 2022 Community for IDE debugging

### Clone and Build

```powershell
git clone https://github.com/frankslin/cbdb-windows-app
cd cbdb-windows-app

dotnet restore

dotnet build Cbdb.WindowsApp.sln -c Debug
```

### Run Avalonia App

```bash
dotnet run --project ./Cbdb.App.Avalonia/Cbdb.App.Avalonia.csproj
```

### Run Headless UI Tests

```bash
DOTNET_CLI_HOME=/tmp dotnet test ./Cbdb.App.Avalonia.Tests/Cbdb.App.Avalonia.Tests.csproj -c Debug -p:UseSharedCompilation=false -maxcpucount:1 -nodeReuse:false
```

Headless UI test artifacts are written under:

- `artifacts/ui-tests/`

### Run Windows WPF App

```powershell
dotnet run --project .\Cbdb.App.Desktop\Cbdb.App.Desktop.csproj
```

### Open in Visual Studio

Open `Cbdb.WindowsApp.sln`, set `Cbdb.App.Avalonia` as startup project for current work, press `F5`.


## SQLite Location

Default bundled DB location:

- `data/cbdb_20260304.sqlite3`

On startup, the app auto-loads this path into the SQLite textbox.
For backward compatibility, it still falls back to `../cbdb-sqlite-db/cbdb_20260304.sqlite3` if present.
## Current Bootstrap State

- Solution and 3 projects created
- Desktop app provides a `NAVIGATION_PANE`-style home page
- SQLite file selection + health check are wired
- Count checks currently cover: `BIOG_MAIN`, `ALTNAME_DATA`, `KIN_DATA`, `ASSOC_DATA`

## i18n Scaffold

The app includes an initial localization architecture:

- `Cbdb.App.Core/ILocalizationService.cs`
- `Cbdb.App.Core/UiLanguage.cs`
- `Cbdb.App.Desktop/Localization/AppLocalizationService.cs`

Supported languages in scaffold:

- English
- Traditional Chinese
- Simplified Chinese

Main window texts are key-driven via localization service.

## Avalonia Migration Bootstrap

- `Cbdb.App.Avalonia` is now the primary desktop shell and the Windows CI publish target.
- `Cbdb.App.Avalonia.Tests` provides a headless Avalonia UI test harness for deterministic window rendering, tab interaction, screenshot capture, and structured test artifacts.
- Current ported features:
  - main navigation window
  - English / Traditional Chinese / Simplified Chinese switch
  - SQLite file picker using cross-platform storage APIs
  - SQLite health check using shared data services
  - external link / user-guide open actions
  - person browser search, summary, Basic Information, and counted tab shell
  - initial headless UI coverage for the Avalonia person browser using fixture-backed fake services
- Not yet ported:
  - query module windows
  - full related-tab content implementations
  - full query-module parity with WPF/Access

## Next Implementation Steps

1. Add Browser page (person search + detail tabs)
2. Implement LookAt query framework
3. Add export flows (CSV/HTML)
4. Add secondary feature: address rank editing + index address rebuild
