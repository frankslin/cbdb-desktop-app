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

public partial class StatusQueryWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly IStatusQueryService _statusQueryService = new SqliteStatusQueryService();
    private readonly string _sqlitePath;

    private IReadOnlyList<StatusCodeOption> _statusOptions = Array.Empty<StatusCodeOption>();
    private List<string> _selectedStatusCodes = new();
    private IReadOnlyList<StatusQueryRecord> _records = Array.Empty<StatusQueryRecord>();
    private IReadOnlyList<StatusQueryPerson> _people = Array.Empty<StatusQueryPerson>();

    private TextBlock _lblPersonKeyword = null!;
    private TextBox _txtPersonKeyword = null!;
    private TextBlock _lblStatusSelection = null!;
    private TextBox _txtSelectedStatuses = null!;
    private CheckBox _chkUseIndexYear = null!;
    private TextBlock _lblIndexYearTo = null!;
    private TextBox _txtIndexYearFrom = null!;
    private TextBox _txtIndexYearTo = null!;
    private DynastyRangePicker _dynastyPicker = null!;
    private Button _btnSelectStatuses = null!;
    private Button _btnClearStatuses = null!;
    private Button _btnRunQuery = null!;
    private Button _btnSavePersonIds = null!;
    private Button _btnClose = null!;
    private TabItem _tabRecords = null!;
    private TabItem _tabPeople = null!;
    private DataGrid _gridRecords = null!;
    private DataGrid _gridPeople = null!;
    private TextBlock _txtStatusBar = null!;

    public StatusQueryWindow()
        : this(string.Empty, new AppLocalizationService()) {
    }

    public StatusQueryWindow(string sqlitePath, AppLocalizationService localizationService) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        _chkUseIndexYear.IsChecked = true;
        _txtIndexYearFrom.Text = "-200";
        _txtIndexYearTo.Text = "1911";

        ApplyLocalization();
        Opened += StatusQueryWindow_Opened;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _lblPersonKeyword = this.FindControl<TextBlock>("LblPersonKeyword") ?? throw new InvalidOperationException("LblPersonKeyword not found.");
        _txtPersonKeyword = this.FindControl<TextBox>("TxtPersonKeyword") ?? throw new InvalidOperationException("TxtPersonKeyword not found.");
        _lblStatusSelection = this.FindControl<TextBlock>("LblStatusSelection") ?? throw new InvalidOperationException("LblStatusSelection not found.");
        _txtSelectedStatuses = this.FindControl<TextBox>("TxtSelectedStatuses") ?? throw new InvalidOperationException("TxtSelectedStatuses not found.");
        _chkUseIndexYear = this.FindControl<CheckBox>("ChkUseIndexYear") ?? throw new InvalidOperationException("ChkUseIndexYear not found.");
        _lblIndexYearTo = this.FindControl<TextBlock>("LblIndexYearTo") ?? throw new InvalidOperationException("LblIndexYearTo not found.");
        _txtIndexYearFrom = this.FindControl<TextBox>("TxtIndexYearFrom") ?? throw new InvalidOperationException("TxtIndexYearFrom not found.");
        _txtIndexYearTo = this.FindControl<TextBox>("TxtIndexYearTo") ?? throw new InvalidOperationException("TxtIndexYearTo not found.");
        _dynastyPicker = this.FindControl<DynastyRangePicker>("DynastyPicker") ?? throw new InvalidOperationException("DynastyPicker not found.");
        _btnSelectStatuses = this.FindControl<Button>("BtnSelectStatuses") ?? throw new InvalidOperationException("BtnSelectStatuses not found.");
        _btnClearStatuses = this.FindControl<Button>("BtnClearStatuses") ?? throw new InvalidOperationException("BtnClearStatuses not found.");
        _btnRunQuery = this.FindControl<Button>("BtnRunQuery") ?? throw new InvalidOperationException("BtnRunQuery not found.");
        _btnSavePersonIds = this.FindControl<Button>("BtnSavePersonIds") ?? throw new InvalidOperationException("BtnSavePersonIds not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
        _tabRecords = this.FindControl<TabItem>("TabRecords") ?? throw new InvalidOperationException("TabRecords not found.");
        _tabPeople = this.FindControl<TabItem>("TabPeople") ?? throw new InvalidOperationException("TabPeople not found.");
        _gridRecords = this.FindControl<DataGrid>("GridRecords") ?? throw new InvalidOperationException("GridRecords not found.");
        _gridPeople = this.FindControl<DataGrid>("GridPeople") ?? throw new InvalidOperationException("GridPeople not found.");
        _txtStatusBar = this.FindControl<TextBlock>("TxtStatusBar") ?? throw new InvalidOperationException("TxtStatusBar not found.");
    }

    private async void StatusQueryWindow_Opened(object? sender, EventArgs e) {
        Opened -= StatusQueryWindow_Opened;
        await LoadStatusOptionsAsync();
    }

    private async Task LoadStatusOptionsAsync() {
        try {
            _statusOptions = await _statusQueryService.GetStatusCodesAsync(_sqlitePath);
            await _dynastyPicker.LoadDynastiesAsync(_sqlitePath);
            UpdateSelectedStatusesText();
            _txtStatusBar.Text = string.Format(T("status_query.loaded_filters"), _statusOptions.Count, _dynastyPicker.OptionCount);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private void ApplyLocalization() {
        Title = T("status_query.title");
        _lblPersonKeyword.Text = T("status_query.person_keyword");
        _lblStatusSelection.Text = T("status_query.selected_statuses");
        _chkUseIndexYear.Content = T("status_query.use_index_year");
        _lblIndexYearTo.Text = T("status_query.to");
        _btnSelectStatuses.Content = T("status_query.select_statuses");
        _btnClearStatuses.Content = T("status_query.clear_statuses");
        _btnRunQuery.Content = T("status_query.run_query");
        _btnSavePersonIds.Content = T("status_query.save_person_ids");
        _btnClose.Content = T("button.exit");
        _tabRecords.Header = T("status_query.tab_records");
        _tabPeople.Header = T("status_query.tab_people");
        _dynastyPicker.Configure(_localizationService);

        ((DataGridTextColumn)_gridRecords.Columns[0]).Header = T("browser.grid_person_id");
        ((DataGridTextColumn)_gridRecords.Columns[1]).Header = T("browser.grid_name_chn");
        ((DataGridTextColumn)_gridRecords.Columns[2]).Header = T("browser.name");
        ((DataGridTextColumn)_gridRecords.Columns[3]).Header = T("browser.grid_index_year");
        ((DataGridTextColumn)_gridRecords.Columns[4]).Header = T("browser.dynasty");
        ((DataGridTextColumn)_gridRecords.Columns[5]).Header = T("browser.status_name");
        ((DataGridTextColumn)_gridRecords.Columns[6]).Header = T("status_query.status_code");
        ((DataGridTextColumn)_gridRecords.Columns[7]).Header = T("status_query.first_year");
        ((DataGridTextColumn)_gridRecords.Columns[8]).Header = T("status_query.last_year");
        ((DataGridTextColumn)_gridRecords.Columns[9]).Header = T("browser.source_title");
        ((DataGridTextColumn)_gridRecords.Columns[10]).Header = T("browser.address_pages");
        ((DataGridTextColumn)_gridRecords.Columns[11]).Header = T("browser.grid_index_address");

        ((DataGridTextColumn)_gridPeople.Columns[0]).Header = T("browser.grid_person_id");
        ((DataGridTextColumn)_gridPeople.Columns[1]).Header = T("browser.grid_name_chn");
        ((DataGridTextColumn)_gridPeople.Columns[2]).Header = T("browser.name");
        ((DataGridTextColumn)_gridPeople.Columns[3]).Header = T("browser.grid_index_year");
        ((DataGridTextColumn)_gridPeople.Columns[4]).Header = T("browser.dynasty");
        ((DataGridTextColumn)_gridPeople.Columns[5]).Header = T("browser.grid_index_address");
        ((DataGridTextColumn)_gridPeople.Columns[6]).Header = T("status_query.status_count");

        UpdateSelectedStatusesText();
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateStatusBarCounts();
    }

    private async void BtnSelectStatuses_Click(object? sender, RoutedEventArgs e) {
        if (_statusOptions.Count == 0) {
            await LoadStatusOptionsAsync();
        }

        var picker = new StatusCodePickerWindow(_localizationService, _statusOptions, _selectedStatusCodes);
        var result = await picker.ShowDialog<bool?>(this);
        if (result == true) {
            _selectedStatusCodes = picker.SelectedCodes.ToList();
            UpdateSelectedStatusesText();
        }
    }

    private void BtnClearStatuses_Click(object? sender, RoutedEventArgs e) {
        _selectedStatusCodes.Clear();
        UpdateSelectedStatusesText();
    }

    private async void BtnRunQuery_Click(object? sender, RoutedEventArgs e) {
        try {
            _btnRunQuery.IsEnabled = false;
            _txtStatusBar.Text = T("status_query.running");

            var result = await _statusQueryService.QueryAsync(_sqlitePath, BuildRequest());
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
            Title = T("status_query.save_person_ids"),
            SuggestedFileName = $"cbdb_status_person_ids_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            FileTypeChoices = new List<FilePickerFileType> {
                new("Text files") {
                    Patterns = new[] { "*.txt" }
                }
            }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var builder = new StringBuilder();
            foreach (var person in _people) {
                builder.AppendLine(person.PersonId.ToString());
            }

            await File.WriteAllTextAsync(path, builder.ToString());
            _txtStatusBar.Text = string.Format(T("status_query.saved_person_ids"), _people.Count, path);
        } catch (Exception ex) {
            _txtStatusBar.Text = ex.Message;
        }
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private StatusQueryRequest BuildRequest() {
        var (selectedFrom, selectedTo) = _dynastyPicker.GetNormalizedRange();

        return new StatusQueryRequest(
            PersonKeyword: NormalizeText(_txtPersonKeyword.Text),
            StatusCodes: _selectedStatusCodes,
            UseIndexYearRange: _chkUseIndexYear.IsChecked == true,
            IndexYearFrom: ParseInt(_txtIndexYearFrom.Text, -200),
            IndexYearTo: ParseInt(_txtIndexYearTo.Text, 1911),
            UseDynastyRange: _dynastyPicker.UseDynastyRange,
            DynastyFrom: selectedFrom,
            DynastyTo: selectedTo
        );
    }

    private void UpdateSelectedStatusesText() {
        if (_selectedStatusCodes.Count == 0) {
            _txtSelectedStatuses.Text = T("status_query.all_statuses");
            return;
        }

        var labels = _statusOptions
            .Where(option => _selectedStatusCodes.Contains(option.Code))
            .Select(option => option.DisplayLabel)
            .Take(3)
            .ToList();

        var suffix = _selectedStatusCodes.Count > labels.Count
            ? string.Format(T("status_query.more_selected"), _selectedStatusCodes.Count - labels.Count)
            : string.Empty;

        _txtSelectedStatuses.Text = string.Join("; ", labels) + suffix;
    }

    private void UpdateStatusBarCounts() {
        _txtStatusBar.Text = string.Format(T("status_query.results_summary"), _records.Count, _people.Count);
    }

    private static int ParseInt(string? value, int fallback) {
        return int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;
    }

    private static string? NormalizeText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string T(string key) => _localizationService.Get(key);
}
