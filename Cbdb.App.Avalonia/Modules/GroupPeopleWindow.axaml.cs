using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Modules;

public partial class GroupPeopleWindow : Window {
    private readonly string _sqlitePath;
    private readonly AppLocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService;
    private readonly IGroupPeopleService _groupPeopleService;

    private IReadOnlyList<int> _loadedPersonIds = Array.Empty<int>();
    private IReadOnlyList<PersonListItem> _loadedPeople = Array.Empty<PersonListItem>();
    private int _requestedPersonIdCount;

    private Button _btnImportPeople = null!;
    private TextBlock _lblDataTypes = null!;
    private CheckBox _chkStatus = null!;
    private CheckBox _chkOffice = null!;
    private CheckBox _chkEntry = null!;
    private CheckBox _chkTexts = null!;
    private CheckBox _chkAddresses = null!;
    private TextBlock _txtWorkflowHint = null!;
    private TextBlock _lblAddressMode = null!;
    private RadioButton _optAllAddresses = null!;
    private RadioButton _optIndexAddresses = null!;
    private Button _btnRunQuery = null!;
    private Button _btnOpenInBrowser = null!;
    private Button _btnClose = null!;
    private TextBlock _lblLoadedPeople = null!;
    private TextBlock _txtLoadedSummary = null!;
    private TextBox _txtLoadedIds = null!;
    private TabItem _tabStatus = null!;
    private TabItem _tabOffice = null!;
    private TabItem _tabEntry = null!;
    private TabItem _tabTexts = null!;
    private TabItem _tabAddresses = null!;
    private DataGrid _gridStatus = null!;
    private DataGrid _gridOffice = null!;
    private DataGrid _gridEntry = null!;
    private DataGrid _gridTexts = null!;
    private DataGrid _gridAddresses = null!;
    private TextBlock _txtStatusBar = null!;

    public GroupPeopleWindow()
        : this(string.Empty, new AppLocalizationService(), new SqlitePersonBrowserService(), new SqliteGroupPeopleService()) {
    }

    public GroupPeopleWindow(string sqlitePath, AppLocalizationService localizationService)
        : this(sqlitePath, localizationService, new SqlitePersonBrowserService(), new SqliteGroupPeopleService()) {
    }

    public GroupPeopleWindow(
        string sqlitePath,
        AppLocalizationService localizationService,
        IPersonBrowserService personBrowserService,
        IGroupPeopleService groupPeopleService
    ) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;
        _personBrowserService = personBrowserService;
        _groupPeopleService = groupPeopleService;

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        UpdateAddressModeEnabledState();
        UpdateLoadedSummary();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _btnImportPeople = this.FindControl<Button>("BtnImportPeople") ?? throw new InvalidOperationException("BtnImportPeople not found.");
        _lblDataTypes = this.FindControl<TextBlock>("LblDataTypes") ?? throw new InvalidOperationException("LblDataTypes not found.");
        _chkStatus = this.FindControl<CheckBox>("ChkStatus") ?? throw new InvalidOperationException("ChkStatus not found.");
        _chkOffice = this.FindControl<CheckBox>("ChkOffice") ?? throw new InvalidOperationException("ChkOffice not found.");
        _chkEntry = this.FindControl<CheckBox>("ChkEntry") ?? throw new InvalidOperationException("ChkEntry not found.");
        _chkTexts = this.FindControl<CheckBox>("ChkTexts") ?? throw new InvalidOperationException("ChkTexts not found.");
        _chkAddresses = this.FindControl<CheckBox>("ChkAddresses") ?? throw new InvalidOperationException("ChkAddresses not found.");
        _txtWorkflowHint = this.FindControl<TextBlock>("TxtWorkflowHint") ?? throw new InvalidOperationException("TxtWorkflowHint not found.");
        _lblAddressMode = this.FindControl<TextBlock>("LblAddressMode") ?? throw new InvalidOperationException("LblAddressMode not found.");
        _optAllAddresses = this.FindControl<RadioButton>("OptAllAddresses") ?? throw new InvalidOperationException("OptAllAddresses not found.");
        _optIndexAddresses = this.FindControl<RadioButton>("OptIndexAddresses") ?? throw new InvalidOperationException("OptIndexAddresses not found.");
        _btnRunQuery = this.FindControl<Button>("BtnRunQuery") ?? throw new InvalidOperationException("BtnRunQuery not found.");
        _btnOpenInBrowser = this.FindControl<Button>("BtnOpenInBrowser") ?? throw new InvalidOperationException("BtnOpenInBrowser not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
        _lblLoadedPeople = this.FindControl<TextBlock>("LblLoadedPeople") ?? throw new InvalidOperationException("LblLoadedPeople not found.");
        _txtLoadedSummary = this.FindControl<TextBlock>("TxtLoadedSummary") ?? throw new InvalidOperationException("TxtLoadedSummary not found.");
        _txtLoadedIds = this.FindControl<TextBox>("TxtLoadedIds") ?? throw new InvalidOperationException("TxtLoadedIds not found.");
        _tabStatus = this.FindControl<TabItem>("TabStatus") ?? throw new InvalidOperationException("TabStatus not found.");
        _tabOffice = this.FindControl<TabItem>("TabOffice") ?? throw new InvalidOperationException("TabOffice not found.");
        _tabEntry = this.FindControl<TabItem>("TabEntry") ?? throw new InvalidOperationException("TabEntry not found.");
        _tabTexts = this.FindControl<TabItem>("TabTexts") ?? throw new InvalidOperationException("TabTexts not found.");
        _tabAddresses = this.FindControl<TabItem>("TabAddresses") ?? throw new InvalidOperationException("TabAddresses not found.");
        _gridStatus = this.FindControl<DataGrid>("GridStatus") ?? throw new InvalidOperationException("GridStatus not found.");
        _gridOffice = this.FindControl<DataGrid>("GridOffice") ?? throw new InvalidOperationException("GridOffice not found.");
        _gridEntry = this.FindControl<DataGrid>("GridEntry") ?? throw new InvalidOperationException("GridEntry not found.");
        _gridTexts = this.FindControl<DataGrid>("GridTexts") ?? throw new InvalidOperationException("GridTexts not found.");
        _gridAddresses = this.FindControl<DataGrid>("GridAddresses") ?? throw new InvalidOperationException("GridAddresses not found.");
        _txtStatusBar = this.FindControl<TextBlock>("TxtStatusBar") ?? throw new InvalidOperationException("TxtStatusBar not found.");
    }

    private void ApplyLocalization() {
        Title = T("group_people.title");
        _btnImportPeople.Content = T("group_people.import_people");
        _btnOpenInBrowser.Content = T("query.open_in_browser");
        _txtWorkflowHint.Text = T("group_people.workflow_hint");
        _lblDataTypes.Text = T("group_people.data_types");
        _chkStatus.Content = T("group_people.status");
        _chkOffice.Content = T("group_people.office");
        _chkEntry.Content = T("group_people.entry");
        _chkTexts.Content = T("group_people.texts");
        _chkAddresses.Content = T("group_people.addresses");
        _lblAddressMode.Text = T("group_people.address_mode");
        _optAllAddresses.Content = T("group_people.all_addresses");
        _optIndexAddresses.Content = T("group_people.index_addresses");
        _btnRunQuery.Content = T("group_people.run_query");
        _btnClose.Content = T("button.exit");
        _lblLoadedPeople.Text = T("group_people.loaded_people");
        _tabStatus.Header = T("group_people.status");
        _tabOffice.Header = T("group_people.office_holding");
        _tabEntry.Header = T("group_people.entry_government");
        _tabTexts.Header = T("group_people.text_production");
        _tabAddresses.Header = T("group_people.place_association");

        SetGridHeaders(_gridStatus, [
            T("browser.grid_person_id"),
            T("browser.grid_name_chn"),
            T("browser.name"),
            T("browser.address_sequence"),
            T("browser.status_name"),
            T("status_query.first_year"),
            T("status_query.last_year"),
            T("browser.source_title"),
            T("browser.address_pages"),
            T("browser.notes")
        ]);

        SetGridHeaders(_gridOffice, [
            T("browser.grid_person_id"),
            T("browser.grid_name_chn"),
            T("browser.name"),
            T("group_people.posting_id"),
            T("office_query.office_code"),
            T("browser.posting_office"),
            T("browser.posting_appointment"),
            T("browser.posting_assume_office"),
            T("browser.posting_addresses"),
            T("office_query.first_year"),
            T("office_query.last_year"),
            T("browser.source_title"),
            T("browser.address_pages"),
            T("browser.notes")
        ]);

        SetGridHeaders(_gridEntry, [
            T("browser.grid_person_id"),
            T("browser.grid_name_chn"),
            T("browser.name"),
            T("browser.address_sequence"),
            T("browser.entry_method"),
            T("browser.entry_year"),
            T("group_people.entry_location"),
            T("browser.source_title"),
            T("browser.address_pages"),
            T("browser.notes")
        ]);

        SetGridHeaders(_gridTexts, [
            T("browser.grid_person_id"),
            T("browser.grid_name_chn"),
            T("browser.name"),
            T("group_people.text_id"),
            T("browser.association_text_title"),
            T("browser.writing_role"),
            T("browser.entry_year"),
            T("browser.source_title"),
            T("browser.address_pages"),
            T("browser.notes")
        ]);

        SetGridHeaders(_gridAddresses, [
            T("browser.grid_person_id"),
            T("browser.grid_name_chn"),
            T("browser.name"),
            T("browser.address_id"),
            T("browser.grid_index_address"),
            T("browser.index_address_type"),
            T("status_query.first_year"),
            T("status_query.last_year"),
            T("browser.source_title"),
            T("browser.notes"),
            T("group_people.is_index_address")
        ]);

        UpdateLoadedSummary();
    }

    private static void SetGridHeaders(DataGrid grid, IReadOnlyList<string> headers) {
        for (var i = 0; i < headers.Count && i < grid.Columns.Count; i++) {
            grid.Columns[i].Header = headers[i];
        }
    }

    private string T(string key) => _localizationService.Get(key);

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private async void BtnImportPeople_Click(object? sender, RoutedEventArgs e) {
        await LoadPeopleFromFileAsync(T("group_people.import_people"));
    }

    private async Task LoadPeopleFromFileAsync(string title) {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _txtStatusBar.Text = T("msg.sqlite_missing");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> {
                new(T("browser.csv_or_text_files")) {
                    Patterns = new[] { "*.csv", "*.txt" }
                }
            }
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var content = await File.ReadAllTextAsync(path, Encoding.UTF8);
            var personIds = PersonIdImportParser.Parse(content);
            await LoadPeopleAsync(personIds);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private async Task LoadPeopleAsync(IReadOnlyList<int> personIds) {
        _loadedPersonIds = Array.Empty<int>();
        _loadedPeople = Array.Empty<PersonListItem>();
        _requestedPersonIdCount = personIds.Count;
        ClearResults();

        if (personIds.Count == 0) {
            UpdateLoadedSummary();
            _txtStatusBar.Text = T("browser.no_valid_person_ids");
            return;
        }

        try {
            var people = await _personBrowserService.GetPeopleByIdsAsync(_sqlitePath, personIds);
            _loadedPeople = people;
            _loadedPersonIds = people.Select(person => person.PersonId).ToArray();
            UpdateLoadedSummary();
            _txtStatusBar.Text = _loadedPersonIds.Count == 0
                ? T("browser.no_matching_person_ids")
                : string.Format(T("group_people.loaded_people_summary"), _loadedPersonIds.Count);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private void UpdateLoadedSummary() {
        if (_loadedPersonIds.Count == 0) {
            _txtLoadedSummary.Text = T("group_people.no_people_loaded");
            _txtLoadedIds.Text = string.Empty;
            return;
        }

        _txtLoadedSummary.Text = string.Format(T("group_people.loaded_people_detail"), _loadedPersonIds.Count, _loadedPeople.Count);
        if (_requestedPersonIdCount > 0) {
            _txtLoadedSummary.Text = string.Format(T("group_people.loaded_people_detail"), _requestedPersonIdCount, _loadedPeople.Count);
        }
        _txtLoadedIds.Text = string.Join(Environment.NewLine, _loadedPeople.Select(person =>
            $"{person.PersonId}\t{person.NameChn ?? string.Empty}\t{person.NameRm ?? string.Empty}"));
    }

    private async void BtnRunQuery_Click(object? sender, RoutedEventArgs e) {
        if (_loadedPersonIds.Count == 0) {
            _txtStatusBar.Text = T("group_people.no_people_loaded");
            return;
        }

        var options = BuildQueryOptions();
        if (!options.IncludeStatus && !options.IncludeOffice && !options.IncludeEntry && !options.IncludeTexts && !options.IncludeAddresses) {
            _txtStatusBar.Text = T("group_people.no_data_type_selected");
            return;
        }

        try {
            _txtStatusBar.Text = T("group_people.running");
            var result = await _groupPeopleService.QueryAsync(_sqlitePath, _loadedPersonIds, options);
            _gridStatus.ItemsSource = result.StatusRecords;
            _gridOffice.ItemsSource = result.OfficeRecords;
            _gridEntry.ItemsSource = result.EntryRecords;
            _gridTexts.ItemsSource = result.TextRecords;
            _gridAddresses.ItemsSource = result.AddressRecords;
            UpdateTabVisibility(options);

            var total = result.StatusRecords.Count + result.OfficeRecords.Count + result.EntryRecords.Count + result.TextRecords.Count + result.AddressRecords.Count;
            _txtStatusBar.Text = string.Format(
                T("group_people.results_summary"),
                total,
                result.StatusRecords.Count,
                result.OfficeRecords.Count,
                result.EntryRecords.Count,
                result.TextRecords.Count,
                result.AddressRecords.Count
            );
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private GroupPeopleQueryOptions BuildQueryOptions() {
        return new GroupPeopleQueryOptions(
            IncludeStatus: _chkStatus.IsChecked == true,
            IncludeOffice: _chkOffice.IsChecked == true,
            IncludeEntry: _chkEntry.IsChecked == true,
            IncludeTexts: _chkTexts.IsChecked == true,
            IncludeAddresses: _chkAddresses.IsChecked == true,
            AddressMode: _optIndexAddresses.IsChecked == true ? GroupPeopleAddressMode.IndexAddresses : GroupPeopleAddressMode.AllAddresses
        );
    }

    private void UpdateTabVisibility(GroupPeopleQueryOptions options) {
        _tabStatus.IsVisible = options.IncludeStatus;
        _tabOffice.IsVisible = options.IncludeOffice;
        _tabEntry.IsVisible = options.IncludeEntry;
        _tabTexts.IsVisible = options.IncludeTexts;
        _tabAddresses.IsVisible = options.IncludeAddresses;
    }

    private void ClearResults() {
        _gridStatus.ItemsSource = Array.Empty<GroupStatusRecord>();
        _gridOffice.ItemsSource = Array.Empty<GroupOfficeRecord>();
        _gridEntry.ItemsSource = Array.Empty<GroupEntryRecord>();
        _gridTexts.ItemsSource = Array.Empty<GroupTextRecord>();
        _gridAddresses.ItemsSource = Array.Empty<GroupAddressRecord>();
        UpdateTabVisibility(BuildQueryOptions());
    }

    private async void BtnOpenInBrowser_Click(object? sender, RoutedEventArgs e) {
        if (_loadedPersonIds.Count == 0) {
            _txtStatusBar.Text = T("group_people.no_people_loaded");
            return;
        }

        if (Owner is MainWindow mainWindow) {
            await mainWindow.OpenPersonBrowserWithIdsAsync(_loadedPersonIds);
            _txtStatusBar.Text = T("query.loaded_into_browser");
        }
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private void AddressModeControl_Changed(object? sender, RoutedEventArgs e) {
        UpdateAddressModeEnabledState();
    }

    private void UpdateAddressModeEnabledState() {
        var enabled = _chkAddresses.IsChecked == true;
        _lblAddressMode.IsEnabled = enabled;
        _optAllAddresses.IsEnabled = enabled;
        _optIndexAddresses.IsEnabled = enabled;
    }
}
