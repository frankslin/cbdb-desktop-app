using Cbdb.App.Core;

namespace Cbdb.App.Desktop.Localization;

public sealed class AppLocalizationService : ILocalizationService {
    private readonly Dictionary<UiLanguage, Dictionary<string, string>> _resources = new() {
        [UiLanguage.English] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "NAVIGATION_PANE",
            ["header.main"] = "China Biographical Database (CBDB)",
            ["button.browse"] = "Browse",
            ["button.check_db"] = "Check DB",
            ["button.users_guide"] = "Users Guide",
            ["button.exit"] = "Exit",
            ["button.report_error"] = "Report an Error",
            ["button.change_index_address"] = "Change Index Address Ranking",
            ["button.relink_tables"] = "Relink Tables / Change Dataset",
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
            ["msg.module_todo"] = "{0} module is not implemented yet. Next step: wire this button to its page/query workflow.",
            ["msg.report_todo"] = "Error reporting flow not wired yet. We can route this to an issue form or local feedback dialog.",
            ["msg.index_addr_todo"] = "This will open Address Rank editor + Index Address rebuild page in the next implementation step.",
            ["msg.dataset_hint"] = "Use Browse to select another SQLite file, then click Check DB.",
            ["msg.user_guide_opened"] = "Users Guide opened",
            ["msg.user_guide_failed"] = "Users Guide failed",
            ["msg.user_guide_not_found"] = "Guide file not found: {0}",
            ["dialog.select_sqlite"] = "Select CBDB SQLite file"
        },
        [UiLanguage.TraditionalChinese] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "導航面板",
            ["header.main"] = "中國歷代人物傳記資料庫",
            ["button.browse"] = "選擇檔案",
            ["button.check_db"] = "檢查資料庫",
            ["button.users_guide"] = "用戶指南",
            ["button.exit"] = "退出",
            ["button.report_error"] = "問題回報",
            ["button.change_index_address"] = "修改索引地址排序",
            ["button.relink_tables"] = "切換資料集",
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
            ["msg.module_todo"] = "模組「{0}」尚未實作。下一步將接入對應查詢頁面。",
            ["msg.report_todo"] = "問題回報流程尚未接線，後續可接入線上表單或本地回饋視窗。",
            ["msg.index_addr_todo"] = "下一步會在此接入 Address Rank 編輯與 Index Address 重建功能。",
            ["msg.dataset_hint"] = "請先選擇 SQLite 檔案，再按「檢查資料庫」。",
            ["msg.user_guide_opened"] = "已開啟用戶指南",
            ["msg.user_guide_failed"] = "開啟用戶指南失敗",
            ["msg.user_guide_not_found"] = "找不到指南檔案：{0}",
            ["dialog.select_sqlite"] = "選擇 CBDB SQLite 檔案"
        },
        [UiLanguage.SimplifiedChinese] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["window.title"] = "导航面板",
            ["header.main"] = "中国历代人物传记资料库",
            ["button.browse"] = "选择文件",
            ["button.check_db"] = "检查数据库",
            ["button.users_guide"] = "用户指南",
            ["button.exit"] = "退出",
            ["button.report_error"] = "问题回报",
            ["button.change_index_address"] = "修改索引地址排序",
            ["button.relink_tables"] = "切换数据集",
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
            ["msg.module_todo"] = "模块“{0}”尚未实现。下一步将接入对应查询页面。",
            ["msg.report_todo"] = "问题回报流程尚未接线，后续可接入线上表单或本地反馈窗口。",
            ["msg.index_addr_todo"] = "下一步会在此接入 Address Rank 编辑与 Index Address 重建功能。",
            ["msg.dataset_hint"] = "请先选择 SQLite 文件，再点“检查数据库”。",
            ["msg.user_guide_opened"] = "已打开用户指南",
            ["msg.user_guide_failed"] = "打开用户指南失败",
            ["msg.user_guide_not_found"] = "找不到指南文件：{0}",
            ["dialog.select_sqlite"] = "选择 CBDB SQLite 文件"
        }
    };

    public UiLanguage CurrentLanguage { get; private set; } = UiLanguage.TraditionalChinese;

    public event EventHandler? LanguageChanged;

    public void SetLanguage(UiLanguage language) {
        if (CurrentLanguage == language) {
            return;
        }

        CurrentLanguage = language;
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
}
