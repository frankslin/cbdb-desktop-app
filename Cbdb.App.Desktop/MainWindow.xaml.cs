using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cbdb.App.Core;
using Cbdb.App.Data;
using Cbdb.App.Desktop.Localization;
using Microsoft.Win32;

namespace Cbdb.App.Desktop;

public partial class MainWindow : Window {
    private readonly IDatabaseHealthService _databaseHealthService = new SqliteDatabaseHealthService();
    private readonly ILocalizationService _localizationService = new AppLocalizationService();

    public MainWindow() {
        InitializeComponent();
        TxtSqlitePath.Text = GuessDefaultSqlitePath();

        _localizationService.LanguageChanged += (_, _) => ApplyLocalization();
        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = T("window.title");
        TxtHeaderMain.Text = T("header.main");

        BtnBrowse.Content = T("button.browse");
        BtnCheck.Content = T("button.check_db");

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

    private void BtnBrowse_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFileDialog {
            Filter = "SQLite Database (*.sqlite3;*.db;*.sqlite)|*.sqlite3;*.db;*.sqlite|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = T("dialog.select_sqlite")
        };

        if (dialog.ShowDialog(this) == true) {
            TxtSqlitePath.Text = dialog.FileName;
        }
    }

    private async void BtnCheck_Click(object sender, RoutedEventArgs e) {
        try {
            BtnCheck.IsEnabled = false;
            TxtStatus.Text = T("status.checking");
            TxtOutput.Text = string.Empty;

            var result = await _databaseHealthService.CheckAsync(TxtSqlitePath.Text);
            TxtStatus.Text = result.Success ? T("status.connected") : T("status.failed");
            TxtOutput.Text = result.Message;
        } finally {
            BtnCheck.IsEnabled = true;
        }
    }

    private void ModuleButton_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button button) {
            return;
        }

        var key = button.Tag?.ToString() ?? "module.unknown";
        var moduleLabel = T(key);

        TxtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
        TxtOutput.Text = string.Format(T("msg.module_todo"), moduleLabel);
    }

    private void BtnReportError_Click(object sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.report_error");
        TxtOutput.Text = T("msg.report_todo");
    }

    private void BtnChangeIndexAddress_Click(object sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.change_index_address");
        TxtOutput.Text = T("msg.index_addr_todo");
    }

    private void BtnRelinkTables_Click(object sender, RoutedEventArgs e) {
        TxtStatus.Text = T("button.relink_tables");
        TxtOutput.Text = T("msg.dataset_hint");
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

    private static string GuessDefaultSqlitePath() {
        var probe = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "cbdb-sqlite-db", "cbdb_20260304.sqlite3"));
        return File.Exists(probe) ? probe : string.Empty;
    }
}
