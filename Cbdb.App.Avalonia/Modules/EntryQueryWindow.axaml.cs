using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cbdb.App.Avalonia.Controls;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Modules;

public partial class EntryQueryWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly IEntryQueryService _entryQueryService = new SqliteEntryQueryService();
    private readonly IPlaceLookupService _placeLookupService = new SqlitePlaceLookupService();
    private readonly string _sqlitePath;

    private EntryPickerData _entryPickerData = new(
        new EntryTypeNode(EntryPickerData.RootCode, null, null, Array.Empty<EntryTypeNode>(), Array.Empty<string>()),
        Array.Empty<EntryCodeOption>(),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    );
    private IReadOnlyList<PlaceOption> _placeOptions = Array.Empty<PlaceOption>();
    private IReadOnlyList<EntryCodeOption> _entryOptions = Array.Empty<EntryCodeOption>();
    private List<string> _selectedEntryCodes = new();
    private List<int> _selectedPlaceIds = new();
    private IReadOnlyList<EntryQueryRecord> _records = Array.Empty<EntryQueryRecord>();
    private IReadOnlyList<EntryQueryPerson> _people = Array.Empty<EntryQueryPerson>();

    private TextBlock _lblPersonKeyword = null!;
    private TextBox _txtPersonKeyword = null!;
    private TextBlock _lblEntrySelection = null!;
    private TextBox _txtSelectedEntries = null!;
    private TextBlock _lblPlaceSelection = null!;
    private TextBox _txtSelectedPlaces = null!;
    private Button _btnSelectPlaces = null!;
    private Button _btnClearPlaces = null!;
    private CheckBox _chkIncludeSubUnits = null!;
    private CheckBox _chkUseIndexYear = null!;
    private TextBlock _lblIndexYearTo = null!;
    private TextBox _txtIndexYearFrom = null!;
    private TextBox _txtIndexYearTo = null!;
    private CheckBox _chkUseEntryYear = null!;
    private TextBlock _lblEntryYearTo = null!;
    private TextBox _txtEntryYearFrom = null!;
    private TextBox _txtEntryYearTo = null!;
    private DynastyRangePicker _dynastyPicker = null!;
    private Button _btnSelectEntries = null!;
    private Button _btnClearEntries = null!;
    private Button _btnRunQuery = null!;
    private Button _btnSavePersonIds = null!;
    private Button _btnSaveEntryCodes = null!;
    private Button _btnOpenInBrowser = null!;
    private Button _btnClose = null!;
    private TabItem _tabRecords = null!;
    private TabItem _tabPeople = null!;
    private DataGrid _gridRecords = null!;
    private DataGrid _gridPeople = null!;
    private TextBlock _txtStatusBar = null!;

    public EntryQueryWindow()
        : this(string.Empty, new AppLocalizationService()) {
    }

    public EntryQueryWindow(string sqlitePath, AppLocalizationService localizationService) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        _chkUseIndexYear.IsChecked = false;
        _txtIndexYearFrom.Text = "-200";
        _txtIndexYearTo.Text = "1911";
        _chkUseEntryYear.IsChecked = false;
        _txtEntryYearFrom.Text = "-200";
        _txtEntryYearTo.Text = "1911";
        _chkUseIndexYear.IsCheckedChanged += ChkUseIndexYear_IsCheckedChanged;
        _chkUseEntryYear.IsCheckedChanged += ChkUseEntryYear_IsCheckedChanged;
        UpdateIndexYearEnabledState();
        UpdateEntryYearEnabledState();

        ApplyLocalization();
        Opened += EntryQueryWindow_Opened;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void InitializeControls() {
        _lblPersonKeyword = this.FindControl<TextBlock>("LblPersonKeyword") ?? throw new InvalidOperationException("LblPersonKeyword not found.");
        _txtPersonKeyword = this.FindControl<TextBox>("TxtPersonKeyword") ?? throw new InvalidOperationException("TxtPersonKeyword not found.");
        _lblEntrySelection = this.FindControl<TextBlock>("LblEntrySelection") ?? throw new InvalidOperationException("LblEntrySelection not found.");
        _txtSelectedEntries = this.FindControl<TextBox>("TxtSelectedEntries") ?? throw new InvalidOperationException("TxtSelectedEntries not found.");
        _lblPlaceSelection = this.FindControl<TextBlock>("LblPlaceSelection") ?? throw new InvalidOperationException("LblPlaceSelection not found.");
        _txtSelectedPlaces = this.FindControl<TextBox>("TxtSelectedPlaces") ?? throw new InvalidOperationException("TxtSelectedPlaces not found.");
        _btnSelectPlaces = this.FindControl<Button>("BtnSelectPlaces") ?? throw new InvalidOperationException("BtnSelectPlaces not found.");
        _btnClearPlaces = this.FindControl<Button>("BtnClearPlaces") ?? throw new InvalidOperationException("BtnClearPlaces not found.");
        _chkIncludeSubUnits = this.FindControl<CheckBox>("ChkIncludeSubUnits") ?? throw new InvalidOperationException("ChkIncludeSubUnits not found.");
        _chkUseIndexYear = this.FindControl<CheckBox>("ChkUseIndexYear") ?? throw new InvalidOperationException("ChkUseIndexYear not found.");
        _lblIndexYearTo = this.FindControl<TextBlock>("LblIndexYearTo") ?? throw new InvalidOperationException("LblIndexYearTo not found.");
        _txtIndexYearFrom = this.FindControl<TextBox>("TxtIndexYearFrom") ?? throw new InvalidOperationException("TxtIndexYearFrom not found.");
        _txtIndexYearTo = this.FindControl<TextBox>("TxtIndexYearTo") ?? throw new InvalidOperationException("TxtIndexYearTo not found.");
        _chkUseEntryYear = this.FindControl<CheckBox>("ChkUseEntryYear") ?? throw new InvalidOperationException("ChkUseEntryYear not found.");
        _lblEntryYearTo = this.FindControl<TextBlock>("LblEntryYearTo") ?? throw new InvalidOperationException("LblEntryYearTo not found.");
        _txtEntryYearFrom = this.FindControl<TextBox>("TxtEntryYearFrom") ?? throw new InvalidOperationException("TxtEntryYearFrom not found.");
        _txtEntryYearTo = this.FindControl<TextBox>("TxtEntryYearTo") ?? throw new InvalidOperationException("TxtEntryYearTo not found.");
        _dynastyPicker = this.FindControl<DynastyRangePicker>("DynastyPicker") ?? throw new InvalidOperationException("DynastyPicker not found.");
        _btnSelectEntries = this.FindControl<Button>("BtnSelectEntries") ?? throw new InvalidOperationException("BtnSelectEntries not found.");
        _btnClearEntries = this.FindControl<Button>("BtnClearEntries") ?? throw new InvalidOperationException("BtnClearEntries not found.");
        _btnRunQuery = this.FindControl<Button>("BtnRunQuery") ?? throw new InvalidOperationException("BtnRunQuery not found.");
        _btnSavePersonIds = this.FindControl<Button>("BtnSavePersonIds") ?? throw new InvalidOperationException("BtnSavePersonIds not found.");
        _btnSaveEntryCodes = this.FindControl<Button>("BtnSaveEntryCodes") ?? throw new InvalidOperationException("BtnSaveEntryCodes not found.");
        _btnOpenInBrowser = this.FindControl<Button>("BtnOpenInBrowser") ?? throw new InvalidOperationException("BtnOpenInBrowser not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
        _tabRecords = this.FindControl<TabItem>("TabRecords") ?? throw new InvalidOperationException("TabRecords not found.");
        _tabPeople = this.FindControl<TabItem>("TabPeople") ?? throw new InvalidOperationException("TabPeople not found.");
        _gridRecords = this.FindControl<DataGrid>("GridRecords") ?? throw new InvalidOperationException("GridRecords not found.");
        _gridPeople = this.FindControl<DataGrid>("GridPeople") ?? throw new InvalidOperationException("GridPeople not found.");
        _txtStatusBar = this.FindControl<TextBlock>("TxtStatusBar") ?? throw new InvalidOperationException("TxtStatusBar not found.");
    }

    private async void EntryQueryWindow_Opened(object? sender, EventArgs e) {
        Opened -= EntryQueryWindow_Opened;
        await LoadFiltersAsync();
    }

    private async Task LoadFiltersAsync() {
        try {
            _entryPickerData = await _entryQueryService.GetEntryPickerDataAsync(_sqlitePath);
            _entryOptions = _entryPickerData.AllEntryCodes;
            _placeOptions = await _placeLookupService.GetPlacesAsync(_sqlitePath);
            await _dynastyPicker.LoadDynastiesAsync(_sqlitePath);
            UpdateSelectedEntriesText();
            UpdateSelectedPlacesText();
            _txtStatusBar.Text = string.Format(T("entry_query.loaded_filters"), _entryOptions.Count, _dynastyPicker.OptionCount);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private void ApplyLocalization() {
        Title = T("entry_query.title");
        _lblPersonKeyword.Text = T("entry_query.person_keyword");
        _lblEntrySelection.Text = T("entry_query.selected_entries");
        _lblPlaceSelection.Text = T("entry_query.selected_places");
        _btnSelectPlaces.Content = T("entry_query.select_places");
        _btnClearPlaces.Content = T("entry_query.clear_places");
        _chkIncludeSubUnits.Content = T("entry_query.include_subunits");
        _chkUseIndexYear.Content = T("entry_query.use_index_year");
        _lblIndexYearTo.Text = T("entry_query.to");
        _chkUseEntryYear.Content = T("entry_query.use_entry_year");
        _lblEntryYearTo.Text = T("entry_query.to");
        _btnSelectEntries.Content = T("entry_query.select_entries");
        _btnClearEntries.Content = T("entry_query.clear_entries");
        _btnRunQuery.Content = T("entry_query.run_query");
        _btnSavePersonIds.Content = T("entry_query.save_person_ids");
        _btnSaveEntryCodes.Content = T("entry_query.save_entry_codes");
        _btnOpenInBrowser.Content = T("query.open_in_browser");
        _btnClose.Content = T("button.exit");
        _tabRecords.Header = T("entry_query.tab_records");
        _tabPeople.Header = T("entry_query.tab_people");
        _dynastyPicker.Configure(_localizationService);

        var rc = _gridRecords.Columns;
        ((DataGridTextColumn)rc[0]).Header = T("browser.grid_person_id");
        ((DataGridTextColumn)rc[1]).Header = T("browser.grid_name_chn");
        ((DataGridTextColumn)rc[2]).Header = T("browser.name");
        ((DataGridTextColumn)rc[3]).Header = T("browser.grid_index_year");
        ((DataGridTextColumn)rc[4]).Header = T("browser.index_year_type");
        ((DataGridTextColumn)rc[5]).Header = T("browser.gender");
        ((DataGridTextColumn)rc[6]).Header = T("browser.dynasty");
        ((DataGridTextColumn)rc[7]).Header = T("browser.index_address_type");
        ((DataGridTextColumn)rc[8]).Header = T("browser.entry_method");
        ((DataGridTextColumn)rc[9]).Header = T("entry_query.entry_code");
        ((DataGridTextColumn)rc[10]).Header = T("browser.entry_year");
        ((DataGridTextColumn)rc[11]).Header = T("browser.entry_nianhao");
        ((DataGridTextColumn)rc[12]).Header = T("browser.association_nianhao_year");
        ((DataGridTextColumn)rc[13]).Header = T("browser.association_range");
        ((DataGridTextColumn)rc[14]).Header = T("browser.entry_exam_rank");
        ((DataGridTextColumn)rc[15]).Header = T("browser.entry_age");
        ((DataGridTextColumn)rc[16]).Header = T("browser.entry_kinship");
        ((DataGridTextColumn)rc[17]).Header = T("browser.entry_kin_person");
        ((DataGridTextColumn)rc[18]).Header = T("browser.entry_association");
        ((DataGridTextColumn)rc[19]).Header = T("browser.entry_associate_person");
        ((DataGridTextColumn)rc[20]).Header = T("browser.entry_institution");
        ((DataGridTextColumn)rc[21]).Header = T("browser.entry_exam_field");
        ((DataGridTextColumn)rc[22]).Header = T("browser.address_id");
        ((DataGridTextColumn)rc[23]).Header = T("browser.entry_address");
        ((DataGridTextColumn)rc[24]).Header = "X";
        ((DataGridTextColumn)rc[25]).Header = "Y";
        ((DataGridTextColumn)rc[26]).Header = T("query.people_at_place");
        ((DataGridTextColumn)rc[27]).Header = T("browser.entry_parental_status");
        ((DataGridTextColumn)rc[28]).Header = T("browser.entry_attempt_count");
        ((DataGridTextColumn)rc[29]).Header = T("browser.source_title");
        ((DataGridTextColumn)rc[30]).Header = T("browser.address_pages");
        ((DataGridTextColumn)rc[31]).Header = T("browser.notes");
        ((DataGridTextColumn)rc[32]).Header = T("browser.entry_posting_notes");

        var pc = _gridPeople.Columns;
        ((DataGridTextColumn)pc[0]).Header = T("browser.grid_person_id");
        ((DataGridTextColumn)pc[1]).Header = T("browser.grid_name_chn");
        ((DataGridTextColumn)pc[2]).Header = T("browser.name");
        ((DataGridTextColumn)pc[3]).Header = T("browser.grid_index_year");
        ((DataGridTextColumn)pc[4]).Header = T("browser.index_year_type");
        ((DataGridTextColumn)pc[5]).Header = T("browser.gender");
        ((DataGridTextColumn)pc[6]).Header = T("browser.dynasty");
        ((DataGridTextColumn)pc[7]).Header = T("browser.address_id");
        ((DataGridTextColumn)pc[8]).Header = T("browser.grid_index_address");
        ((DataGridTextColumn)pc[9]).Header = T("browser.index_address_type");
        ((DataGridTextColumn)pc[10]).Header = T("browser.address_id");
        ((DataGridTextColumn)pc[11]).Header = T("browser.entry_address");
        ((DataGridTextColumn)pc[12]).Header = "X";
        ((DataGridTextColumn)pc[13]).Header = "Y";
        ((DataGridTextColumn)pc[14]).Header = T("query.people_at_place");
        ((DataGridTextColumn)pc[15]).Header = T("entry_query.entry_count");

        UpdateSelectedEntriesText();
        UpdateSelectedPlacesText();
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateStatusBarCounts();
    }

    private void ChkUseIndexYear_IsCheckedChanged(object? sender, RoutedEventArgs e) => UpdateIndexYearEnabledState();

    private void ChkUseEntryYear_IsCheckedChanged(object? sender, RoutedEventArgs e) => UpdateEntryYearEnabledState();

    private async void BtnSelectEntries_Click(object? sender, RoutedEventArgs e) {
        if (_entryOptions.Count == 0) {
            await LoadFiltersAsync();
        }

        var picker = new EntryCodePickerWindow(_localizationService, _entryPickerData, _selectedEntryCodes);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedEntryCodes = picker.SelectedCodes.ToList();
            UpdateSelectedEntriesText();
        }
    }

    private void BtnClearEntries_Click(object? sender, RoutedEventArgs e) {
        _selectedEntryCodes.Clear();
        UpdateSelectedEntriesText();
    }

    private async void BtnSelectPlaces_Click(object? sender, RoutedEventArgs e) {
        if (_placeOptions.Count == 0) {
            await LoadFiltersAsync();
        }

        var picker = new PlacePickerWindow(_localizationService, _placeOptions, _selectedPlaceIds);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedPlaceIds = picker.SelectedPlaceIds.ToList();
            UpdateSelectedPlacesText();
        }
    }

    private void BtnClearPlaces_Click(object? sender, RoutedEventArgs e) {
        _selectedPlaceIds.Clear();
        UpdateSelectedPlacesText();
    }

    private async void BtnRunQuery_Click(object? sender, RoutedEventArgs e) {
        try {
            _btnRunQuery.IsEnabled = false;
            _txtStatusBar.Text = T("entry_query.running");

            var result = await _entryQueryService.QueryAsync(_sqlitePath, BuildRequest());
            _records = result.Records;
            _people = result.People;
            _gridRecords.ItemsSource = _records;
            _gridPeople.ItemsSource = _people;
            UpdateStatusBarCounts();
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        } finally {
            _btnRunQuery.IsEnabled = true;
        }
    }

    private async void BtnSavePersonIds_Click(object? sender, RoutedEventArgs e) {
        if (_people.Count == 0) {
            _txtStatusBar.Text = T("browser.no_data_to_export");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = T("entry_query.save_person_ids"),
            SuggestedFileName = $"cbdb_entry_person_ids_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            FileTypeChoices = new List<FilePickerFileType> { new("Text files") { Patterns = new[] { "*.txt" } } }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            await File.WriteAllLinesAsync(path, _people.Select(person => person.PersonId.ToString()));
            _txtStatusBar.Text = string.Format(T("entry_query.saved_person_ids"), _people.Count, path);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private async void BtnSaveEntryCodes_Click(object? sender, RoutedEventArgs e) {
        if (_selectedEntryCodes.Count == 0) {
            _txtStatusBar.Text = T("entry_query.no_entry_codes_to_export");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = T("entry_query.save_entry_codes"),
            SuggestedFileName = $"cbdb_entry_codes_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = new List<FilePickerFileType> { new("CSV files") { Patterns = new[] { "*.csv" } } }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var selectedOptions = _entryOptions.Where(option => _selectedEntryCodes.Contains(option.Code)).ToList();
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", new[] {
                EscapeCsv(T("entry_query.entry_code")),
                EscapeCsv(T("browser.entry_method")),
                EscapeCsv(T("browser.name"))
            }));

            foreach (var option in selectedOptions) {
                builder.AppendLine(string.Join(",", new[] {
                    EscapeCsv(option.Code),
                    EscapeCsv(option.DescriptionChn),
                    EscapeCsv(option.Description)
                }));
            }

            await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(true));
            _txtStatusBar.Text = string.Format(T("entry_query.saved_entry_codes"), selectedOptions.Count, path);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private async void BtnOpenInBrowser_Click(object? sender, RoutedEventArgs e) {
        if (_people.Count == 0) {
            _txtStatusBar.Text = T("browser.no_data_to_export");
            return;
        }

        if (Owner is MainWindow mainWindow) {
            await mainWindow.OpenPersonBrowserWithIdsAsync(_people.Select(person => person.PersonId).ToList());
            _txtStatusBar.Text = T("query.loaded_into_browser");
        }
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) => Close();

    private EntryQueryRequest BuildRequest() {
        var (selectedFrom, selectedTo) = _dynastyPicker.GetNormalizedRange();

        return new EntryQueryRequest(
            PersonKeyword: NormalizeText(_txtPersonKeyword.Text),
            EntryCodes: _selectedEntryCodes,
            PlaceIds: _selectedPlaceIds,
            IncludeSubordinateUnits: _chkIncludeSubUnits.IsChecked == true,
            UseIndexYearRange: _chkUseIndexYear.IsChecked == true,
            IndexYearFrom: ParseInt(_txtIndexYearFrom.Text, -200),
            IndexYearTo: ParseInt(_txtIndexYearTo.Text, 1911),
            UseEntryYearRange: _chkUseEntryYear.IsChecked == true,
            EntryYearFrom: ParseInt(_txtEntryYearFrom.Text, -200),
            EntryYearTo: ParseInt(_txtEntryYearTo.Text, 1911),
            UseDynastyRange: _dynastyPicker.UseDynastyRange,
            DynastyFrom: selectedFrom,
            DynastyTo: selectedTo
        );
    }

    private void UpdateIndexYearEnabledState() {
        var isEnabled = _chkUseIndexYear.IsChecked == true;
        _txtIndexYearFrom.IsEnabled = isEnabled;
        _txtIndexYearTo.IsEnabled = isEnabled;
        _lblIndexYearTo.IsEnabled = isEnabled;
    }

    private void UpdateEntryYearEnabledState() {
        var isEnabled = _chkUseEntryYear.IsChecked == true;
        _txtEntryYearFrom.IsEnabled = isEnabled;
        _txtEntryYearTo.IsEnabled = isEnabled;
        _lblEntryYearTo.IsEnabled = isEnabled;
    }

    private void UpdateSelectedEntriesText() {
        if (_selectedEntryCodes.Count == 0) {
            _txtSelectedEntries.Text = T("entry_query.all_entries");
            return;
        }

        var selectedOptions = _entryOptions
            .Where(option => _selectedEntryCodes.Contains(option.Code))
            .ToList();

        if (selectedOptions.Count == 1) {
            _txtSelectedEntries.Text = selectedOptions[0].DisplayLabel;
            return;
        }

        if (selectedOptions.Count == _entryOptions.Count && _entryOptions.Count > 0) {
            _txtSelectedEntries.Text = T("entry_query.all_entries");
            return;
        }

        var matchingType = FindMatchingEntryType(_entryPickerData.Root, _selectedEntryCodes);
        if (matchingType is not null) {
            _txtSelectedEntries.Text = GetEntryTypeDisplayLabel(matchingType);
            return;
        }

        _txtSelectedEntries.Text = T("entry_query.multi_select");
    }

    private void UpdateSelectedPlacesText() {
        if (_selectedPlaceIds.Count == 0) {
            _txtSelectedPlaces.Text = T("entry_query.all_places");
            return;
        }

        var labels = _placeOptions
            .Where(option => _selectedPlaceIds.Contains(option.AddressId))
            .Select(option => option.DisplayLabel)
            .Take(3)
            .ToList();

        var suffix = _selectedPlaceIds.Count > labels.Count
            ? string.Format(T("entry_query.more_selected"), _selectedPlaceIds.Count - labels.Count)
            : string.Empty;

        _txtSelectedPlaces.Text = string.Join("; ", labels) + suffix;
    }

    private void UpdateStatusBarCounts() {
        _txtStatusBar.Text = string.Format(T("entry_query.results_summary"), _records.Count, _people.Count);
    }

    private static int ParseInt(string? value, int fallback) => int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;

    private static EntryTypeNode? FindMatchingEntryType(EntryTypeNode node, IReadOnlyCollection<string> selectedCodes) {
        foreach (var child in node.Children) {
            var match = FindMatchingEntryType(child, selectedCodes);
            if (match is not null) {
                return match;
            }
        }

        return node.IsRoot || node.EntryCodes.Count == 0 || !node.EntryCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).SequenceEqual(selectedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
            ? null
            : node;
    }

    private static string GetEntryTypeDisplayLabel(EntryTypeNode node) {
        if (!string.IsNullOrWhiteSpace(node.DescriptionChn) && !string.IsNullOrWhiteSpace(node.Description)) {
            return $"{node.DescriptionChn} / {node.Description}";
        }

        return node.DescriptionChn ?? node.Description ?? node.Code;
    }

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeCsv(string? value) {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private string T(string key) => _localizationService.Get(key);
}
