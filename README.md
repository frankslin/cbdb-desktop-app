# CBDB Windows App

This repository contains the CBDB desktop app workspace for SQLite browsing.

## Projects

- `Cbdb.App.Desktop`: legacy WPF desktop shell kept only as migration/form-design reference, no longer included in the solution
- `Cbdb.App.Avalonia`: current cross-platform desktop shell for macOS/Windows
- `Cbdb.App.Core`: shared interfaces/models
- `Cbdb.App.Data`: SQLite data access services

## Build Instructions

Detailed cross-platform build and test steps:

- `BUILD_GUIDE.md`
- `UI_TEST_GUIDE.md`

### Prerequisites

- Windows 10 22H2 or later, or macOS 12.0 or later
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

On macOS, if Gatekeeper blocks the downloaded app bundle, remove the quarantine attribute before launching:

```bash
xattr -dr com.apple.quarantine /path/to/Cbdb.app
```

### Run Headless UI Tests

```bash
DOTNET_CLI_HOME=/tmp dotnet test ./Cbdb.App.Avalonia.Tests/Cbdb.App.Avalonia.Tests.csproj -c Debug -p:UseSharedCompilation=false -maxcpucount:1 -nodeReuse:false
```

Headless UI test artifacts are written under:

- `artifacts/ui-tests/`

### Open in Visual Studio

Open `Cbdb.WindowsApp.sln`, set `Cbdb.App.Avalonia` as startup project for current work, press `F5`.


## SQLite Location

Default bundled DB location:

- `data/cbdb_20260304.sqlite3`

On startup, the app auto-loads this path into the SQLite textbox.
For backward compatibility, it still falls back to `../cbdb-sqlite-db/cbdb_20260304.sqlite3` if present.
## Current App Scope

- The current app supports four usable modules:
  - `Person Browser`
  - `Status Query`
  - `Entry Query`
  - `Office Query`
- The Avalonia app is the primary desktop shell on both Windows and macOS
- `Cbdb.App.Desktop` remains in the repository only as legacy reference material

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

- `Cbdb.App.Avalonia` is now the primary desktop shell and publish target.
- `Cbdb.App.Avalonia.Tests` provides a headless Avalonia UI test harness for deterministic window rendering, tab interaction, screenshot capture, and structured test artifacts.
- Current ported features:
  - main navigation window
  - English / Traditional Chinese / Simplified Chinese switch
  - SQLite file picker using cross-platform storage APIs
  - SQLite health check using shared data services
  - external link / user-guide open actions
  - Person Browser search, summary, Basic Information, file import/export, and repeated-form related tabs
  - Status Query with hierarchical status selection, place filtering, export, and result tabs
  - Entry Query with hierarchical entry selection, place filtering, export, and result tabs
  - Office Query with hierarchical office selection, split people-place / office-place filters, export, and result tabs
  - headless UI coverage for Person Browser and injected query-window paths
- Not yet ported:
  - several other home-page query modules
  - full Access-equivalent workflows for Status / Entry / Office
  - GIS / KML / Neo4j style export paths

## Release Process

- Manual releases are created through `.github/workflows/release.yml`
- Release notes are maintained in `RELEASE_NOTES.md`
- Official release packaging includes:
  - `win-x64`
  - `osx-arm64`
  - `osx-x64`

## Next Implementation Steps

1. Continue tightening `Status`, `Entry`, and `Office` toward closer Access-equivalent workflows
2. Expand query-window headless UI coverage and fixture-backed integration tests
3. Port the next home-page module that can reuse the existing query shell and picker patterns
4. Add secondary feature: address rank editing + index address rebuild
