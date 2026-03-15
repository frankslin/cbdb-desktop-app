# Build Guide

This repository currently contains two desktop shells with different platform targets:

- `Cbdb.App.Desktop`: WPF, Windows only, kept only as legacy reference and no longer included in `Cbdb.WindowsApp.sln`
- `Cbdb.App.Avalonia`: Avalonia, cross-platform shell for macOS and Windows, and the current publish target

Shared libraries:

- `Cbdb.App.Core`: shared models and interfaces
- `Cbdb.App.Data`: SQLite data access services

## Current Platform Matrix

| Project | macOS | Windows |
| --- | --- | --- |
| `Cbdb.App.Core` | Supported | Supported |
| `Cbdb.App.Data` | Supported | Supported |
| `Cbdb.App.Avalonia` | Supported | Supported |
| `Cbdb.App.Desktop` | Not supported | Reference only |

## Prerequisites

Install:

- .NET SDK 8.x

Recommended checks:

```bash
dotnet --info
dotnet --list-sdks
```

Expected baseline:

- SDK `8.0.x`

## Repository Setup

From the repository root:

```bash
git clone https://github.com/frankslin/cbdb-windows-app
cd cbdb-windows-app
dotnet restore
```

The solution now restores only:

- `Cbdb.App.Core`
- `Cbdb.App.Data`
- `Cbdb.App.Avalonia`
- `Cbdb.App.Avalonia.Tests`

If you are using a local SDK unpacked outside the default install path, ensure `dotnet` is on `PATH` first.

## SQLite Test Data

Preferred bundled path:

- `data/cbdb_20260304.sqlite3`

Fallback logic also searches nearby `data/` folders from the app base directory and current working directory.

The Avalonia app also remembers the last successfully opened SQLite path using the platform's local app-data directory:

- Windows: `%LocalAppData%/CbdbApp/settings.json`
- macOS: `~/Library/Application Support/CbdbApp/settings.json`

On startup, it first tries that remembered path. If that path is missing or no longer passes the health check, it falls back to the bundled `data/` auto-detection logic.

## macOS Guide

### What You Can Test on macOS

You can build and run:

- `Cbdb.App.Core`
- `Cbdb.App.Data`
- `Cbdb.App.Avalonia`

You cannot build or run:

- `Cbdb.App.Desktop` because it targets `net8.0-windows` and WPF, and it is no longer part of the solution

### Build on macOS

```bash
dotnet build Cbdb.App.Core/Cbdb.App.Core.csproj -c Debug
dotnet build Cbdb.App.Data/Cbdb.App.Data.csproj -c Debug
dotnet build Cbdb.App.Avalonia/Cbdb.App.Avalonia.csproj -c Debug
```

### Run on macOS

```bash
dotnet run --project ./Cbdb.App.Avalonia/Cbdb.App.Avalonia.csproj
```

### Manual Test Checklist on macOS

1. Launch the Avalonia app.
2. Confirm the main navigation window opens.
3. Switch among `EN`, `繁`, and `简`.
4. Verify the header, module labels, and side buttons update language.
5. Click `Relink Tables / Change Dataset` or its localized equivalent.
6. Select a valid SQLite file from `data/`.
7. Confirm the status changes to connected and the output shows row counts.
8. Click `Report an Error` and confirm the browser opens.
9. Click `Users Guide` and confirm the browser opens:
   English UI: `https://cbdb-project.github.io/cbdb-user-guide`
   Chinese UI: `https://cbdb-project.github.io/cbdb-user-guide/zh-TW/`
10. Click module buttons and confirm the shell shows porting placeholder messages rather than crashing.

### Expected macOS Result

- Avalonia app should build and run.
- WPF project should remain untestable on macOS by design.

## Windows Guide

### What You Can Test on Windows

You can build and run from the solution:

- `Cbdb.App.Core`
- `Cbdb.App.Data`
- `Cbdb.App.Avalonia`
- `Cbdb.App.Avalonia.Tests`

You can still inspect `Cbdb.App.Desktop` as legacy reference material, but it is no longer part of the normal build/release path.

### Build on Windows

PowerShell:

```powershell
dotnet build .\Cbdb.WindowsApp.sln -c Debug
```

### Run on Windows

Run Avalonia shell:

```powershell
dotnet run --project .\Cbdb.App.Avalonia\Cbdb.App.Avalonia.csproj
```

### Manual Test Checklist on Windows

For Avalonia:

1. Run the Avalonia app.
2. Repeat the same language, SQLite picker, health check, external link, and user guide checks listed in the macOS section.

### Expected Windows Result

- The Avalonia app and test project should build from the solution.
- Windows CI publishes the Avalonia app as the packaged `win-x64` artifact.
- `Cbdb.App.Desktop` remains available only for local comparison and migration reference.

## Known Issues and Boundaries

- `Cbdb.App.Desktop` is Windows-only because it uses WPF and targets `net8.0-windows`.
- `Cbdb.App.Avalonia` is still partial at this stage.
- The `Person Browser` shell has been ported, but related-tab content and query-module windows are still incomplete.
- If `data/cbdb_20260304.sqlite3` is missing, the app will require manual file selection.

## Troubleshooting

### `dotnet: command not found`

Install .NET SDK 8.x and ensure the `dotnet` binary is on `PATH`.

### NuGet restore fails

Check:

- network access to `https://api.nuget.org/v3/index.json`
- local certificate trust and proxy settings

Then retry:

```bash
dotnet restore
```

### Windows project fails on macOS

That is expected. `Cbdb.App.Desktop` targets Windows only and is no longer part of the solution build.

### SQLite health check fails

Check:

- the selected file exists
- the file is a valid SQLite CBDB database
- tables such as `BIOG_MAIN`, `ALTNAME_DATA`, `KIN_DATA`, and `ASSOC_DATA` exist

## Suggested Test Order

1. Build `Core`
2. Build `Data`
3. Build the platform shell you intend to test
4. Run with bundled SQLite data
5. Verify language switch
6. Verify dataset selection and health check
7. Verify module launch or placeholder behavior
