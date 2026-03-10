using Avalonia;
using Avalonia.Media;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Localization;

public sealed class AppLocalizationService : ILocalizationService {
    private readonly Dictionary<UiLanguage, Dictionary<string, string>> _resources = new() {
        [UiLanguage.English] = new(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "NAVIGATION_PANE",
            ["header.main"] = "China Biographical Database (CBDB)",
            ["button.report_error"] = "Report an Error",
            ["button.change_index_address"] = "Change Index Address Ranking",
            ["button.relink_tables"] = "Relink Tables / Change Dataset",
            ["button.users_guide"] = "Users Guide",
            ["button.exit"] = "Exit",
            ["module.browser"] = "Look Up Data on an Individual",
            ["module.entry"] = "Query by Methods of Entry into Government",
            ["module.office"] = "Query Office Holding",
            ["module.kinship"] = "Query Kinship",
            ["module.associations"] = "Query Associations",
            ["module.networks"] = "Query Social Networks",
            ["module.association_pairs"] = "Query Pair-wise Associations",
            ["module.place"] = "Query Place Associations",
            ["module.status"] = "Query Status",
            ["module.texts"] = "Query Texts",
            ["status.ready"] = "Ready",
            ["status.checking"] = "Checking...",
            ["status.connected"] = "Connected",
            ["status.failed"] = "Failed",
            ["status.language_set"] = "Language switched",
            ["status.module_selected"] = "Module selected: {0}",
            ["msg.index_addr_todo"] = "This module remains to be ported after the navigation shell.",
            ["msg.user_guide_opened"] = "Users Guide opened",
            ["msg.user_guide_failed"] = "Users Guide failed",
            ["msg.user_guide_not_found"] = "Guide file not found: {0}",
            ["msg.sqlite_missing"] = "SQLite file does not exist. Please choose a valid CBDB SQLite file first.",
            ["msg.module_todo"] = "{0} has not been ported to Avalonia yet.",
            ["msg.browser_todo"] = "Person Browser shell is the next Avalonia port target.",
            ["msg.report_opened"] = "Opened error report page",
            ["dialog.select_sqlite"] = "Select CBDB SQLite file",
            ["browser.window_title"] = "Person Browser",
            ["browser.search"] = "Search",
            ["browser.clear"] = "Clear",
            ["browser.save_to_file"] = "Save to File",
            ["browser.keyword_tooltip"] = "Search by name, Chinese name, pinyin, or alt names",
            ["browser.grid_person_id"] = "ID",
            ["browser.grid_name_chn"] = "Chinese Name",
            ["browser.grid_name_rm"] = "Pinyin",
            ["browser.grid_index_year"] = "Index Year",
            ["browser.grid_index_address"] = "Index Address",
            ["browser.person_id"] = "Person ID",
            ["browser.name_chn"] = "Chinese Name",
            ["browser.name"] = "Name",
            ["browser.gender"] = "Gender",
            ["browser.birth_year"] = "Birth Year",
            ["browser.death_year"] = "Death Year",
            ["browser.dynasty"] = "Dynasty",
            ["browser.index_year"] = "Index Year",
            ["browser.index_year_type"] = "Index Year Type",
            ["browser.index_year_source"] = "Index Year Source",
            ["browser.index_address"] = "Index Address",
            ["browser.index_address_type"] = "Index Address Type",
            ["browser.tab_basic"] = "Basic Information",
            ["browser.tab_related"] = "Related",
            ["browser.all_fields_header"] = "All BIOG_MAIN Fields",
            ["browser.search_result_count"] = "Results: {0}",
            ["browser.related_counts"] = "Addresses: {0} | Alt Names: {1} | Kinship: {2} | Associations: {3}",
            ["browser.related_counts_more"] = "Offices: {0} | Entries: {1} | Events: {2} | Status: {3} | Texts: {4}",
            ["browser.related_counts_tail"] = "Possessions: {0} | Sources: {1} | Institutions: {2}",
            ["browser.related_placeholder"] = "Related data grids have not been ported to Avalonia yet.",
            ["browser.no_selection"] = "Select a person to view details.",
            ["browser.male"] = "Male",
            ["browser.female"] = "Female",
            ["browser.unknown"] = "Unknown",
            ["browser.no_data_to_export"] = "There is no data to export."
        },
        [UiLanguage.TraditionalChinese] = new(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "導航面板",
            ["header.main"] = "中國歷代人物傳記資料庫",
            ["button.report_error"] = "問題回報",
            ["button.change_index_address"] = "修改索引地址排序",
            ["button.relink_tables"] = "切換資料集",
            ["button.users_guide"] = "用戶指南",
            ["button.exit"] = "退出",
            ["module.browser"] = "按人查詢",
            ["module.entry"] = "按入仕途徑查詢",
            ["module.office"] = "官職查詢",
            ["module.kinship"] = "親屬關係查詢",
            ["module.associations"] = "社會關係查詢",
            ["module.networks"] = "社會網絡查詢",
            ["module.association_pairs"] = "兩人關係網交集查詢",
            ["module.place"] = "地區關係查詢",
            ["module.status"] = "社會區分查詢",
            ["module.texts"] = "文本關係查詢",
            ["status.ready"] = "就緒",
            ["status.checking"] = "檢查中...",
            ["status.connected"] = "連線成功",
            ["status.failed"] = "連線失敗",
            ["status.language_set"] = "語言已切換",
            ["status.module_selected"] = "已選模組：{0}",
            ["msg.index_addr_todo"] = "導航殼完成後再移植此模組。",
            ["msg.user_guide_opened"] = "已開啟用戶指南",
            ["msg.user_guide_failed"] = "開啟用戶指南失敗",
            ["msg.user_guide_not_found"] = "找不到指南檔案：{0}",
            ["msg.sqlite_missing"] = "SQLite 檔案不存在，請先選擇有效的 CBDB SQLite 檔案。",
            ["msg.module_todo"] = "模組「{0}」尚未移植到 Avalonia。",
            ["msg.browser_todo"] = "人物瀏覽視窗是下一個 Avalonia 移植目標。",
            ["msg.report_opened"] = "已開啟問題回報頁面",
            ["dialog.select_sqlite"] = "選擇 CBDB SQLite 檔案",
            ["browser.window_title"] = "人物瀏覽",
            ["browser.search"] = "查詢",
            ["browser.clear"] = "清除",
            ["browser.save_to_file"] = "存成檔案",
            ["browser.keyword_tooltip"] = "可用姓名、中文名、拼音或別名查詢",
            ["browser.grid_person_id"] = "人物 ID",
            ["browser.grid_name_chn"] = "中文姓名",
            ["browser.grid_name_rm"] = "拼音",
            ["browser.grid_index_year"] = "索引年",
            ["browser.grid_index_address"] = "索引地址",
            ["browser.person_id"] = "人物 ID",
            ["browser.name_chn"] = "中文姓名",
            ["browser.name"] = "姓名",
            ["browser.gender"] = "性別",
            ["browser.birth_year"] = "出生年",
            ["browser.death_year"] = "卒年",
            ["browser.dynasty"] = "朝代",
            ["browser.index_year"] = "索引年",
            ["browser.index_year_type"] = "索引年類型",
            ["browser.index_year_source"] = "索引年來源",
            ["browser.index_address"] = "索引地址",
            ["browser.index_address_type"] = "索引地址類型",
            ["browser.tab_basic"] = "基本資料",
            ["browser.tab_related"] = "相關資料",
            ["browser.all_fields_header"] = "BIOG_MAIN 全部欄位",
            ["browser.search_result_count"] = "結果數：{0}",
            ["browser.related_counts"] = "地址：{0} | 別名：{1} | 親屬：{2} | 社會關係：{3}",
            ["browser.related_counts_more"] = "任官：{0} | 入仕：{1} | 事件：{2} | 身份：{3} | 文本：{4}",
            ["browser.related_counts_tail"] = "財產：{0} | 來源：{1} | 機構：{2}",
            ["browser.related_placeholder"] = "相關資料表格尚未移植到 Avalonia。",
            ["browser.no_selection"] = "請先選擇人物以查看詳細資料。",
            ["browser.male"] = "男",
            ["browser.female"] = "女",
            ["browser.unknown"] = "未詳",
            ["browser.no_data_to_export"] = "目前沒有可匯出的資料。"
        },
        [UiLanguage.SimplifiedChinese] = new(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "导航面板",
            ["header.main"] = "中国历代人物传记资料库",
            ["button.report_error"] = "问题回报",
            ["button.change_index_address"] = "修改索引地址排序",
            ["button.relink_tables"] = "切换数据集",
            ["button.users_guide"] = "用户指南",
            ["button.exit"] = "退出",
            ["module.browser"] = "按人查询",
            ["module.entry"] = "按入仕途径查询",
            ["module.office"] = "官职查询",
            ["module.kinship"] = "亲属关系查询",
            ["module.associations"] = "社会关系查询",
            ["module.networks"] = "社会网络查询",
            ["module.association_pairs"] = "两人关系网交集查询",
            ["module.place"] = "地区关系查询",
            ["module.status"] = "社会区分查询",
            ["module.texts"] = "文本关系查询",
            ["status.ready"] = "就绪",
            ["status.checking"] = "检查中...",
            ["status.connected"] = "连接成功",
            ["status.failed"] = "连接失败",
            ["status.language_set"] = "语言已切换",
            ["status.module_selected"] = "已选模块：{0}",
            ["msg.index_addr_todo"] = "导航壳完成后再移植此模块。",
            ["msg.user_guide_opened"] = "已打开用户指南",
            ["msg.user_guide_failed"] = "打开用户指南失败",
            ["msg.user_guide_not_found"] = "找不到指南文件：{0}",
            ["msg.sqlite_missing"] = "SQLite 文件不存在，请先选择有效的 CBDB SQLite 文件。",
            ["msg.module_todo"] = "模块“{0}”尚未移植到 Avalonia。",
            ["msg.browser_todo"] = "人物浏览窗口是下一个 Avalonia 移植目标。",
            ["msg.report_opened"] = "已打开问题回报页面",
            ["dialog.select_sqlite"] = "选择 CBDB SQLite 文件",
            ["browser.window_title"] = "人物浏览",
            ["browser.search"] = "查询",
            ["browser.clear"] = "清除",
            ["browser.save_to_file"] = "存成文件",
            ["browser.keyword_tooltip"] = "可用姓名、中文名、拼音或别名查询",
            ["browser.grid_person_id"] = "人物 ID",
            ["browser.grid_name_chn"] = "中文姓名",
            ["browser.grid_name_rm"] = "拼音",
            ["browser.grid_index_year"] = "索引年",
            ["browser.grid_index_address"] = "索引地址",
            ["browser.person_id"] = "人物 ID",
            ["browser.name_chn"] = "中文姓名",
            ["browser.name"] = "姓名",
            ["browser.gender"] = "性别",
            ["browser.birth_year"] = "出生年",
            ["browser.death_year"] = "卒年",
            ["browser.dynasty"] = "朝代",
            ["browser.index_year"] = "索引年",
            ["browser.index_year_type"] = "索引年类型",
            ["browser.index_year_source"] = "索引年来源",
            ["browser.index_address"] = "索引地址",
            ["browser.index_address_type"] = "索引地址类型",
            ["browser.tab_basic"] = "基本资料",
            ["browser.tab_related"] = "相关资料",
            ["browser.all_fields_header"] = "BIOG_MAIN 全部字段",
            ["browser.search_result_count"] = "结果数：{0}",
            ["browser.related_counts"] = "地址：{0} | 别名：{1} | 亲属：{2} | 社会关系：{3}",
            ["browser.related_counts_more"] = "任官：{0} | 入仕：{1} | 事件：{2} | 身份：{3} | 文本：{4}",
            ["browser.related_counts_tail"] = "财产：{0} | 来源：{1} | 机构：{2}",
            ["browser.related_placeholder"] = "相关资料表格尚未移植到 Avalonia。",
            ["browser.no_selection"] = "请先选择人物以查看详细资料。",
            ["browser.male"] = "男",
            ["browser.female"] = "女",
            ["browser.unknown"] = "未详",
            ["browser.no_data_to_export"] = "目前没有可导出的数据。"
        }
    };

    public UiLanguage CurrentLanguage { get; private set; } = UiLanguage.TraditionalChinese;

    public event EventHandler? LanguageChanged;

    public void SetLanguage(UiLanguage language) {
        if (CurrentLanguage == language) {
            return;
        }

        CurrentLanguage = language;
        ApplyFontFamily(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key) {
        if (_resources.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var value)) {
            return value;
        }

        if (_resources[UiLanguage.English].TryGetValue(key, out var fallback)) {
            return fallback;
        }

        return key;
    }

    public void ApplyCurrentLanguage() {
        ApplyFontFamily(CurrentLanguage);
    }

    private static void ApplyFontFamily(UiLanguage language) {
        if (Application.Current is null) {
            return;
        }

        var familyName = language switch {
            UiLanguage.SimplifiedChinese => "PingFang SC, Microsoft YaHei UI, sans-serif",
            UiLanguage.TraditionalChinese => "PingFang TC, Microsoft JhengHei UI, sans-serif",
            _ => "SF Pro Text, Segoe UI, sans-serif"
        };

        Application.Current.Resources["AppFontFamily"] = new FontFamily(familyName);
    }
}
