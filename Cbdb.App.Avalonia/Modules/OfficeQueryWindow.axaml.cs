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

public partial class OfficeQueryWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly IOfficeQueryService _officeQueryService;
    private readonly IPlaceLookupService _placeLookupService;
    private readonly string _sqlitePath;

    private OfficePickerData _officePickerData = new(
        new OfficeTypeNode(OfficePickerData.RootCode, null, null, Array.Empty<OfficeTypeNode>(), Array.Empty<string>()),
        Array.Empty<OfficeCodeOption>(),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    );
    private IReadOnlyList<PlaceOption> _placeOptions = Array.Empty<PlaceOption>();
    private IReadOnlyList<OfficeCodeOption> _officeOptions = Array.Empty<OfficeCodeOption>();
    private List<string> _selectedOfficeCodes = new();
    private List<int> _selectedPersonPlaceIds = new();
    private List<int> _selectedOfficePlaceIds = new();
    private IReadOnlyList<OfficeQueryRecord> _records = Array.Empty<OfficeQueryRecord>();
    private IReadOnlyList<OfficeQueryPerson> _people = Array.Empty<OfficeQueryPerson>();

    private TextBlock _lblPersonKeyword = null!;
    private TextBox _txtPersonKeyword = null!;
    private TextBlock _lblOfficeSelection = null!;
    private TextBox _txtSelectedOffices = null!;
    private TextBlock _lblPersonPlaceSelection = null!;
    private TextBox _txtSelectedPersonPlaces = null!;
    private Button _btnSelectPersonPlaces = null!;
    private Button _btnClearPersonPlaces = null!;
    private CheckBox _chkIncludePersonSubUnits = null!;
    private TextBlock _lblOfficePlaceSelection = null!;
    private TextBox _txtSelectedOfficePlaces = null!;
    private Button _btnSelectOfficePlaces = null!;
    private Button _btnClearOfficePlaces = null!;
    private CheckBox _chkIncludeOfficeSubUnits = null!;
    private CheckBox _chkUseIndexYear = null!;
    private TextBlock _lblIndexYearTo = null!;
    private TextBox _txtIndexYearFrom = null!;
    private TextBox _txtIndexYearTo = null!;
    private CheckBox _chkUseOfficeYear = null!;
    private TextBlock _lblOfficeYearTo = null!;
    private TextBox _txtOfficeYearFrom = null!;
    private TextBox _txtOfficeYearTo = null!;
    private DynastyRangePicker _dynastyPicker = null!;
    private Button _btnSelectOffices = null!;
    private Button _btnClearOffices = null!;
    private Button _btnRunQuery = null!;
    private Button _btnSavePersonIds = null!;
    private Button _btnSaveOfficeCodes = null!;
    private Button _btnOpenInBrowser = null!;
    private Button _btnClose = null!;
    private TabItem _tabRecords = null!;
    private TabItem _tabPeople = null!;
    private DataGrid _gridRecords = null!;
    private DataGrid _gridPeople = null!;
    private TextBlock _txtStatusBar = null!;

    public OfficeQueryWindow()
        : this(string.Empty, new AppLocalizationService(), new SqliteOfficeQueryService(), new SqlitePlaceLookupService()) {
    }

    public OfficeQueryWindow(string sqlitePath, AppLocalizationService localizationService) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;
        _officeQueryService = new SqliteOfficeQueryService();
        _placeLookupService = new SqlitePlaceLookupService();

        InitializeWindow();
    }

    public OfficeQueryWindow(
        string sqlitePath,
        AppLocalizationService localizationService,
        IOfficeQueryService officeQueryService,
        IPlaceLookupService placeLookupService
    ) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;
        _officeQueryService = officeQueryService;
        _placeLookupService = placeLookupService;

        InitializeWindow();
    }

    private void InitializeWindow() {
        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        _chkUseIndexYear.IsChecked = false;
        _txtIndexYearFrom.Text = "-200";
        _txtIndexYearTo.Text = "1911";
        _chkUseOfficeYear.IsChecked = false;
        _txtOfficeYearFrom.Text = "-200";
        _txtOfficeYearTo.Text = "1911";
        _chkUseIndexYear.IsCheckedChanged += ChkUseIndexYear_IsCheckedChanged;
        _chkUseOfficeYear.IsCheckedChanged += ChkUseOfficeYear_IsCheckedChanged;
        UpdateIndexYearEnabledState();
        UpdateOfficeYearEnabledState();

        ApplyLocalization();
        Opened += OfficeQueryWindow_Opened;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void InitializeControls() {
        _lblPersonKeyword = this.FindControl<TextBlock>("LblPersonKeyword") ?? throw new InvalidOperationException("LblPersonKeyword not found.");
        _txtPersonKeyword = this.FindControl<TextBox>("TxtPersonKeyword") ?? throw new InvalidOperationException("TxtPersonKeyword not found.");
        _lblOfficeSelection = this.FindControl<TextBlock>("LblOfficeSelection") ?? throw new InvalidOperationException("LblOfficeSelection not found.");
        _txtSelectedOffices = this.FindControl<TextBox>("TxtSelectedOffices") ?? throw new InvalidOperationException("TxtSelectedOffices not found.");
        _lblPersonPlaceSelection = this.FindControl<TextBlock>("LblPersonPlaceSelection") ?? throw new InvalidOperationException("LblPersonPlaceSelection not found.");
        _txtSelectedPersonPlaces = this.FindControl<TextBox>("TxtSelectedPersonPlaces") ?? throw new InvalidOperationException("TxtSelectedPersonPlaces not found.");
        _btnSelectPersonPlaces = this.FindControl<Button>("BtnSelectPersonPlaces") ?? throw new InvalidOperationException("BtnSelectPersonPlaces not found.");
        _btnClearPersonPlaces = this.FindControl<Button>("BtnClearPersonPlaces") ?? throw new InvalidOperationException("BtnClearPersonPlaces not found.");
        _chkIncludePersonSubUnits = this.FindControl<CheckBox>("ChkIncludePersonSubUnits") ?? throw new InvalidOperationException("ChkIncludePersonSubUnits not found.");
        _lblOfficePlaceSelection = this.FindControl<TextBlock>("LblOfficePlaceSelection") ?? throw new InvalidOperationException("LblOfficePlaceSelection not found.");
        _txtSelectedOfficePlaces = this.FindControl<TextBox>("TxtSelectedOfficePlaces") ?? throw new InvalidOperationException("TxtSelectedOfficePlaces not found.");
        _btnSelectOfficePlaces = this.FindControl<Button>("BtnSelectOfficePlaces") ?? throw new InvalidOperationException("BtnSelectOfficePlaces not found.");
        _btnClearOfficePlaces = this.FindControl<Button>("BtnClearOfficePlaces") ?? throw new InvalidOperationException("BtnClearOfficePlaces not found.");
        _chkIncludeOfficeSubUnits = this.FindControl<CheckBox>("ChkIncludeOfficeSubUnits") ?? throw new InvalidOperationException("ChkIncludeOfficeSubUnits not found.");
        _chkUseIndexYear = this.FindControl<CheckBox>("ChkUseIndexYear") ?? throw new InvalidOperationException("ChkUseIndexYear not found.");
        _lblIndexYearTo = this.FindControl<TextBlock>("LblIndexYearTo") ?? throw new InvalidOperationException("LblIndexYearTo not found.");
        _txtIndexYearFrom = this.FindControl<TextBox>("TxtIndexYearFrom") ?? throw new InvalidOperationException("TxtIndexYearFrom not found.");
        _txtIndexYearTo = this.FindControl<TextBox>("TxtIndexYearTo") ?? throw new InvalidOperationException("TxtIndexYearTo not found.");
        _chkUseOfficeYear = this.FindControl<CheckBox>("ChkUseOfficeYear") ?? throw new InvalidOperationException("ChkUseOfficeYear not found.");
        _lblOfficeYearTo = this.FindControl<TextBlock>("LblOfficeYearTo") ?? throw new InvalidOperationException("LblOfficeYearTo not found.");
        _txtOfficeYearFrom = this.FindControl<TextBox>("TxtOfficeYearFrom") ?? throw new InvalidOperationException("TxtOfficeYearFrom not found.");
        _txtOfficeYearTo = this.FindControl<TextBox>("TxtOfficeYearTo") ?? throw new InvalidOperationException("TxtOfficeYearTo not found.");
        _dynastyPicker = this.FindControl<DynastyRangePicker>("DynastyPicker") ?? throw new InvalidOperationException("DynastyPicker not found.");
        _btnSelectOffices = this.FindControl<Button>("BtnSelectOffices") ?? throw new InvalidOperationException("BtnSelectOffices not found.");
        _btnClearOffices = this.FindControl<Button>("BtnClearOffices") ?? throw new InvalidOperationException("BtnClearOffices not found.");
        _btnRunQuery = this.FindControl<Button>("BtnRunQuery") ?? throw new InvalidOperationException("BtnRunQuery not found.");
        _btnSavePersonIds = this.FindControl<Button>("BtnSavePersonIds") ?? throw new InvalidOperationException("BtnSavePersonIds not found.");
        _btnSaveOfficeCodes = this.FindControl<Button>("BtnSaveOfficeCodes") ?? throw new InvalidOperationException("BtnSaveOfficeCodes not found.");
        _btnOpenInBrowser = this.FindControl<Button>("BtnOpenInBrowser") ?? throw new InvalidOperationException("BtnOpenInBrowser not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
        _tabRecords = this.FindControl<TabItem>("TabRecords") ?? throw new InvalidOperationException("TabRecords not found.");
        _tabPeople = this.FindControl<TabItem>("TabPeople") ?? throw new InvalidOperationException("TabPeople not found.");
        _gridRecords = this.FindControl<DataGrid>("GridRecords") ?? throw new InvalidOperationException("GridRecords not found.");
        _gridPeople = this.FindControl<DataGrid>("GridPeople") ?? throw new InvalidOperationException("GridPeople not found.");
        _txtStatusBar = this.FindControl<TextBlock>("TxtStatusBar") ?? throw new InvalidOperationException("TxtStatusBar not found.");
    }

    private async void OfficeQueryWindow_Opened(object? sender, EventArgs e) {
        Opened -= OfficeQueryWindow_Opened;
        await LoadFiltersAsync();
    }

    private async Task LoadFiltersAsync() {
        try {
            _officePickerData = await _officeQueryService.GetOfficePickerDataAsync(_sqlitePath);
            _officeOptions = _officePickerData.AllOfficeCodes;
            _placeOptions = await _placeLookupService.GetPlacesAsync(_sqlitePath);
            await _dynastyPicker.LoadDynastiesAsync(_sqlitePath);
            UpdateSelectedOfficesText();
            UpdateSelectedPersonPlacesText();
            UpdateSelectedOfficePlacesText();
            _txtStatusBar.Text = string.Format(T("office_query.loaded_filters"), _officeOptions.Count, _dynastyPicker.OptionCount);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private void ApplyLocalization() {
        Title = T("office_query.title");
        _lblPersonKeyword.Text = T("office_query.person_keyword");
        _lblOfficeSelection.Text = T("office_query.selected_offices");
        _lblPersonPlaceSelection.Text = T("office_query.selected_person_places");
        _btnSelectPersonPlaces.Content = T("office_query.select_person_places");
        _btnClearPersonPlaces.Content = T("office_query.clear_person_places");
        _chkIncludePersonSubUnits.Content = T("office_query.include_person_subunits");
        _lblOfficePlaceSelection.Text = T("office_query.selected_office_places");
        _btnSelectOfficePlaces.Content = T("office_query.select_office_places");
        _btnClearOfficePlaces.Content = T("office_query.clear_office_places");
        _chkIncludeOfficeSubUnits.Content = T("office_query.include_office_subunits");
        _chkUseIndexYear.Content = T("office_query.use_index_year");
        _lblIndexYearTo.Text = T("office_query.to");
        _chkUseOfficeYear.Content = T("office_query.use_office_year");
        _lblOfficeYearTo.Text = T("office_query.to");
        _btnSelectOffices.Content = T("office_query.select_offices");
        _btnClearOffices.Content = T("office_query.clear_offices");
        _btnRunQuery.Content = T("office_query.run_query");
        _btnSavePersonIds.Content = T("office_query.save_person_ids");
        _btnSaveOfficeCodes.Content = T("office_query.save_office_codes");
        _btnOpenInBrowser.Content = T("query.open_in_browser");
        _btnClose.Content = T("button.exit");
        _tabRecords.Header = T("office_query.tab_records");
        _tabPeople.Header = T("office_query.tab_people");
        _dynastyPicker.Configure(_localizationService);

        var rc = _gridRecords.Columns;
        ((DataGridTextColumn)rc[0]).Header = T("browser.posting_id");
        ((DataGridTextColumn)rc[1]).Header = T("browser.grid_person_id");
        ((DataGridTextColumn)rc[2]).Header = T("browser.grid_name_chn");
        ((DataGridTextColumn)rc[3]).Header = T("browser.name");
        ((DataGridTextColumn)rc[4]).Header = T("browser.grid_index_year");
        ((DataGridTextColumn)rc[5]).Header = T("browser.index_year_type");
        ((DataGridTextColumn)rc[6]).Header = T("browser.gender");
        ((DataGridTextColumn)rc[7]).Header = T("browser.dynasty");
        ((DataGridTextColumn)rc[8]).Header = T("office_query.posting_dynasty");
        ((DataGridTextColumn)rc[9]).Header = T("browser.index_address_type");
        ((DataGridTextColumn)rc[10]).Header = T("browser.address_sequence");
        ((DataGridTextColumn)rc[11]).Header = T("browser.posting_office");
        ((DataGridTextColumn)rc[12]).Header = T("office_query.office_code");
        ((DataGridTextColumn)rc[13]).Header = T("office_query.appointment_code");
        ((DataGridTextColumn)rc[14]).Header = T("browser.posting_appointment");
        ((DataGridTextColumn)rc[15]).Header = T("office_query.assume_office_code");
        ((DataGridTextColumn)rc[16]).Header = T("browser.posting_assume_office");
        ((DataGridTextColumn)rc[17]).Header = T("office_query.office_category_id");
        ((DataGridTextColumn)rc[18]).Header = T("browser.posting_category");
        ((DataGridTextColumn)rc[19]).Header = T("office_query.first_year");
        ((DataGridTextColumn)rc[20]).Header = T("office_query.nianhao_code");
        ((DataGridTextColumn)rc[21]).Header = T("office_query.first_nianhao");
        ((DataGridTextColumn)rc[22]).Header = T("office_query.nianhao_pinyin");
        ((DataGridTextColumn)rc[23]).Header = T("office_query.first_nianhao_year");
        ((DataGridTextColumn)rc[24]).Header = T("office_query.range_code");
        ((DataGridTextColumn)rc[25]).Header = T("office_query.range_desc");
        ((DataGridTextColumn)rc[26]).Header = T("office_query.first_range");
        ((DataGridTextColumn)rc[27]).Header = T("office_query.first_month");
        ((DataGridTextColumn)rc[28]).Header = T("office_query.first_intercalary");
        ((DataGridTextColumn)rc[29]).Header = T("office_query.first_day");
        ((DataGridTextColumn)rc[30]).Header = T("office_query.ganzhi_code");
        ((DataGridTextColumn)rc[31]).Header = T("office_query.first_day_ganzhi");
        ((DataGridTextColumn)rc[32]).Header = T("office_query.ganzhi_pinyin");
        ((DataGridTextColumn)rc[33]).Header = T("office_query.last_year");
        ((DataGridTextColumn)rc[34]).Header = T("office_query.nianhao_code");
        ((DataGridTextColumn)rc[35]).Header = T("office_query.last_nianhao");
        ((DataGridTextColumn)rc[36]).Header = T("office_query.nianhao_pinyin");
        ((DataGridTextColumn)rc[37]).Header = T("office_query.last_nianhao_year");
        ((DataGridTextColumn)rc[38]).Header = T("office_query.range_code");
        ((DataGridTextColumn)rc[39]).Header = T("office_query.range_desc");
        ((DataGridTextColumn)rc[40]).Header = T("office_query.last_range");
        ((DataGridTextColumn)rc[41]).Header = T("office_query.last_month");
        ((DataGridTextColumn)rc[42]).Header = T("office_query.last_intercalary");
        ((DataGridTextColumn)rc[43]).Header = T("office_query.last_day");
        ((DataGridTextColumn)rc[44]).Header = T("office_query.ganzhi_code");
        ((DataGridTextColumn)rc[45]).Header = T("office_query.last_day_ganzhi");
        ((DataGridTextColumn)rc[46]).Header = T("office_query.ganzhi_pinyin");
        ((DataGridTextColumn)rc[47]).Header = T("office_query.institution_code");
        ((DataGridTextColumn)rc[48]).Header = T("office_query.institution_name_code");
        ((DataGridTextColumn)rc[49]).Header = T("browser.entry_institution");
        ((DataGridTextColumn)rc[50]).Header = T("browser.address_id");
        ((DataGridTextColumn)rc[51]).Header = T("browser.posting_addresses");
        ((DataGridTextColumn)rc[52]).Header = "X";
        ((DataGridTextColumn)rc[53]).Header = "Y";
        ((DataGridTextColumn)rc[54]).Header = T("query.people_at_place");
        ((DataGridTextColumn)rc[55]).Header = T("office_query.source_id");
        ((DataGridTextColumn)rc[56]).Header = T("browser.source_title");
        ((DataGridTextColumn)rc[57]).Header = T("browser.address_pages");
        ((DataGridTextColumn)rc[58]).Header = T("browser.notes");

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
        ((DataGridTextColumn)pc[11]).Header = T("browser.posting_addresses");
        ((DataGridTextColumn)pc[12]).Header = "X";
        ((DataGridTextColumn)pc[13]).Header = "Y";
        ((DataGridTextColumn)pc[14]).Header = T("query.people_at_place");
        ((DataGridTextColumn)pc[15]).Header = T("office_query.posting_count");

        UpdateSelectedOfficesText();
        UpdateSelectedPersonPlacesText();
        UpdateSelectedOfficePlacesText();
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateStatusBarCounts();
    }

    private void ChkUseIndexYear_IsCheckedChanged(object? sender, RoutedEventArgs e) => UpdateIndexYearEnabledState();

    private void ChkUseOfficeYear_IsCheckedChanged(object? sender, RoutedEventArgs e) => UpdateOfficeYearEnabledState();

    private async void BtnSelectOffices_Click(object? sender, RoutedEventArgs e) {
        if (_officeOptions.Count == 0) {
            await LoadFiltersAsync();
        }

        var picker = new OfficeCodePickerWindow(_localizationService, _officePickerData, _selectedOfficeCodes);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedOfficeCodes = picker.SelectedCodes.ToList();
            UpdateSelectedOfficesText();
        }
    }

    private void BtnClearOffices_Click(object? sender, RoutedEventArgs e) {
        _selectedOfficeCodes.Clear();
        UpdateSelectedOfficesText();
    }

    private async void BtnSelectPersonPlaces_Click(object? sender, RoutedEventArgs e) {
        if (_placeOptions.Count == 0) {
            await LoadFiltersAsync();
        }

        var picker = new PlacePickerWindow(_localizationService, _placeOptions, _selectedPersonPlaceIds);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedPersonPlaceIds = picker.SelectedPlaceIds.ToList();
            UpdateSelectedPersonPlacesText();
        }
    }

    private void BtnClearPersonPlaces_Click(object? sender, RoutedEventArgs e) {
        _selectedPersonPlaceIds.Clear();
        UpdateSelectedPersonPlacesText();
    }

    private async void BtnSelectOfficePlaces_Click(object? sender, RoutedEventArgs e) {
        if (_placeOptions.Count == 0) {
            await LoadFiltersAsync();
        }

        var picker = new PlacePickerWindow(_localizationService, _placeOptions, _selectedOfficePlaceIds);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedOfficePlaceIds = picker.SelectedPlaceIds.ToList();
            UpdateSelectedOfficePlacesText();
        }
    }

    private void BtnClearOfficePlaces_Click(object? sender, RoutedEventArgs e) {
        _selectedOfficePlaceIds.Clear();
        UpdateSelectedOfficePlacesText();
    }

    private async void BtnRunQuery_Click(object? sender, RoutedEventArgs e) {
        try {
            _btnRunQuery.IsEnabled = false;
            _txtStatusBar.Text = T("office_query.running");

            var result = await _officeQueryService.QueryAsync(_sqlitePath, BuildRequest());
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
            Title = T("office_query.save_person_ids"),
            SuggestedFileName = $"cbdb_office_person_ids_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            FileTypeChoices = new List<FilePickerFileType> { new("Text files") { Patterns = new[] { "*.txt" } } }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            await File.WriteAllLinesAsync(path, _people.Select(person => person.PersonId.ToString()));
            _txtStatusBar.Text = string.Format(T("office_query.saved_person_ids"), _people.Count, path);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private async void BtnSaveOfficeCodes_Click(object? sender, RoutedEventArgs e) {
        if (_selectedOfficeCodes.Count == 0) {
            _txtStatusBar.Text = T("office_query.no_office_codes_to_export");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = T("office_query.save_office_codes"),
            SuggestedFileName = $"cbdb_office_codes_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = new List<FilePickerFileType> { new("CSV files") { Patterns = new[] { "*.csv" } } }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var selectedOptions = _officeOptions.Where(option => _selectedOfficeCodes.Contains(option.Code)).ToList();
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", new[] {
                EscapeCsv(T("office_query.office_code")),
                EscapeCsv(T("browser.posting_office")),
                EscapeCsv(T("browser.dynasty"))
            }));

            foreach (var option in selectedOptions) {
                builder.AppendLine(string.Join(",", new[] {
                    EscapeCsv(option.Code),
                    EscapeCsv(option.DescriptionChn),
                    EscapeCsv(option.DynastyChn)
                }));
            }

            await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(true));
            _txtStatusBar.Text = string.Format(T("office_query.saved_office_codes"), selectedOptions.Count, path);
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

    private OfficeQueryRequest BuildRequest() {
        var (selectedFrom, selectedTo) = _dynastyPicker.GetNormalizedRange();

        return new OfficeQueryRequest(
            PersonKeyword: NormalizeText(_txtPersonKeyword.Text),
            OfficeCodes: _selectedOfficeCodes,
            PersonPlaceIds: _selectedPersonPlaceIds,
            IncludeSubordinatePersonUnits: _chkIncludePersonSubUnits.IsChecked == true,
            OfficePlaceIds: _selectedOfficePlaceIds,
            IncludeSubordinateOfficeUnits: _chkIncludeOfficeSubUnits.IsChecked == true,
            UseIndexYearRange: _chkUseIndexYear.IsChecked == true,
            IndexYearFrom: ParseInt(_txtIndexYearFrom.Text, -200),
            IndexYearTo: ParseInt(_txtIndexYearTo.Text, 1911),
            UseOfficeYearRange: _chkUseOfficeYear.IsChecked == true,
            OfficeYearFrom: ParseInt(_txtOfficeYearFrom.Text, -200),
            OfficeYearTo: ParseInt(_txtOfficeYearTo.Text, 1911),
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

    private void UpdateOfficeYearEnabledState() {
        var isEnabled = _chkUseOfficeYear.IsChecked == true;
        _txtOfficeYearFrom.IsEnabled = isEnabled;
        _txtOfficeYearTo.IsEnabled = isEnabled;
        _lblOfficeYearTo.IsEnabled = isEnabled;
    }

    private void UpdateSelectedOfficesText() {
        if (_selectedOfficeCodes.Count == 0) {
            _txtSelectedOffices.Text = T("office_query.all_offices");
            return;
        }

        var selectedOptions = _officeOptions
            .Where(option => _selectedOfficeCodes.Contains(option.Code))
            .ToList();

        if (selectedOptions.Count == 1) {
            _txtSelectedOffices.Text = selectedOptions[0].DisplayLabel;
            return;
        }

        if (selectedOptions.Count == _officeOptions.Count && _officeOptions.Count > 0) {
            _txtSelectedOffices.Text = T("office_query.all_offices");
            return;
        }

        var matchingType = FindMatchingOfficeType(_officePickerData.Root, _selectedOfficeCodes);
        if (matchingType is not null) {
            _txtSelectedOffices.Text = GetOfficeTypeDisplayLabel(matchingType);
            return;
        }

        _txtSelectedOffices.Text = T("office_query.multi_select");
    }

    private void UpdateSelectedPersonPlacesText() {
        _txtSelectedPersonPlaces.Text = FormatSelectedPlaces(_selectedPersonPlaceIds, T("office_query.all_person_places"));
    }

    private void UpdateSelectedOfficePlacesText() {
        _txtSelectedOfficePlaces.Text = FormatSelectedPlaces(_selectedOfficePlaceIds, T("office_query.all_office_places"));
    }

    private string FormatSelectedPlaces(IReadOnlyCollection<int> placeIds, string defaultLabel) {
        if (placeIds.Count == 0) {
            return defaultLabel;
        }

        var labels = _placeOptions
            .Where(option => placeIds.Contains(option.AddressId))
            .Select(option => option.DisplayLabel)
            .Take(3)
            .ToList();

        var suffix = placeIds.Count > labels.Count
            ? string.Format(T("office_query.more_selected"), placeIds.Count - labels.Count)
            : string.Empty;

        return string.Join("; ", labels) + suffix;
    }

    private void UpdateStatusBarCounts() {
        _txtStatusBar.Text = string.Format(T("office_query.results_summary"), _records.Count, _people.Count);
    }

    private static int ParseInt(string? value, int fallback) => int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;

    private static OfficeTypeNode? FindMatchingOfficeType(OfficeTypeNode node, IReadOnlyCollection<string> selectedCodes) {
        foreach (var child in node.Children) {
            var match = FindMatchingOfficeType(child, selectedCodes);
            if (match is not null) {
                return match;
            }
        }

        return node.IsRoot || node.OfficeCodes.Count == 0 || !node.OfficeCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).SequenceEqual(selectedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
            ? null
            : node;
    }

    private static string GetOfficeTypeDisplayLabel(OfficeTypeNode node) {
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
