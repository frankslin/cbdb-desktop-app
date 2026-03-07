using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cbdb.App.Core;
using Cbdb.App.Data;
using Cbdb.App.Desktop.Browser;
using Cbdb.App.Desktop.Localization;
using Cbdb.App.Desktop.Modules;
using Microsoft.Win32;

namespace Cbdb.App.Desktop;

public partial class MainWindow : Window {
    private readonly IDatabaseHealthService _databaseHealthService = new SqliteDatabaseHealthService();
    private readonly ILocalizationService _localizationService = new AppLocalizationService();
    private string _sqlitePath = string.Empty;

    public MainWindow() {
        InitializeComponent();
        _sqlitePath = GuessDefaultSqlitePath();
        if (!string.IsNullOrWhiteSpace(_sqlitePath)) {
            TxtOutput.Text = _sqlitePath;
        }

        _localizationService.LanguageChanged += (_, _) => ApplyLocalization();
        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = T("window.title");
        TxtHeaderMain.Text = T("header.main");

        BtnModuleBrowser.Content = T("module.browser");
        BtnModuleEntry.Content = T("module.entry");
        BtnModuleOffice.Content = T("module.office");
        BtnModuleKinship.Content = T("module.kinship");
        BtnModuleAssociations.Content = T("module.associations");
        BtnModuleNetworks.Content = T("module.networks");
        BtnModuleAssociationPairs.Content = T("module.association_pairs");
        BtnModulePlace.Content = T("module.place");
        BtnModuleStatus.Content = T("module.status");
        BtnModuleTexts.Content = T("module.texts");

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

    private void HighlightLanguageButton() {
        BtnLangEn.FontWeight = FontWeights.Normal;
        BtnLangZhHant.FontWeight = FontWeights.Normal;
        BtnLangZhHans.FontWeight = FontWeights.Normal;

        switch (_localizationService.CurrentLanguage) {
            case UiLanguage.English:
                BtnLangEn.FontWeight = FontWeights.Bold;
                break;
            case UiLanguage.TraditionalChinese:
                BtnLangZhHant.FontWeight = FontWeights.Bold;
                break;
            case UiLanguage.SimplifiedChinese:
                BtnLangZhHans.FontWeight = FontWeights.Bold;
                break;
        }
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.English);
        TxtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHant_Click(object sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.TraditionalChinese);
        TxtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHans_Click(object sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.SimplifiedChinese);
        TxtStatus.Text = T("status.language_set");
    }

    private void ModuleButton_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button button) {
            return;
        }

        var key = button.Tag?.ToString() ?? "module.unknown";
        var moduleLabel = T(key);

        if (string.Equals(key, "module.browser", StringComparison.OrdinalIgnoreCase)) {
            var sqlitePath = NormalizeSqlitePath(_sqlitePath);
            if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
                TxtStatus.Text = T("status.failed");
                TxtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            var browserWindow = new PersonBrowserWindow(sqlitePath, _localizationService) {
                Owner = this
            };
            browserWindow.Show();

            TxtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            TxtOutput.Text = T("msg.browser_opened");
            return;
        }

        if (TryGetQueryModuleKind(key, out var moduleKind)) {
            var sqlitePath = NormalizeSqlitePath(_sqlitePath);
            if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
                TxtStatus.Text = T("status.failed");
                TxtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            try {
                var window = new QueryModuleWindow(moduleKind, _localizationService, sqlitePath) {
                    Owner = this
                };
                window.Show();

                TxtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                TxtOutput.Text = T("msg.query_shell_opened");
            } catch (Exception ex) {
                TxtStatus.Text = T("status.failed");
                TxtOutput.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "CBDB", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        TxtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
        TxtOutput.Text = string.Format(T("msg.module_todo"), moduleLabel);
    }

    private static bool TryGetQueryModuleKind(string key, out QueryModuleKind kind) {
        switch (key) {
            case "module.entry":
                kind = QueryModuleKind.Entry;
                return true;
            case "module.office":
                kind = QueryModuleKind.Office;
                return true;
            case "module.kinship":
                kind = QueryModuleKind.Kinship;
                return true;
            case "module.associations":
                kind = QueryModuleKind.Associations;
                return true;
            case "module.networks":
                kind = QueryModuleKind.Networks;
                return true;
            case "module.association_pairs":
                kind = QueryModuleKind.AssociationPairs;
                return true;
            case "module.place":
                kind = QueryModuleKind.Place;
                return true;
            case "module.status":
                kind = QueryModuleKind.Status;
                return true;
            case "module.texts":
                kind = QueryModuleKind.Texts;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private void BtnReportError_Click(object sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.report_error");
        TxtOutput.Text = T("msg.report_todo");
    }

    private void BtnChangeIndexAddress_Click(object sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.change_index_address");
        TxtOutput.Text = T("msg.index_addr_todo");
    }

    private async void BtnRelinkTables_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFileDialog {
            Filter = "SQLite Database (*.sqlite3;*.db;*.sqlite)|*.sqlite3;*.db;*.sqlite|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = T("dialog.select_sqlite")
        };

        if (!string.IsNullOrWhiteSpace(_sqlitePath)) {
            try {
                dialog.InitialDirectory = Path.GetDirectoryName(_sqlitePath);
                dialog.FileName = Path.GetFileName(_sqlitePath);
            } catch {
                // Ignore invalid path; dialog will use default location.
            }
        }

        if (dialog.ShowDialog(this) != true) {
            return;
        }

        _sqlitePath = NormalizeSqlitePath(dialog.FileName);

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

    private void BtnUsersGuide_Click(object sender, RoutedEventArgs e) {
        try {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "cbdb-user-guide", "docs", "index.md"));
            if (!File.Exists(path)) {
                TxtStatus.Text = T("button.users_guide");
                TxtOutput.Text = string.Format(T("msg.user_guide_not_found"), path);
                return;
            }

            Process.Start(new ProcessStartInfo {
                FileName = path,
                UseShellExecute = true
            });
            TxtStatus.Text = T("msg.user_guide_opened");
        } catch (Exception ex) {
            TxtStatus.Text = T("msg.user_guide_failed");
            TxtOutput.Text = ex.Message;
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e) {
        Close();
    }

    private string T(string key) => _localizationService.Get(key);

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

            var firstAny = Directory.EnumerateFiles(dataDir)
                .Where(path =>
                    path.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return firstAny;
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

        var legacyProbe = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "cbdb-sqlite-db", "cbdb_20260304.sqlite3"));
        return File.Exists(legacyProbe) ? legacyProbe : string.Empty;
    }
}
