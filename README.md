# CBDB Windows App

This subdirectory contains a new Windows desktop app workspace for CBDB SQLite browsing.

## Projects

- `Cbdb.App.Desktop`: WPF desktop shell
- `Cbdb.App.Core`: shared interfaces/models
- `Cbdb.App.Data`: SQLite data access services

## Current bootstrap state

- Solution and 3 projects created
- Desktop app can pick a SQLite file and run a connection/count health check
- Count checks currently cover: `BIOG_MAIN`, `ALTNAME_DATA`, `KIN_DATA`, `ASSOC_DATA`

## Next implementation steps

1. Add Browser page (person search + detail tabs)
2. Implement LookAt query framework
3. Add export flows (CSV/HTML)
4. Add secondary feature: address rank editing + index address rebuild

## i18n scaffold

The app now includes an initial localization architecture:

- `Cbdb.App.Core/ILocalizationService.cs`
- `Cbdb.App.Core/UiLanguage.cs`
- `Cbdb.App.Desktop/Localization/AppLocalizationService.cs`

Supported languages in scaffold:

- English
- Traditional Chinese
- Simplified Chinese

Main window texts are key-driven via localization service (no hard-coded UI strings for core navigation labels).
