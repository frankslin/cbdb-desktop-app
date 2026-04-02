using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Primitives;
using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Core;
using Cbdb.App.Data;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Cbdb.App.Avalonia;

public partial class MainWindow : Window {
    private const string UserGuideUrlEn = "https://cbdb-project.github.io/cbdb-user-guide";
    private const string UserGuideUrlZhTw = "https://cbdb-project.github.io/cbdb-user-guide/zh-TW/";
    private const string LatestDataUrl = "https://huggingface.co/datasets/cbdb/cbdb-sqlite/blob/main/latest.zip";
    private static readonly IReadOnlyDictionary<string, string> ModuleIconPaths = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["module.browser"] = "M224 248a120 120 0 1 0 0-240 120 120 0 1 0 0 240m-29.7 56C95.8 304 16 383.8 16 482.3c0 16.4 13.3 29.7 29.7 29.7h356.6c16.4 0 29.7-13.3 29.7-29.7 0-98.5-79.8-178.3-178.3-178.3z",
        ["module.entry"] = "m48 195.8 209.2 86.1c9.8 4 20.2 6.1 30.8 6.1s21-2.1 30.8-6.1l242.4-99.8c9-3.7 14.8-12.4 14.8-22.1s-5.8-18.4-14.8-22.1L318.8 38.1c-9.8-4-20.2-6.1-30.8-6.1s-21 2.1-30.8 6.1L14.8 137.9C5.8 141.6 0 150.3 0 160v296c0 13.3 10.7 24 24 24s24-10.7 24-24zm48 71.7V384c0 53 86 96 192 96s192-43 192-96V267.4l-142.9 58.9c-15.6 6.4-32.2 9.7-49.1 9.7s-33.5-3.3-49.1-9.7L96 267.4z",
        ["module.office"] = "M200 48h112c4.4 0 8 3.6 8 8v40H192V56c0-4.4 3.6-8 8-8m-56 8v40H64c-35.3 0-64 28.7-64 64v96h512v-96c0-35.3-28.7-64-64-64h-80V56c0-30.9-25.1-56-56-56H200c-30.9 0-56 25.1-56 56m368 248H320v16c0 17.7-14.3 32-32 32h-64c-17.7 0-32-14.3-32-32v-16H0v112c0 35.3 28.7 64 64 64h384c35.3 0 64-28.7 64-64z",
        ["module.kinship"] = "M64 128a112 112 0 1 1 224 0 112 112 0 1 1-224 0M0 464c0-97.2 78.8-176 176-176s176 78.8 176 176v6c0 23.2-18.8 42-42 42H42c-23.2 0-42-18.8-42-42zM432 64a96 96 0 1 1 0 192 96 96 0 1 1 0-192m0 240c79.5 0 144 64.5 144 144v22.4c0 23-18.6 41.6-41.6 41.6H389.6c6.6-12.5 10.4-26.8 10.4-42v-6c0-51.5-17.4-98.9-46.5-136.7 22.6-14.7 49.6-23.3 78.5-23.3",
        ["module.associations"] = "M32 64a64 64 0 1 1 128 0 64 64 0 1 1-128 0M0 224c0-35.3 28.7-64 64-64h64c3.2 0 6.4.2 9.5.7l-44.4 44.4c-28.1 28.1-28.1 73.7 0 101.8l56 56c3.4 3.4 7 6.4 10.9 9V464c0 26.5-21.5 48-48 48H80c-26.5 0-48-21.5-48-48V343.4c-19.1-11-32-31.7-32-55.4zM352 64a64 64 0 1 1 128 0 64 64 0 1 1-128 0m66.9 141.1-44.4-44.4c3.1-.5 6.3-.7 9.5-.7h64c35.3 0 64 28.7 64 64v64c0 23.7-12.9 44.4-32 55.4V464c0 26.5-21.5 48-48 48h-32c-26.5 0-48-21.5-48-48v-92.1c3.9-2.6 7.5-5.6 10.9-9l56-56c28.1-28.1 28.1-73.7 0-101.8m-116.1-27.3c9-3.7 19.3-1.7 26.2 5.2l56 56c9.4 9.4 9.4 24.6 0 33.9l-56 56c-6.9 6.9-17.2 8.9-26.2 5.2S288 321.7 288 312v-24h-64v24c0 9.7-5.8 18.5-14.8 22.2s-19.3 1.7-26.2-5.2l-56-56c-9.4-9.4-9.4-24.6 0-33.9l56-56c6.9-6.9 17.2-8.9 26.2-5.2S224 190.3 224 200v24h64v-24c0-9.7 5.8-18.5 14.8-22.2",
        ["module.networks"] = "M384 192c53 0 96-43 96-96S437 0 384 0s-96 43-96 96c0 5.4.5 10.8 1.3 16l-129.7 72.1c-16.9-15-39.2-24.1-63.6-24.1-53 0-96 43-96 96s43 96 96 96c24.4 0 46.6-9.1 63.6-24.1L289.3 400c-.9 5.2-1.3 10.5-1.3 16 0 53 43 96 96 96s96-43 96-96-43-96-96-96c-24.4 0-46.6 9.1-63.6 24.1L190.7 272c.9-5.2 1.3-10.5 1.3-16s-.5-10.8-1.3-16l129.7-72.1c16.9 15 39.2 24.1 63.6 24.1",
        ["module.association_pairs"] = "M0 80c0-26.5 21.5-48 48-48h96c26.5 0 48 21.5 48 48v16h128V80c0-26.5 21.5-48 48-48h96c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48h-96c-26.5 0-48-21.5-48-48v-16H192v16c0 7.3-1.7 14.3-4.6 20.5L256 288h80c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48h-96c-26.5 0-48-21.5-48-48v-96c0-7.3 1.7-14.3 4.6-20.5L128 224H48c-26.5 0-48-21.5-48-48z",
        ["module.place"] = "M576 48c0-11.1-5.7-21.4-15.2-27.2s-21.2-6.4-31.1-1.4L413.5 77.5 234.1 17.6c-8.1-2.7-16.8-2.1-24.4 1.7l-128 64C70.8 88.8 64 99.9 64 112v352c0 11.1 5.7 21.4 15.2 27.2s21.2 6.4 31.1 1.4l116.1-58.1 173.3 57.8c-4.3-6.4-8.5-13.1-12.6-19.9-11-18.3-21.9-39.3-30-61.8l-101.2-33.7V92.4l128 42.7v99.3c31-35.8 77-58.4 128-58.4 22.6 0 44.2 4.4 64 12.5zm-64 176c-66.3 0-120 52.8-120 117.9 0 68.9 64.1 150.4 98.6 189.3 11.6 13 31.3 13 42.9 0 34.5-38.9 98.6-120.4 98.6-189.3 0-65.1-53.7-117.9-120-117.9zm-40 120a40 40 0 1 1 80 0 40 40 0 1 1-80 0",
        ["module.status"] = "M288 160h96V96h-96zM0 160V80c0-26.5 21.5-48 48-48h352c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48H48c-26.5 0-48-21.5-48-48zm160 256h224v-64H160zM0 416v-80c0-26.5 21.5-48 48-48h352c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48H48c-26.5 0-48-21.5-48-48z",
        ["module.status"] = "M288 160h96V96h-96zM0 160V80c0-26.5 21.5-48 48-48h352c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48H48c-26.5 0-48-21.5-48-48zm160 256h224v-64H160zM0 416v-80c0-26.5 21.5-48 48-48h352c26.5 0 48 21.5 48 48v96c0 26.5-21.5 48-48 48H48c-26.5 0-48-21.5-48-48z",
        ["module.texts"] = "M256 141.3v309.3l.5-.2A449 449 0 0 1 428.8 416H448V96h-19.2c-42.2 0-84.1 8.4-123.1 24.6-16.8 7-33.4 13.9-49.7 20.7m-25.1-79.8L256 72l25.1-10.5C327.9 42 378.1 32 428.8 32H464c26.5 0 48 21.5 48 48v352c0 26.5-21.5 48-48 48h-35.2c-50.7 0-100.9 10-147.7 29.5l-12.8 5.3c-7.9 3.3-16.7 3.3-24.6 0l-12.8-5.3C184.1 490 133.9 480 83.2 480H48c-26.5 0-48-21.5-48-48V80c0-26.5 21.5-48 48-48h35.2c50.7 0 100.9 10 147.7 29.5",
        ["module.group_people"] = "M96 128a112 112 0 1 1 224 0A112 112 0 1 1 96 128zM0 480c0-97.2 78.8-176 176-176h64c97.2 0 176 78.8 176 176c0 17.7-14.3 32-32 32H32c-17.7 0-32-14.3-32-32zM480 224a96 96 0 1 0 0-192 96 96 0 1 0 0 192zm-67.2 56.5c29.1-15.7 62.4-24.5 97.7-24.5C625.1 256 718 349 718 463.5c0 26.8-21.7 48.5-48.5 48.5H497.4c-9.6 0-17.4-7.8-17.4-17.4 0-76.7-26.1-147.3-69.9-203.1-2.1-2.7-.8-6.8 2.7-8.4z",
        ["module.query_example"] = "M384 336H192c-35.3 0-64-28.7-64-64V64c0-35.3 28.7-64 64-64H332.1c17 0 33.3 6.7 45.3 18.7l57.9 57.9c12 12 18.7 28.3 18.7 45.3V272c0 35.3-28.7 64-64 64zM256 48v80h80L256 48zM64 176H48C21.5 176 0 197.5 0 224V448c0 35.3 28.7 64 64 64H288c26.5 0 48-21.5 48-48V448H272v16H64V224h64V176z"
    };

    private readonly IDatabaseHealthService _databaseHealthService = new SqliteDatabaseHealthService();
    private readonly IDatabaseIndexService _databaseIndexService = new SqliteDatabaseIndexService();
    private readonly AppLocalizationService _localizationService = new();

    private ToggleButton _btnLangEn = null!;
    private ToggleButton _btnLangZhHant = null!;
    private ToggleButton _btnLangZhHans = null!;
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
    private Button _btnModuleGroupPeople = null!;
    private Button _btnModuleQueryExample = null!;
    private Button _btnReportError = null!;
    private Button _btnRelinkTables = null!;
    private Button _btnDownloadLatestData = null!;
    private Button _btnChangeIndexAddress = null!;
    private Button _btnUsersGuide = null!;
    private Button _btnAbout = null!;
    private Button _btnExit = null!;
    private TextBlock _txtHeaderMain = null!;
    private TextBlock _txtStatus = null!;
    private TextBox _txtOutput = null!;

    private string _sqlitePath = string.Empty;
    private PersonBrowserWindow? _personBrowserWindow;
    private EntryQueryWindow? _entryQueryWindow;
    private OfficeQueryWindow? _officeQueryWindow;
    private StatusQueryWindow? _statusQueryWindow;
    private GroupPeopleWindow? _groupPeopleWindow;

    internal AppLocalizationService LocalizationService => _localizationService;

    public MainWindow() {
        InitializeComponent();
        InitializeControls();

        var savedLanguage = AppSettingsStore.TryGetLastLanguage();
        if (savedLanguage.HasValue) {
            _localizationService.SetLanguage(savedLanguage.Value);
        }

        _localizationService.LanguageChanged += (_, _) => ApplyLocalization();
        _localizationService.ApplyCurrentLanguage();

        ApplyLocalization();

        Opened += async (_, _) => await InitializeSqlitePathAsync();
    }

    private void ApplyLocalization() {
        Title = T("window.title");
        _txtHeaderMain.Text = T("header.main");

        SetModuleButtonContent(_btnModuleBrowser, "module.browser");
        SetModuleButtonContent(_btnModuleEntry, "module.entry");
        SetModuleButtonContent(_btnModuleOffice, "module.office");
        SetModuleButtonContent(_btnModuleKinship, "module.kinship");
        SetModuleButtonContent(_btnModuleAssociations, "module.associations");
        SetModuleButtonContent(_btnModuleNetworks, "module.networks");
        SetModuleButtonContent(_btnModuleAssociationPairs, "module.association_pairs");
        SetModuleButtonContent(_btnModulePlace, "module.place");
        SetModuleButtonContent(_btnModuleStatus, "module.status");
        SetModuleButtonContent(_btnModuleTexts, "module.texts");
        SetModuleButtonContent(_btnModuleGroupPeople, "module.group_people");
        SetModuleButtonContent(_btnModuleQueryExample, "module.query_example");

        _btnReportError.Content = T("button.report_error");
        _btnChangeIndexAddress.Content = T("button.change_index_address");
        _btnRelinkTables.Content = T("button.relink_tables");
        _btnDownloadLatestData.Content = T("button.download_latest_data");
        _btnUsersGuide.Content = T("button.users_guide");
        _btnAbout.Content = T("button.about");
        _btnExit.Content = T("button.exit");

        if (string.IsNullOrWhiteSpace(_txtStatus.Text) || _txtStatus.Text == "Ready" || _txtStatus.Text == "就緒" || _txtStatus.Text == "就绪") {
            _txtStatus.Text = T("status.ready");
        }

        HighlightLanguageButton();
    }

    private void SetModuleButtonContent(Button button, string key) {
        var pathData = ModuleIconPaths.TryGetValue(key, out var value)
            ? value
            : ModuleIconPaths["module.browser"];

        var icon = new ShapePath {
            Data = Geometry.Parse(pathData),
            Fill = new SolidColorBrush(Color.Parse("#133FB5")),
            Stretch = Stretch.Uniform,
            Width = 24,
            Height = 24,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
        };

        var label = new TextBlock {
            Text = T(key),
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var content = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(label, 1);
        content.Children.Add(icon);
        content.Children.Add(label);
        button.Content = content;
    }

    private void HighlightLanguageButton() {
        _btnLangEn.IsChecked = false;
        _btnLangZhHant.IsChecked = false;
        _btnLangZhHans.IsChecked = false;

        switch (_localizationService.CurrentLanguage) {
            case UiLanguage.English:
                _btnLangEn.IsChecked = true;
                break;
            case UiLanguage.TraditionalChinese:
                _btnLangZhHant.IsChecked = true;
                break;
            case UiLanguage.SimplifiedChinese:
                _btnLangZhHans.IsChecked = true;
                break;
        }
    }

    private void BtnLangEn_Click(object? sender, RoutedEventArgs e) {
        SetLanguage(UiLanguage.English);
    }

    private void BtnLangZhHant_Click(object? sender, RoutedEventArgs e) {
        SetLanguage(UiLanguage.TraditionalChinese);
    }

    private void BtnLangZhHans_Click(object? sender, RoutedEventArgs e) {
        SetLanguage(UiLanguage.SimplifiedChinese);
    }

    private void SetLanguage(UiLanguage language) {
        _localizationService.SetLanguage(language);
        _txtStatus.Text = T("status.language_set");
        _ = AppSettingsStore.SaveLastLanguageAsync(language);
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

            if (_personBrowserWindow is { } existingWindow) {
                if (existingWindow.WindowState == WindowState.Minimized) {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                _txtOutput.Text = T("msg.browser_opened");
                return;
            }

            var window = new PersonBrowserWindow(_sqlitePath, _localizationService);
            _personBrowserWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_personBrowserWindow, window)) {
                    _personBrowserWindow = null;
                }
            };
            window.Show();
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.browser_opened");
            return;
        }

        if (key == "module.entry") {
            if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            if (_entryQueryWindow is { } existingWindow) {
                if (existingWindow.WindowState == WindowState.Minimized) {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                _txtOutput.Text = T("msg.entry_query_opened");
                return;
            }

            var window = new EntryQueryWindow(_sqlitePath, _localizationService);
            _entryQueryWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_entryQueryWindow, window)) {
                    _entryQueryWindow = null;
                }
            };
            window.Show(this);
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.entry_query_opened");
            return;
        }

        if (key == "module.office") {
            if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            if (_officeQueryWindow is { } existingWindow) {
                if (existingWindow.WindowState == WindowState.Minimized) {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                _txtOutput.Text = T("msg.office_query_opened");
                return;
            }

            var window = new OfficeQueryWindow(_sqlitePath, _localizationService);
            _officeQueryWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_officeQueryWindow, window)) {
                    _officeQueryWindow = null;
                }
            };
            window.Show(this);
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.office_query_opened");
            return;
        }

        if (key == "module.status") {
            if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            if (_statusQueryWindow is { } existingWindow) {
                if (existingWindow.WindowState == WindowState.Minimized) {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                _txtOutput.Text = T("msg.status_query_opened");
                return;
            }

            var window = new StatusQueryWindow(_sqlitePath, _localizationService);
            _statusQueryWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_statusQueryWindow, window)) {
                    _statusQueryWindow = null;
                }
            };
            window.Show(this);
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.status_query_opened");
            return;
        }

        if (key == "module.group_people") {
            if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
                _txtStatus.Text = T("status.failed");
                _txtOutput.Text = T("msg.sqlite_missing");
                return;
            }

            if (_groupPeopleWindow is { } existingWindow) {
                if (existingWindow.WindowState == WindowState.Minimized) {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
                _txtOutput.Text = T("msg.group_people_opened");
                return;
            }

            var window = new GroupPeopleWindow(_sqlitePath, _localizationService);
            _groupPeopleWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_groupPeopleWindow, window)) {
                    _groupPeopleWindow = null;
                }
            };
            window.Show(this);
            _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
            _txtOutput.Text = T("msg.group_people_opened");
            return;
        }

        _txtStatus.Text = string.Format(T("status.module_selected"), moduleLabel);
        _txtOutput.Text = key == "module.browser" ? T("msg.browser_todo") : string.Format(T("msg.module_todo"), moduleLabel);
    }

    internal async Task OpenPersonBrowserWithIdsAsync(IReadOnlyList<int> personIds) {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = T("msg.sqlite_missing");
            return;
        }

        if (_personBrowserWindow is not { } window) {
            window = new PersonBrowserWindow(_sqlitePath, _localizationService);
            _personBrowserWindow = window;
            window.Closed += (_, _) => {
                if (ReferenceEquals(_personBrowserWindow, window)) {
                    _personBrowserWindow = null;
                }
            };
            window.Show();
        } else {
            if (window.WindowState == WindowState.Minimized) {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
        }

        await window.LoadPeopleByIdsAsync(personIds, T("browser.no_valid_person_ids"));
        _txtStatus.Text = string.Format(T("status.module_selected"), T("module.browser"));
        _txtOutput.Text = T("msg.browser_loaded_query_results");
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
                await EnsureRecommendedIndexesAsync(_sqlitePath, _txtOutput.Text);
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

    private void BtnDownloadLatestData_Click(object? sender, RoutedEventArgs e) {
        try {
            OpenExternalTarget(LatestDataUrl);
            _txtStatus.Text = T("button.download_latest_data");
            _txtOutput.Text = $"{T("msg.download_latest_data_opened")}{Environment.NewLine}{LatestDataUrl}";
        } catch (Exception ex) {
            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = ex.Message;
        }
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

    private void BtnAbout_Click(object? sender, RoutedEventArgs e) {
        if (Application.Current is App app) {
            app.ShowAboutWindow(this);
            _txtStatus.Text = T("about.title");
            _txtOutput.Text = T("msg.about_opened");
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
        _btnLangEn = this.FindControl<ToggleButton>("BtnLangEn") ?? throw new InvalidOperationException("BtnLangEn not found.");
        _btnLangZhHant = this.FindControl<ToggleButton>("BtnLangZhHant") ?? throw new InvalidOperationException("BtnLangZhHant not found.");
        _btnLangZhHans = this.FindControl<ToggleButton>("BtnLangZhHans") ?? throw new InvalidOperationException("BtnLangZhHans not found.");
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
        _btnModuleGroupPeople = this.FindControl<Button>("BtnModuleGroupPeople") ?? throw new InvalidOperationException("BtnModuleGroupPeople not found.");
        _btnModuleQueryExample = this.FindControl<Button>("BtnModuleQueryExample") ?? throw new InvalidOperationException("BtnModuleQueryExample not found.");
        _btnReportError = this.FindControl<Button>("BtnReportError") ?? throw new InvalidOperationException("BtnReportError not found.");
        _btnRelinkTables = this.FindControl<Button>("BtnRelinkTables") ?? throw new InvalidOperationException("BtnRelinkTables not found.");
        _btnDownloadLatestData = this.FindControl<Button>("BtnDownloadLatestData") ?? throw new InvalidOperationException("BtnDownloadLatestData not found.");
        _btnChangeIndexAddress = this.FindControl<Button>("BtnChangeIndexAddress") ?? throw new InvalidOperationException("BtnChangeIndexAddress not found.");
        _btnUsersGuide = this.FindControl<Button>("BtnUsersGuide") ?? throw new InvalidOperationException("BtnUsersGuide not found.");
        _btnAbout = this.FindControl<Button>("BtnAbout") ?? throw new InvalidOperationException("BtnAbout not found.");
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
            await EnsureRecommendedIndexesAsync(_sqlitePath, _txtOutput.Text);

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

    private async Task EnsureRecommendedIndexesAsync(string sqlitePath, string baseOutputText) {
        var check = await _databaseIndexService.CheckRecommendedIndexesAsync(sqlitePath);
        if (check.HasAllIndexes) {
            _txtStatus.Text = T("status.connected");
            _txtOutput.Text = baseOutputText;
            return;
        }

        var prompt = new ConfirmActionWindow(
            _localizationService,
            T("db_index.prompt_title"),
            string.Format(
                T("db_index.prompt_body"),
                check.MissingIndexNames.Count,
                string.Join(Environment.NewLine, check.MissingIndexNames)
            )
        );

        var accepted = await prompt.ShowDialog<bool?>(this);
        if (accepted != true) {
            _txtStatus.Text = T("status.connected");
            _txtOutput.Text = $"{baseOutputText}{Environment.NewLine}{string.Format(T("db_index.skipped"), check.MissingIndexNames.Count)}";
            return;
        }

        var progressWindow = new DatabaseIndexProgressWindow(_localizationService);
        progressWindow.UpdateProgress(
            T("db_index.progress_running"),
            string.Format(T("db_index.progress_step"), 0, check.MissingIndexNames.Count, "-"),
            0,
            check.MissingIndexNames.Count
        );

        var progressDialogTask = progressWindow.ShowDialog(this);
        var progress = new Progress<DatabaseIndexProgress>(update => {
            progressWindow.UpdateProgress(
                T("db_index.progress_running"),
                string.Format(T("db_index.progress_step"), update.CompletedSteps, update.TotalSteps, update.IndexName),
                update.CompletedSteps,
                update.TotalSteps
            );
        });

        try {
            _txtStatus.Text = T("status.checking");
            await Task.Run(() => _databaseIndexService.EnsureRecommendedIndexesAsync(sqlitePath, progress));
            progressWindow.UpdateProgress(
                T("db_index.completed"),
                string.Format(
                    T("db_index.progress_step"),
                    check.MissingIndexNames.Count,
                    check.MissingIndexNames.Count,
                    check.MissingIndexNames.Last()
                ),
                check.MissingIndexNames.Count,
                check.MissingIndexNames.Count
            );
            progressWindow.Close();
            await progressDialogTask;

            _txtStatus.Text = T("status.connected");
            _txtOutput.Text = $"{baseOutputText}{Environment.NewLine}{T("db_index.output_completed")}";
        } catch (Exception ex) {
            progressWindow.Close();
            await progressDialogTask;

            _txtStatus.Text = T("status.failed");
            _txtOutput.Text = $"{baseOutputText}{Environment.NewLine}{ex.Message}";
        }
    }
}
