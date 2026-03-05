# CBDB Windows App

This subdirectory contains a new Windows desktop app workspace for CBDB SQLite browsing.

## Projects

- `Cbdb.App.Desktop`: WPF desktop shell
- `Cbdb.App.Core`: shared interfaces/models
- `Cbdb.App.Data`: SQLite data access services

## Build Instructions

### Prerequisites

- Windows 10/11
- .NET SDK 8.x
- (Optional) Visual Studio 2022 Community for IDE debugging

### Clone and Build

```powershell
git clone <repo-url>
cd cbdb-windows-app

dotnet restore

dotnet build Cbdb.WindowsApp.sln -c Debug
```

### Run

```powershell
dotnet run --project .\Cbdb.App.Desktop\Cbdb.App.Desktop.csproj
```

### Open in Visual Studio

Open `Cbdb.WindowsApp.sln`, set `Cbdb.App.Desktop` as startup project, press `F5`.


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

## Next Implementation Steps

1. Add Browser page (person search + detail tabs)
2. Implement LookAt query framework
3. Add export flows (CSV/HTML)
4. Add secondary feature: address rank editing + index address rebuild



