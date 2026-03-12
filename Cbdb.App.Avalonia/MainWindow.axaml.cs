using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia;

public partial class MainWindow : Window {
    private const string UserGuideUrlEn = "https://cbdb-project.github.io/cbdb-user-guide";
    private const string UserGuideUrlZhTw = "https://cbdb-project.github.io/cbdb-user-guide/zh-TW/";

    private readonly IDatabaseHealthService _databaseHealthService = new SqliteDatabaseHealthService();
    private readonly AppLocalizationService _localizationService = new();

    private Button _btnLangEn = null!;
    private Button _btnLangZhHant = null!;
    private Button _btnLangZhHans = null!;
    private Button _btnModuleBrowser = null!;
    private Button _btnModuleEntry = null!;
    private Button _btnModuleOffice = null!;
    private Button _btnModuleKinship = null!;
    private Button _btnModuleAssociations = null!;
    private Button _btnModuleNetworks = null!;
    private Button _btnModuleAssociationPairs = null!;
    private Button _btnModulePlace = null!;
    private Button _btnModuleStatus = null!;
    private Button _btnModuleTexts = null!;
    private Button _btnReportError = null!;
    private Button _btnRelinkTables = null!;
    private Button _btnChangeIndexAddress = null!;
    private Button _btnUsersGuide = null!;
    private Button _btnExit = null!;
    private TextBlock _txtHeaderMain = null!;
    private TextBlock _txtStatus = null!;
    private TextBox _txtOutput = null!;

    private string _sqlitePath = string.Empty;

    public MainWindow() {
        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += (_, _) => ApplyLocalization();
        _localizationService.ApplyCurrentLanguage();

        ApplyLocalization();

        Opened += async (_, _) => await InitializeSqlitePathAsync();
    }

    private void ApplyLocalization() {
        Title = T("window.title");
        _txtHeaderMain.Text = T("header.main");

        SetModuleButtonContent(_btnModuleBrowser, "module.browser", "◎");
        SetModuleButtonContent(_btnModuleEntry, "module.entry", "◇");
        SetModuleButtonContent(_btnModuleOffice, "module.office", "▣");
        SetModuleButtonContent(_btnModuleKinship, "module.kinship", "◌");
        SetModuleButtonContent(_btnModuleAssociations, "module.associations", "△");
        SetModuleButtonContent(_btnModuleNetworks, "module.networks", "⌘");
        SetModuleButtonContent(_btnModuleAssociationPairs, "module.association_pairs", "◫");
        SetModuleButtonContent(_btnModulePlace, "module.place", "◈");
        SetModuleButtonContent(_btnModuleStatus, "module.status", "≡");
        SetModuleButtonContent(_btnModuleTexts, "module.texts", "✦");

        _btnReportError.Content = T("button.report_error");
        _btnChangeIndexAddress.Content = T("button.change_index_address");
        _btnRelinkTables.Content = T("button.relink_tables");
        _btnUsersGuide.Content = T("button.users_guide");
        _btnExit.Content = T("button.exit");

        if (string.IsNullOrWhiteSpace(_txtStatus.Text) || _txtStatus.Text == "Ready" || _txtStatus.Text == "就緒" || _txtStatus.Text == "就绪") {
            _txtStatus.Text = T("status.ready");
        }

        HighlightLanguageButton();
    }

    private void SetModuleButtonContent(Button button, string key, string icon) {
        button.Content = $"{icon}  {T(key)}";
    }

    private void HighlightLanguageButton() {
        _btnLangEn.FontWeight = FontWeight.Normal;
        _btnLangZhHant.FontWeight = FontWeight.Normal;
        _btnLangZhHans.FontWeight = FontWeight.Normal;

        switch (_localizationService.CurrentLanguage) {
            case UiLanguage.English:
                _btnLangEn.FontWeight = FontWeight.Bold;
                break;
            case UiLanguage.TraditionalChinese:
                _btnLangZhHant.FontWeight = FontWeight.Bold;
                break;
            case UiLanguage.SimplifiedChinese:
                _btnLangZhHans.FontWeight = FontWeight.Bold;
                break;
        }
    }

    private void BtnLangEn_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.English);
        _txtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHant_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.TraditionalChinese);
        _txtStatus.Text = T("status.language_set");
    }

    private void BtnLangZhHans_Click(object? sender, RoutedEventArgs e) {
        _localizationService.SetLanguage(UiLanguage.SimplifiedChinese);
        _txtStatus.Text = T("status.language_set");
    }

    private void ModuleButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is not Button button) {
            return;
        }

        var key = Convert.ToString(button.Tag) ?? "module.unknown";
        var moduleLabel = T(key);

        if (key == "module.browser") {
            if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            var window = new PersonBrowserWindow(_sqlitePath, _localizationService);
            window.Show();
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.browser_opened");
            return;
        }

        _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
        _txtOutput.Text = key == "module.browser" ? T("msg.browser_todo") : string.Format(T("msg.module_todo"), moduleLabel);
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
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = T("msg.sqlite_missing");
            return;
        }

        _sqlitePath = NormalizeSqlitePath(path);

        try {
            _txtStatus.Text = T("status.checking");
            _txtOutput.Text = string.Empty;

            var result = await _databaseHealthService.CheckAsync(_sqlitePath);
            _txtStatus.Text = result.Success ? T("status.connected") : T("status.failed");
            _txtOutput.Text = result.Success
                ? $"{result.Message}{Environment.NewLine}{_sqlitePath}"
                : result.Message;

            if (result.Success) {
                await AppSettingsStore.SaveLastSqlitePathAsync(_sqlitePath);
            }
        } catch (Exception ex) {
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = ex.Message;
        }
    }

    private void BtnReportError_Click(object? sender, RoutedEventArgs e) {
        const string reportUrl = "https://cbdb.hsites.harvard.edu/report-error";

        try {
            OpenExternalTarget(reportUrl);
            _txtStatus.Text = T("button.report_error");
            _txtOutput.Text = $"{T("msg.report_opened")}{Environment.NewLine}{reportUrl}";
        } catch (Exception ex) {
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = ex.Message;
        }
    }

    private void BtnChangeIndexAddress_Click(object? sender, RoutedEventArgs e) {
        _txtStatus.Text = T("button.change_index_address");
        _txtOutput.Text = T("msg.index_addr_todo");
    }

    private void BtnUsersGuide_Click(object? sender, RoutedEventArgs e) {
        try {
            var userGuideUrl = GetUserGuideUrl();
            OpenExternalTarget(userGuideUrl);
            _txtStatus.Text = T("msg.user_guide_opened");
            _txtOutput.Text = userGuideUrl;
        } catch (Exception ex) {
            _txtStatus.Text = T("msg.user_guide_failed");
            _txtOutput.Text = ex.Message;
        }
    }

    private void BtnExit_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private string T(string key) => _localizationService.Get(key);

    private string GetUserGuideUrl() {
        return _localizationService.CurrentLanguage == UiLanguage.English ? UserGuideUrlEn : UserGuideUrlZhTw;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _btnLangEn = this.FindControl<Button>("BtnLangEn") ?? throw new InvalidOperationException("BtnLangEn not found.");
        _btnLangZhHant = this.FindControl<Button>("BtnLangZhHant") ?? throw new InvalidOperationException("BtnLangZhHant not found.");
        _btnLangZhHans = this.FindControl<Button>("BtnLangZhHans") ?? throw new InvalidOperationException("BtnLangZhHans not found.");
        _btnModuleBrowser = this.FindControl<Button>("BtnModuleBrowser") ?? throw new InvalidOperationException("BtnModuleBrowser not found.");
        _btnModuleEntry = this.FindControl<Button>("BtnModuleEntry") ?? throw new InvalidOperationException("BtnModuleEntry not found.");
        _btnModuleOffice = this.FindControl<Button>("BtnModuleOffice") ?? throw new InvalidOperationException("BtnModuleOffice not found.");
        _btnModuleKinship = this.FindControl<Button>("BtnModuleKinship") ?? throw new InvalidOperationException("BtnModuleKinship not found.");
        _btnModuleAssociations = this.FindControl<Button>("BtnModuleAssociations") ?? throw new InvalidOperationException("BtnModuleAssociations not found.");
        _btnModuleNetworks = this.FindControl<Button>("BtnModuleNetworks") ?? throw new InvalidOperationException("BtnModuleNetworks not found.");
        _btnModuleAssociationPairs = this.FindControl<Button>("BtnModuleAssociationPairs") ?? throw new InvalidOperationException("BtnModuleAssociationPairs not found.");
        _btnModulePlace = this.FindControl<Button>("BtnModulePlace") ?? throw new InvalidOperationException("BtnModulePlace not found.");
        _btnModuleStatus = this.FindControl<Button>("BtnModuleStatus") ?? throw new InvalidOperationException("BtnModuleStatus not found.");
        _btnModuleTexts = this.FindControl<Button>("BtnModuleTexts") ?? throw new InvalidOperationException("BtnModuleTexts not found.");
        _btnReportError = this.FindControl<Button>("BtnReportError") ?? throw new InvalidOperationException("BtnReportError not found.");
        _btnRelinkTables = this.FindControl<Button>("BtnRelinkTables") ?? throw new InvalidOperationException("BtnRelinkTables not found.");
        _btnChangeIndexAddress = this.FindControl<Button>("BtnChangeIndexAddress") ?? throw new InvalidOperationException("BtnChangeIndexAddress not found.");
        _btnUsersGuide = this.FindControl<Button>("BtnUsersGuide") ?? throw new InvalidOperationException("BtnUsersGuide not found.");
        _btnExit = this.FindControl<Button>("BtnExit") ?? throw new InvalidOperationException("BtnExit not found.");
        _txtHeaderMain = this.FindControl<TextBlock>("TxtHeaderMain") ?? throw new InvalidOperationException("TxtHeaderMain not found.");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus") ?? throw new InvalidOperationException("TxtStatus not found.");
        _txtOutput = this.FindControl<TextBox>("TxtOutput") ?? throw new InvalidOperationException("TxtOutput not found.");
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

    private async Task InitializeSqlitePathAsync() {
        var restoredPath = NormalizeSqlitePath(await AppSettingsStore.TryGetLastSqlitePathAsync() ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(restoredPath) && await TryUseSqlitePathAsync(restoredPath, persistOnSuccess: false)) {
            return;
        }

        var guessedPath = GuessDefaultSqlitePath();
        if (!string.IsNullOrWhiteSpace(guessedPath)) {
            await TryUseSqlitePathAsync(guessedPath, persistOnSuccess: true);
        }
    }

    private async Task<bool> TryUseSqlitePathAsync(string sqlitePath, bool persistOnSuccess) {
        var normalizedPath = NormalizeSqlitePath(sqlitePath);
        if (string.IsNullOrWhiteSpace(normalizedPath) || !File.Exists(normalizedPath)) {
            return false;
        }

        try {
            _txtStatus.Text = T("status.checking");
            var result = await _databaseHealthService.CheckAsync(normalizedPath);
            if (!result.Success) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = result.Message;
                return false;
            }

            _sqlitePath = normalizedPath;
            _txtStatus.Text = T("status.connected");
            _txtOutput.Text = $"{result.Message}{Environment.NewLine}{_sqlitePath}";

            if (persistOnSuccess) {
                await AppSettingsStore.SaveLastSqlitePathAsync(_sqlitePath);
            }

            return true;
        } catch (Exception ex) {
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = ex.Message;
            return false;
        }
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
