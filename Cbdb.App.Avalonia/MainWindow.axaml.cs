using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia;

public partial class MainWindow : Window {
    private readonly IDatabaseHealthService _databaseHealthService = new SqliteDatabaseHealthService();
    private readonly AppLocalizationService _localizationService = new();

    private string _sqlitePath = string.Empty;

    public MainWindow() {
        InitializeComponent();

        _localizationService.LanguageChanged += (_, _) => ApplyLocalization();
        _localizationService.ApplyCurrentLanguage();

        _sqlitePath = GuessDefaultSqlitePath();
        if (!string.IsNullOrWhiteSpace(_sqlitePath)) {
            TxtOutput.Text = _sqlitePath;
        }

        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = T("window.title");
        TxtHeaderMain.Text = T("header.main");

        SetModuleButtonContent(BtnModuleBrowser, "module.browser", "◎");
        SetModuleButtonContent(BtnModuleEntry, "module.entry", "◇");
        SetModuleButtonContent(BtnModuleOffice, "module.office", "▣");
        SetModuleButtonContent(BtnModuleKinship, "module.kinship", "◌");
        SetModuleButtonContent(BtnModuleAssociations, "module.associations", "△");
        SetModuleButtonContent(BtnModuleNetworks, "module.networks", "⌘");
        SetModuleButtonContent(BtnModuleAssociationPairs, "module.association_pairs", "◫");
        SetModuleButtonContent(BtnModulePlace, "module.place", "◈");
        SetModuleButtonContent(BtnModuleStatus, "module.status", "≡");
        SetModuleButtonContent(BtnModuleTexts, "module.texts", "✦");

        BtnReportError.Content = T("button.report_error");
        BtnChangeIndexAddress.Content = T("button.change_index_address");
        BtnRelinkTables.Content = T("button.relink_tables");
        BtnUsersGuide.Content = T("button.users_guide");
        BtnExit.Content = T("button.exit");

        if (string.IsNullOrWhiteSpace(TxtStatus.Text) || TxtStatus.Text == "Ready" || TxtStatus.Text == "就緒" || TxtStatus.Text == "就绪") {
            TxtStatus.Text = T("status.ready");
        }

        HighlightLanguageButton();
    }

    private void SetModuleButtonContent(Button button, string key, string icon) {
        button.Content = $"{icon}  {T(key)}";
    }

    private void HighlightLanguageButton() {
        BtnLangEn.FontWeight = FontWeight.Normal;
        BtnLangZhHant.FontWeight = FontWeight.Normal;
        BtnLangZhHans.FontWeight = FontWeight.Normal;

        switch (_localizationService.CurrentLanguage) {
            case UiLanguage.English:
                BtnLangEn.FontWeight = FontWeight.Bold;
                break;
            case UiLanguage.TraditionalChinese:
                BtnLangZhHant.FontWeight = FontWeight.Bold;
                break;
            case UiLanguage.SimplifiedChinese:
                BtnLangZhHans.FontWeight = FontWeight.Bold;
                break;
        }
    }

    private void BtnLangEn_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.English);
        TxtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHant_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.TraditionalChinese);
        TxtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHans_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.SimplifiedChinese);
        TxtStatus.Text = T("status.language_set");
    }

    private void ModuleButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is not Button button) {
            return;
        }

        var key = Convert.ToString(button.Tag) ?? "module.unknown";
        var moduleLabel = T(key);

        TxtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
        TxtOutput.Text = key == "module.browser" ? T("msg.browser_todo") : string.Format(T("msg.module_todo"), moduleLabel);
    }

    private async void BtnRelinkTables_Click(object? sender, RoutedEventArgs e) {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = T("dialog.select_sqlite"),
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> {
                new("SQLite Database") {
                    Patterns = new[] { "*.sqlite3", "*.sqlite", "*.db" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0) {
            return;
        }

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            TxtStatus.Text = T("status.failed");
            TxtOutput.Text = T("msg.sqlite_missing");
            return;
        }

        _sqlitePath = NormalizeSqlitePath(path);

        try {
            TxtStatus.Text = T("status.checking");
            TxtOutput.Text = string.Empty;

            var result = await _databaseHealthService.CheckAsync(_sqlitePath);
            TxtStatus.Text = result.Success ? T("status.connected") : T("status.failed");
            TxtOutput.Text = result.Success
                ? $"{result.Message}{Environment.NewLine}{_sqlitePath}"
                : result.Message;
        } catch (Exception ex) {
            TxtStatus.Text = T("status.failed");
            TxtOutput.Text = ex.Message;
        }
    }

    private void BtnReportError_Click(object? sender, RoutedEventArgs e) {
        const string reportUrl = "https://cbdb.hsites.harvard.edu/report-error";

        try {
            OpenExternalTarget(reportUrl);
            TxtStatus.Text = T("button.report_error");
            TxtOutput.Text = $"{T("msg.report_opened")}{Environment.NewLine}{reportUrl}";
        } catch (Exception ex) {
            TxtStatus.Text = T("status.failed");
            TxtOutput.Text = ex.Message;
        }
    }

    private void BtnChangeIndexAddress_Click(object? sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.change_index_address");
        TxtOutput.Text = T("msg.index_addr_todo");
    }

    private void BtnUsersGuide_Click(object? sender, RoutedEventArgs e) {
        try {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "cbdb-user-guide", "docs", "index.md"));
            if (!File.Exists(path)) {
                TxtStatus.Text = T("button.users_guide");
                TxtOutput.Text = string.Format(T("msg.user_guide_not_found"), path);
                return;
            }

            OpenExternalTarget(path);
            TxtStatus.Text = T("msg.user_guide_opened");
            TxtOutput.Text = path;
        } catch (Exception ex) {
            TxtStatus.Text = T("msg.user_guide_failed");
            TxtOutput.Text = ex.Message;
        }
    }

    private void BtnExit_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private string T(string key) => _localizationService.Get(key);

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private static void OpenExternalTarget(string target) {
        Process.Start(new ProcessStartInfo {
            FileName = target,
            UseShellExecute = true
        });
    }

    private static string NormalizeSqlitePath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        return path.Trim().Trim((char)34);
    }

    private static string GuessDefaultSqlitePath() {
        static string? FindInDataFolder(string root) {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
                return null;
            }

            var dataDir = Path.Combine(root, "data");
            if (!Directory.Exists(dataDir)) {
                return null;
            }

            var preferred = Path.Combine(dataDir, "cbdb_20260304.sqlite3");
            if (File.Exists(preferred)) {
                return preferred;
            }

            return Directory.EnumerateFiles(dataDir)
                .Where(path =>
                    path.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        var roots = new List<string> {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
            Directory.GetCurrentDirectory()
        };

        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase)) {
            var found = FindInDataFolder(root);
            if (!string.IsNullOrWhiteSpace(found)) {
                return found;
            }
        }

        return string.Empty;
    }
}
