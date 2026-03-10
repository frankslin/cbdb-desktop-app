using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Browser;

public partial class PersonBrowserWindow : Window {
    private const int PageSize = 300;

    private readonly AppLocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService = new SqlitePersonBrowserService();
    private readonly ObservableCollection<PersonListItem> _people = new();
    private readonly ObservableCollection<PersonFieldValue> _detailFields = new();
    private readonly string _sqlitePath;

    private TextBox _txtKeyword = null!;
    private Button _btnSearch = null!;
    private Button _btnClear = null!;
    private Button _btnSaveToFile = null!;
    private TextBlock _lblSearchByName = null!;
    private TextBlock _hdrPersonId = null!;
    private TextBlock _hdrNameChn = null!;
    private TextBlock _hdrNameRm = null!;
    private TextBlock _hdrIndexYear = null!;
    private TextBlock _hdrIndexAddress = null!;
    private ListBox _listPeople = null!;
    private TextBlock _txtRecord = null!;
    private TextBlock _lblPersonId = null!;
    private TextBlock _lblNameChn = null!;
    private TextBlock _lblName = null!;
    private TextBlock _lblGender = null!;
    private TextBlock _lblBirthYear = null!;
    private TextBlock _lblDeathYear = null!;
    private TextBlock _lblDynasty = null!;
    private TextBlock _lblIndexYear = null!;
    private TextBlock _lblIndexYearType = null!;
    private TextBlock _lblIndexYearSource = null!;
    private TextBlock _lblIndexAddress = null!;
    private TextBlock _lblIndexAddressType = null!;
    private TextBox _valPersonId = null!;
    private TextBox _valNameChn = null!;
    private TextBox _valName = null!;
    private TextBox _valGender = null!;
    private TextBox _valBirthYear = null!;
    private TextBox _valDeathYear = null!;
    private TextBox _valDynasty = null!;
    private TextBox _valIndexYear = null!;
    private TextBox _valIndexYearType = null!;
    private TextBox _valIndexYearSource = null!;
    private TextBox _valIndexAddress = null!;
    private TextBox _valIndexAddressType = null!;
    private TabItem _tabBasic = null!;
    private TabItem _tabRelated = null!;
    private TextBlock _lblAllFieldsHeader = null!;
    private TextBlock _txtNoSelection = null!;
    private ItemsControl _itemsBasicFields = null!;
    private TextBlock _txtRelatedCounts = null!;
    private TextBlock _txtRelatedCountsMore = null!;
    private TextBlock _txtRelatedCountsTail = null!;
    private TextBlock _txtRelatedPlaceholder = null!;
    private TextBlock _txtFooter = null!;

    private int? _selectedPersonId;
    private PersonDetail? _currentDetail;

    public PersonBrowserWindow() : this(string.Empty, new AppLocalizationService()) {
    }

    public PersonBrowserWindow(string sqlitePath, AppLocalizationService localizationService) {
        _sqlitePath = NormalizeSqlitePath(sqlitePath);
        _localizationService = localizationService;

        InitializeComponent();
        InitializeControls();

        _listPeople.ItemsSource = _people;
        _itemsBasicFields.ItemsSource = _detailFields;

        _localizationService.LanguageChanged += OnLanguageChanged;
        ApplyLocalization();

        Opened += async (_, _) => await SearchAsync();
        Closed += (_, _) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateRecordText();
        UpdateRelatedSummary();
    }

    private void ApplyLocalization() {
        Title = T("browser.window_title");
        _btnSearch.Content = T("browser.search");
        _btnClear.Content = T("browser.clear");
        _btnSaveToFile.Content = T("browser.save_to_file");
        _lblSearchByName.Text = T("browser.keyword_tooltip");
        _txtKeyword.Watermark = T("browser.keyword_tooltip");

        _hdrPersonId.Text = T("browser.grid_person_id");
        _hdrNameChn.Text = T("browser.grid_name_chn");
        _hdrNameRm.Text = T("browser.grid_name_rm");
        _hdrIndexYear.Text = T("browser.grid_index_year");
        _hdrIndexAddress.Text = T("browser.grid_index_address");

        _lblPersonId.Text = T("browser.person_id");
        _lblNameChn.Text = T("browser.name_chn");
        _lblName.Text = T("browser.name");
        _lblGender.Text = T("browser.gender");
        _lblBirthYear.Text = T("browser.birth_year");
        _lblDeathYear.Text = T("browser.death_year");
        _lblDynasty.Text = T("browser.dynasty");
        _lblIndexYear.Text = T("browser.index_year");
        _lblIndexYearType.Text = T("browser.index_year_type");
        _lblIndexYearSource.Text = T("browser.index_year_source");
        _lblIndexAddress.Text = T("browser.index_address");
        _lblIndexAddressType.Text = T("browser.index_address_type");

        _tabBasic.Header = T("browser.tab_basic");
        _tabRelated.Header = T("browser.tab_related");
        _lblAllFieldsHeader.Text = T("browser.all_fields_header");
        _txtNoSelection.Text = _currentDetail is null ? T("browser.no_selection") : string.Empty;
        _txtRelatedPlaceholder.Text = T("browser.related_placeholder");

        if (string.IsNullOrWhiteSpace(_txtFooter.Text) || _txtFooter.Text == "Ready" || _txtFooter.Text == "就緒" || _txtFooter.Text == "就绪") {
            _txtFooter.Text = T("status.ready");
        }
    }

    private async void BtnSearch_Click(object? sender, RoutedEventArgs e) {
        await SearchAsync();
    }

    private async void BtnClear_Click(object? sender, RoutedEventArgs e) {
        _txtKeyword.Text = string.Empty;
        await SearchAsync();
    }

    private async void TxtKeyword_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter) {
            return;
        }

        e.Handled = true;
        await SearchAsync();
    }

    private async Task SearchAsync() {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _people.Clear();
            ClearDetail();
            _txtFooter.Text = $"{T("msg.sqlite_missing")}: {_sqlitePath}";
            return;
        }

        try {
            _btnSearch.IsEnabled = false;
            _btnClear.IsEnabled = false;
            _txtFooter.Text = T("status.checking");

            var keyword = string.IsNullOrWhiteSpace(_txtKeyword.Text) ? null : _txtKeyword.Text.Trim();
            var rows = await _personBrowserService.SearchAsync(_sqlitePath, keyword, PageSize, 0);

            _people.Clear();
            foreach (var row in rows) {
                _people.Add(row);
            }

            UpdateRecordText();

            if (_people.Count > 0) {
                _listPeople.SelectedIndex = 0;
            } else {
                ClearDetail();
            }
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        } finally {
            _btnSearch.IsEnabled = true;
            _btnClear.IsEnabled = true;
        }
    }

    private async void ListPeople_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_listPeople.SelectedItem is not PersonListItem selected) {
            ClearDetail();
            return;
        }

        _selectedPersonId = selected.PersonId;
        _txtFooter.Text = T("status.checking");

        try {
            var detail = await _personBrowserService.GetDetailAsync(_sqlitePath, selected.PersonId);
            if (detail is null) {
                ClearDetail();
                return;
            }

            _currentDetail = detail;
            _valPersonId.Text = detail.PersonId.ToString();
            _valNameChn.Text = detail.NameChn ?? string.Empty;
            _valName.Text = JoinDisplay(detail.Name, BuildJoinedName(detail.SurnameRm, detail.MingziRm));
            _valGender.Text = detail.Gender switch {
                "M" => T("browser.male"),
                "F" => T("browser.female"),
                _ => T("browser.unknown")
            };
            _valBirthYear.Text = detail.BirthYear?.ToString() ?? string.Empty;
            _valDeathYear.Text = detail.DeathYear?.ToString() ?? string.Empty;
            _valDynasty.Text = JoinDisplay(detail.DynastyChn, detail.Dynasty);
            _valIndexYear.Text = detail.IndexYear?.ToString() ?? string.Empty;
            _valIndexYearType.Text = detail.IndexYearType ?? string.Empty;
            _valIndexYearSource.Text = detail.IndexYearSource ?? string.Empty;
            _valIndexAddress.Text = JoinDisplay(detail.IndexAddressChn, detail.IndexAddress);
            _valIndexAddressType.Text = detail.IndexAddressType ?? string.Empty;

            _detailFields.Clear();
            foreach (var field in detail.Fields) {
                _detailFields.Add(field);
            }

            _txtNoSelection.Text = string.Empty;
            UpdateRelatedSummary();
            _txtFooter.Text = string.Format(T("browser.search_result_count"), _people.Count);
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        }
    }

    private async void BtnSaveToFile_Click(object? sender, RoutedEventArgs e) {
        if (_people.Count == 0) {
            _txtFooter.Text = T("browser.no_data_to_export");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = T("browser.save_to_file"),
            SuggestedFileName = $"cbdb_people_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = new List<FilePickerFileType> {
                new("CSV files") {
                    Patterns = new[] { "*.csv" }
                }
            }
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", new[] {
                EscapeCsv(T("browser.grid_person_id")),
                EscapeCsv(T("browser.grid_name_chn")),
                EscapeCsv(T("browser.grid_name_rm")),
                EscapeCsv(T("browser.grid_index_year")),
                EscapeCsv(T("browser.grid_index_address"))
            }));

            foreach (var person in _people) {
                builder.AppendLine(string.Join(",", new[] {
                    EscapeCsv(person.PersonId.ToString()),
                    EscapeCsv(person.NameChn),
                    EscapeCsv(person.NameRm),
                    EscapeCsv(person.IndexYear?.ToString()),
                    EscapeCsv(person.IndexAddress)
                }));
            }

            await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(true));
            _txtFooter.Text = path;
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        }
    }

    private void ClearDetail() {
        _selectedPersonId = null;
        _currentDetail = null;

        _valPersonId.Text = string.Empty;
        _valNameChn.Text = string.Empty;
        _valName.Text = string.Empty;
        _valGender.Text = string.Empty;
        _valBirthYear.Text = string.Empty;
        _valDeathYear.Text = string.Empty;
        _valDynasty.Text = string.Empty;
        _valIndexYear.Text = string.Empty;
        _valIndexYearType.Text = string.Empty;
        _valIndexYearSource.Text = string.Empty;
        _valIndexAddress.Text = string.Empty;
        _valIndexAddressType.Text = string.Empty;

        _detailFields.Clear();
        _txtNoSelection.Text = T("browser.no_selection");
        UpdateRelatedSummary();
        UpdateRecordText();
    }

    private void UpdateRecordText() {
        _txtRecord.Text = string.Format(T("browser.search_result_count"), _people.Count);
        if (_currentDetail is null && _people.Count == 0) {
            _txtFooter.Text = string.Format(T("browser.search_result_count"), 0);
        }
    }

    private void UpdateRelatedSummary() {
        if (_currentDetail is null) {
            _txtRelatedCounts.Text = string.Empty;
            _txtRelatedCountsMore.Text = string.Empty;
            _txtRelatedCountsTail.Text = string.Empty;
            return;
        }

        _txtRelatedCounts.Text = string.Format(
            T("browser.related_counts"),
            _currentDetail.AddressCount,
            _currentDetail.AltNameCount,
            _currentDetail.KinCount,
            _currentDetail.AssocCount
        );
        _txtRelatedCountsMore.Text = string.Format(
            T("browser.related_counts_more"),
            _currentDetail.OfficeCount,
            _currentDetail.EntryCount,
            _currentDetail.EventCount,
            _currentDetail.StatusCount,
            _currentDetail.TextCount
        );
        _txtRelatedCountsTail.Text = string.Format(
            T("browser.related_counts_tail"),
            _currentDetail.PossessionCount,
            _currentDetail.SourceCount,
            _currentDetail.InstitutionCount
        );
    }

    private static string EscapeCsv(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static string BuildJoinedName(string? first, string? second) {
        if (string.IsNullOrWhiteSpace(first)) {
            return second ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(second)) {
            return first;
        }

        return $"{first} {second}".Trim();
    }

    private static string JoinDisplay(string? first, string? second) {
        if (string.IsNullOrWhiteSpace(first)) {
            return second ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(second) || string.Equals(first, second, StringComparison.OrdinalIgnoreCase)) {
            return first;
        }

        return $"{first} / {second}";
    }

    private static string NormalizeSqlitePath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        return path.Trim().Trim((char)34);
    }

    private string T(string key) => _localizationService.Get(key);

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtKeyword = this.FindControl<TextBox>("TxtKeyword") ?? throw new InvalidOperationException("TxtKeyword not found.");
        _btnSearch = this.FindControl<Button>("BtnSearch") ?? throw new InvalidOperationException("BtnSearch not found.");
        _btnClear = this.FindControl<Button>("BtnClear") ?? throw new InvalidOperationException("BtnClear not found.");
        _btnSaveToFile = this.FindControl<Button>("BtnSaveToFile") ?? throw new InvalidOperationException("BtnSaveToFile not found.");
        _lblSearchByName = this.FindControl<TextBlock>("LblSearchByName") ?? throw new InvalidOperationException("LblSearchByName not found.");
        _hdrPersonId = this.FindControl<TextBlock>("HdrPersonId") ?? throw new InvalidOperationException("HdrPersonId not found.");
        _hdrNameChn = this.FindControl<TextBlock>("HdrNameChn") ?? throw new InvalidOperationException("HdrNameChn not found.");
        _hdrNameRm = this.FindControl<TextBlock>("HdrNameRm") ?? throw new InvalidOperationException("HdrNameRm not found.");
        _hdrIndexYear = this.FindControl<TextBlock>("HdrIndexYear") ?? throw new InvalidOperationException("HdrIndexYear not found.");
        _hdrIndexAddress = this.FindControl<TextBlock>("HdrIndexAddress") ?? throw new InvalidOperationException("HdrIndexAddress not found.");
        _listPeople = this.FindControl<ListBox>("ListPeople") ?? throw new InvalidOperationException("ListPeople not found.");
        _txtRecord = this.FindControl<TextBlock>("TxtRecord") ?? throw new InvalidOperationException("TxtRecord not found.");
        _lblPersonId = this.FindControl<TextBlock>("LblPersonId") ?? throw new InvalidOperationException("LblPersonId not found.");
        _lblNameChn = this.FindControl<TextBlock>("LblNameChn") ?? throw new InvalidOperationException("LblNameChn not found.");
        _lblName = this.FindControl<TextBlock>("LblName") ?? throw new InvalidOperationException("LblName not found.");
        _lblGender = this.FindControl<TextBlock>("LblGender") ?? throw new InvalidOperationException("LblGender not found.");
        _lblBirthYear = this.FindControl<TextBlock>("LblBirthYear") ?? throw new InvalidOperationException("LblBirthYear not found.");
        _lblDeathYear = this.FindControl<TextBlock>("LblDeathYear") ?? throw new InvalidOperationException("LblDeathYear not found.");
        _lblDynasty = this.FindControl<TextBlock>("LblDynasty") ?? throw new InvalidOperationException("LblDynasty not found.");
        _lblIndexYear = this.FindControl<TextBlock>("LblIndexYear") ?? throw new InvalidOperationException("LblIndexYear not found.");
        _lblIndexYearType = this.FindControl<TextBlock>("LblIndexYearType") ?? throw new InvalidOperationException("LblIndexYearType not found.");
        _lblIndexYearSource = this.FindControl<TextBlock>("LblIndexYearSource") ?? throw new InvalidOperationException("LblIndexYearSource not found.");
        _lblIndexAddress = this.FindControl<TextBlock>("LblIndexAddress") ?? throw new InvalidOperationException("LblIndexAddress not found.");
        _lblIndexAddressType = this.FindControl<TextBlock>("LblIndexAddressType") ?? throw new InvalidOperationException("LblIndexAddressType not found.");
        _valPersonId = this.FindControl<TextBox>("ValPersonId") ?? throw new InvalidOperationException("ValPersonId not found.");
        _valNameChn = this.FindControl<TextBox>("ValNameChn") ?? throw new InvalidOperationException("ValNameChn not found.");
        _valName = this.FindControl<TextBox>("ValName") ?? throw new InvalidOperationException("ValName not found.");
        _valGender = this.FindControl<TextBox>("ValGender") ?? throw new InvalidOperationException("ValGender not found.");
        _valBirthYear = this.FindControl<TextBox>("ValBirthYear") ?? throw new InvalidOperationException("ValBirthYear not found.");
        _valDeathYear = this.FindControl<TextBox>("ValDeathYear") ?? throw new InvalidOperationException("ValDeathYear not found.");
        _valDynasty = this.FindControl<TextBox>("ValDynasty") ?? throw new InvalidOperationException("ValDynasty not found.");
        _valIndexYear = this.FindControl<TextBox>("ValIndexYear") ?? throw new InvalidOperationException("ValIndexYear not found.");
        _valIndexYearType = this.FindControl<TextBox>("ValIndexYearType") ?? throw new InvalidOperationException("ValIndexYearType not found.");
        _valIndexYearSource = this.FindControl<TextBox>("ValIndexYearSource") ?? throw new InvalidOperationException("ValIndexYearSource not found.");
        _valIndexAddress = this.FindControl<TextBox>("ValIndexAddress") ?? throw new InvalidOperationException("ValIndexAddress not found.");
        _valIndexAddressType = this.FindControl<TextBox>("ValIndexAddressType") ?? throw new InvalidOperationException("ValIndexAddressType not found.");
        _tabBasic = this.FindControl<TabItem>("TabBasic") ?? throw new InvalidOperationException("TabBasic not found.");
        _tabRelated = this.FindControl<TabItem>("TabRelated") ?? throw new InvalidOperationException("TabRelated not found.");
        _lblAllFieldsHeader = this.FindControl<TextBlock>("LblAllFieldsHeader") ?? throw new InvalidOperationException("LblAllFieldsHeader not found.");
        _txtNoSelection = this.FindControl<TextBlock>("TxtNoSelection") ?? throw new InvalidOperationException("TxtNoSelection not found.");
        _itemsBasicFields = this.FindControl<ItemsControl>("ItemsBasicFields") ?? throw new InvalidOperationException("ItemsBasicFields not found.");
        _txtRelatedCounts = this.FindControl<TextBlock>("TxtRelatedCounts") ?? throw new InvalidOperationException("TxtRelatedCounts not found.");
        _txtRelatedCountsMore = this.FindControl<TextBlock>("TxtRelatedCountsMore") ?? throw new InvalidOperationException("TxtRelatedCountsMore not found.");
        _txtRelatedCountsTail = this.FindControl<TextBlock>("TxtRelatedCountsTail") ?? throw new InvalidOperationException("TxtRelatedCountsTail not found.");
        _txtRelatedPlaceholder = this.FindControl<TextBlock>("TxtRelatedPlaceholder") ?? throw new InvalidOperationException("TxtRelatedPlaceholder not found.");
        _txtFooter = this.FindControl<TextBlock>("TxtFooter") ?? throw new InvalidOperationException("TxtFooter not found.");
    }
}
