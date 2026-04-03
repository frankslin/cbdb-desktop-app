# CBDB Desktop Version 0.3.1-beta1

This release expands the query workflow beyond the 0.2.1-rc1 baseline, with a new module, clearer multilingual UI behavior, and more usable place/address selection.

The app now supports three user-facing modules: **Person Browser**, **Status Query**, and **Entry Query**.

## Highlights
- Added a new **Entry Query** module, extending the app beyond the earlier Person Browser and Status Query toolset
- Improved interface readability and consistency, including locale-specific font handling and clearer language-selection behavior
- Refined place/address selection and related query interactions to make address-based filtering easier to use

## Improvements
- Query-side picker windows have been refined for clearer selection flow and better large-list usability
- Traditional Chinese display is improved through bundled Noto Sans TC support on Windows
- About-window notices and release metadata have been updated for the current beta version
- Ongoing interface polish improves consistency across module windows and selection dialogs

## System Requirement
- Windows 10 22H2 or later
- macOS 12.0 or later

### macOS Note
- On macOS, Gatekeeper may block the app when opening a downloaded build for the first time
- If needed, remove the quarantine attribute manually before launching:
  `xattr -dr com.apple.quarantine /path/to/CBDB\ Desktop.app`

# CBDB Desktop Version 0.2.1-rc1

This release improves the Person Viewer, Kinship browsing, Status query tools, and overall display stability.

The app currently supports two modules: **Person Browser** and **Status Query**.

## Highlights
- Kinship browsing is now more powerful, with clearer relationship grouping and support for expanded kinship networks
- Person Browser now supports importing and exporting person lists by file
- Status Query is easier to use, with improved category selection, place filtering, and clearer result tabs
- The app can now automatically add recommended SQLite indexes to help speed up postings queries
- Display behavior has been improved in dark-mode environments; at this stage the app uses light mode only for better readability

## Improvements
- Place and dynasty selection have been refined to make filtering more accurate and easier to understand
- General interface polish improves readability and consistency across windows
- Official release packaging is now more consistent and better tested

## System Requirement
- Windows 10 22H2 or later
- macOS 12.0 or later

### macOS Note
- On macOS, Gatekeeper may block the app when opening a downloaded build for the first time
- If needed, remove the quarantine attribute manually before launching:
  `xattr -dr com.apple.quarantine /path/to/CBDB\ Desktop.app`
