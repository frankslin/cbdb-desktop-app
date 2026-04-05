# CBDB Desktop Version 0.3.3-beta1

This release expands the desktop app from the earlier query baseline into a broader, more stable research toolset. It adds a new group-based query module, deepens office-query behavior, improves picker and scrolling performance, and introduces signed and notarized macOS packaging.

The app now supports five user-facing modules:
- **Person Browser**
- **Status Query**
- **Entry Query**
- **Office Query**
- **Look Up Data on a Group of People**

## Highlights
- Added a new **Look Up Data on a Group of People** module for loading a person-ID set and expanding related status, office, entry, text, and address results
- Deepened **Office Query** with richer place-workflow behavior and clearer derived result fields for people-place and office-place matching
- Switched dynasty filtering in query modules from a simple range to a **multi-select picker**, so users can choose non-contiguous dynasties directly
- Improved scrolling and incremental loading in large browser and picker views, especially for the person browser and place-selection dialogs
- The app now attempts to check GitHub releases for updates at startup, and the **About** window lets users run the check again manually
- macOS packaging now produces **signed, notarized DMG packages** for both Apple Silicon and Intel builds

## Improvements
- Office Query result tables now expose clearer place-match and place-workflow fields, and person-level aggregation preserves multi-place semantics more accurately
- Status, Entry, and Office query windows now have stronger whole-window filter coverage in the headless Avalonia test suite
- Query-side pickers continue to converge toward a shared interaction model, with better search, selection, and large-list behavior
- Dynasty ordering rules were refined further, including placing `高麗 / 高丽` with the end-grouped external dynasties
- Place picker and related browser lists now use more incremental loading paths to reduce UI stalls during scrolling
- The new group-people module received layout and workflow cleanup so imported person sets are easier to understand and browse

## Packaging and Distribution
- macOS releases are now distributed as notarized `.dmg` files instead of `.zip` archives
- macOS build automation now signs with hardened runtime, applies the required .NET `allow-jit` entitlement, notarizes, and staples the shipped app bundles
- Windows release packaging remains self-contained and continues to include bundled license materials

## System Requirement
- Windows 10 22H2 or later
- macOS 12.0 or later

---

# CBDB Desktop 0.3.3-beta1 版更新說明

這個版本把桌面應用程式從先前的查詢基線，進一步擴展成更完整、也更穩定的研究工具。除了新增按人群查詢模組之外，也進一步深化了官職查詢的工作流程、改善了大型清單的捲動與選擇體驗，並完成了 macOS 的簽章、公證與 DMG 封裝流程。

目前應用程式已提供五個可用模組：
- **人物瀏覽**
- **社會區分查詢**
- **入仕查詢**
- **官職查詢**
- **按人群查詢**

## 主要更新
- 新增 **按人群查詢** 模組，可先載入一組人物 ID，再展開相關的社會區分、官職、入仕、文本與地址結果
- 深化 **官職查詢** 的地點工作流程，補強人物地點與官職地點的對應語意與派生欄位
- 查詢模組中的朝代篩選由區間改為 **多選式選擇器**，可直接跳著選取不連續的朝代
- 改善人物瀏覽與地點選擇等大型清單的捲動與增量載入行為，降低卡頓感
- 程式現在會在啟動時嘗試檢查 GitHub releases 是否有新版本，而 **關於** 視窗也提供手動再次檢查的入口
- macOS 封裝現在會產出 **已簽章、已公證的 DMG 安裝映像檔**，並分別提供 Apple Silicon 與 Intel 版本

## 改進項目
- 官職查詢結果表現在提供更清楚的地點比對與地點工作流程欄位，人物層級聚合也能更完整保留多地點語意
- 社會區分、入仕與官職三個查詢視窗都補強了整體篩選行為的 headless Avalonia 測試覆蓋
- 查詢側各種選擇器的互動方式進一步收斂，搜尋、選取與大型清單操作更一致
- 朝代排序規則再調整，`高麗 / 高丽` 現在也會併入尾端的外部朝代分組
- 地點選擇器與人物瀏覽相關清單改採更多增量載入路徑，減少捲動時的 UI 停頓
- 新的按人群查詢模組也進一步整理了版面與操作流程，讓匯入的人物集合更容易理解與檢視

## 封裝與發佈
- macOS 版本現在改以已公證的 `.dmg` 發佈，不再使用 `.zip`
- macOS 自動化建置流程現在會套用 hardened runtime、加入 .NET 所需的 `allow-jit` entitlement，並完成公證與附票
- Windows 版本仍維持 self-contained 封裝，並持續附帶授權相關文件

## 系統需求
- Windows 10 22H2 或更新版本
- macOS 12.0 或更新版本
