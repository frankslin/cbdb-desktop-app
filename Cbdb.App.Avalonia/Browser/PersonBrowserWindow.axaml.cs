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
    private readonly string _sqlitePath;
    private readonly Dictionary<string, TextBlock> _basicGroupHeaders = new();
    private readonly Dictionary<string, TextBlock> _basicLabels = new();
    private readonly Dictionary<string, TextBox> _basicValues = new();
    private readonly Dictionary<string, CheckBox> _basicChecks = new();

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
    private TextBlock _lblSurnameChnField = null!;
    private TextBlock _lblMingziChnField = null!;
    private TextBlock _lblNameChnField = null!;
    private TextBlock _lblPersonId = null!;
    private TextBlock _lblSurnameField = null!;
    private TextBlock _lblMingziField = null!;
    private TextBlock _lblNameField = null!;
    private TextBlock _lblGenderSummary = null!;
    private TextBlock _lblSurnameProperField = null!;
    private TextBlock _lblMingziProperField = null!;
    private TextBlock _lblNameProperField = null!;
    private TextBlock _lblBirthYearSummary = null!;
    private TextBlock _lblSurnameRmField = null!;
    private TextBlock _lblMingziRmField = null!;
    private TextBlock _lblNameRmField = null!;
    private TextBlock _lblDeathYearSummary = null!;
    private TextBlock _lblDynastySummary = null!;
    private TextBlock _lblIndexYearSummary = null!;
    private TextBlock _lblIndexYearTypeSummary = null!;
    private TextBlock _lblIndexYearSourceSummary = null!;
    private TextBlock _lblIndexAddressSummary = null!;
    private TextBlock _lblIndexAddressTypeSummary = null!;
    private TextBox _valPersonId = null!;
    private TextBox _valSurnameChn = null!;
    private TextBox _valMingziChn = null!;
    private TextBox _valNameChn = null!;
    private TextBox _valSurname = null!;
    private TextBox _valMingzi = null!;
    private TextBox _valName = null!;
    private TextBox _valSurnameProper = null!;
    private TextBox _valMingziProper = null!;
    private TextBox _valNameProper = null!;
    private TextBox _valSurnameRm = null!;
    private TextBox _valMingziRm = null!;
    private TextBox _valNameRm = null!;
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
    private TextBlock _txtNoSelection = null!;
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

        _lblSurnameChnField.Text = B("field_surname_chn");
        _lblMingziChnField.Text = B("field_mingzi_chn");
        _lblNameChnField.Text = B("field_name_chn");
        _lblPersonId.Text = "c_personid";
        _lblSurnameField.Text = B("field_surname");
        _lblMingziField.Text = B("field_mingzi");
        _lblNameField.Text = B("field_name");
        _lblGenderSummary.Text = B("gender");
        _lblSurnameProperField.Text = B("field_surname_proper");
        _lblMingziProperField.Text = B("field_mingzi_proper");
        _lblNameProperField.Text = B("field_name_proper");
        _lblBirthYearSummary.Text = B("birth_year");
        _lblSurnameRmField.Text = B("field_surname_rm");
        _lblMingziRmField.Text = B("field_mingzi_rm");
        _lblNameRmField.Text = B("field_name_rm");
        _lblDeathYearSummary.Text = B("death_year");
        _lblDynastySummary.Text = B("dynasty");
        _lblIndexYearSummary.Text = B("index_year");
        _lblIndexYearTypeSummary.Text = B("index_year_type");
        _lblIndexYearSourceSummary.Text = B("index_year_source");
        _lblIndexAddressSummary.Text = B("index_address");
        _lblIndexAddressTypeSummary.Text = B("index_address_type");

        _tabBasic.Header = T("browser.tab_basic");
        _tabRelated.Header = T("browser.tab_related");
        _txtNoSelection.Text = _currentDetail is null ? T("browser.no_selection") : string.Empty;
        _txtRelatedPlaceholder.Text = T("browser.related_placeholder");
        ApplyBasicInfoLocalization();

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
            _valSurnameChn.Text = detail.SurnameChn ?? string.Empty;
            _valMingziChn.Text = detail.MingziChn ?? string.Empty;
            _valNameChn.Text = detail.NameChn ?? string.Empty;
            _valSurname.Text = detail.Surname ?? string.Empty;
            _valMingzi.Text = detail.Mingzi ?? string.Empty;
            _valName.Text = detail.Name ?? string.Empty;
            _valSurnameProper.Text = detail.SurnameProper ?? string.Empty;
            _valMingziProper.Text = detail.MingziProper ?? string.Empty;
            _valNameProper.Text = GetRawField(detail, "c_name_proper");
            _valSurnameRm.Text = detail.SurnameRm ?? string.Empty;
            _valMingziRm.Text = detail.MingziRm ?? string.Empty;
            _valNameRm.Text = GetRawField(detail, "c_name_rm");
            _valGender.Text = detail.Gender switch {
                "M" => B("male"),
                "F" => B("female"),
                _ => B("unknown")
            };
            _valBirthYear.Text = detail.BirthYear?.ToString() ?? string.Empty;
            _valDeathYear.Text = detail.DeathYear?.ToString() ?? string.Empty;
            _valDynasty.Text = JoinDisplay(detail.DynastyChn, detail.Dynasty);
            _valIndexYear.Text = detail.IndexYear?.ToString() ?? string.Empty;
            _valIndexYearType.Text = detail.IndexYearType ?? string.Empty;
            _valIndexYearSource.Text = detail.IndexYearSource ?? string.Empty;
            _valIndexAddress.Text = JoinDisplay(detail.IndexAddressChn, detail.IndexAddress);
            _valIndexAddressType.Text = detail.IndexAddressType ?? string.Empty;

            PopulateBasicInfo(detail);
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
        _valSurnameChn.Text = string.Empty;
        _valMingziChn.Text = string.Empty;
        _valNameChn.Text = string.Empty;
        _valSurname.Text = string.Empty;
        _valMingzi.Text = string.Empty;
        _valName.Text = string.Empty;
        _valSurnameProper.Text = string.Empty;
        _valMingziProper.Text = string.Empty;
        _valNameProper.Text = string.Empty;
        _valSurnameRm.Text = string.Empty;
        _valMingziRm.Text = string.Empty;
        _valNameRm.Text = string.Empty;
        _valGender.Text = string.Empty;
        _valBirthYear.Text = string.Empty;
        _valDeathYear.Text = string.Empty;
        _valDynasty.Text = string.Empty;
        _valIndexYear.Text = string.Empty;
        _valIndexYearType.Text = string.Empty;
        _valIndexYearSource.Text = string.Empty;
        _valIndexAddress.Text = string.Empty;
        _valIndexAddressType.Text = string.Empty;

        ClearBasicInfo();
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

    private string B(string key) {
        return _localizationService.CurrentLanguage switch {
            UiLanguage.TraditionalChinese => key switch {
                "field_surname_chn" => "姓（中文）",
                "field_mingzi_chn" => "名（中文）",
                "field_name_chn" => "姓名（中文）",
                "field_surname" => "姓（拼音）",
                "field_mingzi" => "名（拼音）",
                "field_name" => "姓名（拼音）",
                "field_surname_proper" => "外文姓／原文姓",
                "field_mingzi_proper" => "外文名／原文名",
                "field_name_proper" => "外文全名／原文全名",
                "field_surname_rm" => "姓（羅馬字轉寫）",
                "field_mingzi_rm" => "名（羅馬字轉寫）",
                "field_name_rm" => "姓名（羅馬字轉寫）",
                "gender" => "性別",
                "male" => "男",
                "female" => "女",
                "unknown" => "未詳",
                "birth_year" => "出生年",
                "death_year" => "卒年",
                "dynasty" => "朝代",
                "index_year" => "索引年",
                "index_year_type" => "規則類型",
                "index_year_source" => "來源",
                "index_address" => "索引地址",
                "index_address_type" => "規則類型",
                _ => key
            },
            UiLanguage.SimplifiedChinese => key switch {
                "field_surname_chn" => "姓（中文）",
                "field_mingzi_chn" => "名（中文）",
                "field_name_chn" => "姓名（中文）",
                "field_surname" => "姓（拼音）",
                "field_mingzi" => "名（拼音）",
                "field_name" => "姓名（拼音）",
                "field_surname_proper" => "外文姓／原文姓",
                "field_mingzi_proper" => "外文名／原文名",
                "field_name_proper" => "外文全名／原文全名",
                "field_surname_rm" => "姓（罗马字转写）",
                "field_mingzi_rm" => "名（罗马字转写）",
                "field_name_rm" => "姓名（罗马字转写）",
                "gender" => "性别",
                "male" => "男",
                "female" => "女",
                "unknown" => "未详",
                "birth_year" => "出生年",
                "death_year" => "卒年",
                "dynasty" => "朝代",
                "index_year" => "索引年",
                "index_year_type" => "规则类型",
                "index_year_source" => "来源",
                "index_address" => "索引地址",
                "index_address_type" => "规则类型",
                _ => key
            },
            _ => key switch {
                "field_surname_chn" => "Surname (Chinese)",
                "field_mingzi_chn" => "Given Name (Chinese)",
                "field_name_chn" => "Full Name (Chinese)",
                "field_surname" => "Surname (Pinyin)",
                "field_mingzi" => "Given Name (Pinyin)",
                "field_name" => "Full Name (Pinyin)",
                "field_surname_proper" => "Surname (Foreign/Original)",
                "field_mingzi_proper" => "Given Name (Foreign/Original)",
                "field_name_proper" => "Full Name (Foreign/Original)",
                "field_surname_rm" => "Surname (Romanized)",
                "field_mingzi_rm" => "Given Name (Romanized)",
                "field_name_rm" => "Full Name (Romanized)",
                "gender" => "Gender",
                "male" => "Male",
                "female" => "Female",
                "unknown" => "Unknown",
                "birth_year" => "Birth Year",
                "death_year" => "Death Year",
                "dynasty" => "Dynasty",
                "index_year" => "Index Year",
                "index_year_type" => "Rule Type",
                "index_year_source" => "Source",
                "index_address" => "Index Address",
                "index_address_type" => "Rule Type",
                _ => key
            }
        };
    }

    private void ApplyBasicInfoLocalization() {
        SetGroupHeader("birth_group");
        SetGroupHeader("death_group");
        SetGroupHeader("fl_group");
        SetGroupHeader("identity_group");
        SetGroupHeader("notes_group");
        SetGroupHeader("audit_group");

        foreach (var pair in _basicLabels) {
            pair.Value.Text = BasicText(pair.Key);
        }

        if (_basicChecks.TryGetValue("birth_intercalary", out var birthIntercalary)) {
            birthIntercalary.Content = BasicText("birth_intercalary");
        }
        if (_basicChecks.TryGetValue("death_intercalary", out var deathIntercalary)) {
            deathIntercalary.Content = BasicText("death_intercalary");
        }
    }

    private void SetGroupHeader(string key) {
        if (_basicGroupHeaders.TryGetValue(key, out var header)) {
            header.Text = BasicText(key);
        }
    }

    private void PopulateBasicInfo(PersonDetail detail) {
        SetValue("birth_year", GetRawField(detail, "c_birthyear"));
        SetValue("birth_nianhao", GetDisplayOnlyField(detail, "c_by_nh_code"));
        SetValue("birth_nianhao_year", GetRawField(detail, "c_by_nh_year"));
        SetValue("birth_month", GetRawField(detail, "c_by_month"));
        SetCheck("birth_intercalary", GetBooleanField(detail, "c_by_intercalary"));
        SetValue("birth_day", GetRawField(detail, "c_by_day"));
        SetValue("birth_ganzhi", GetDisplayOnlyField(detail, "c_by_day_gz"));
        SetValue("birth_range", GetDisplayOnlyField(detail, "c_by_range"));

        SetValue("death_year", GetRawField(detail, "c_deathyear"));
        SetValue("death_nianhao", GetDisplayOnlyField(detail, "c_dy_nh_code"));
        SetValue("death_nianhao_year", GetRawField(detail, "c_dy_nh_year"));
        SetValue("death_month", GetRawField(detail, "c_dy_month"));
        SetCheck("death_intercalary", GetBooleanField(detail, "c_dy_intercalary"));
        SetValue("death_day", GetRawField(detail, "c_dy_day"));
        SetValue("death_ganzhi", GetDisplayOnlyField(detail, "c_dy_day_gz"));
        SetValue("death_range", GetDisplayOnlyField(detail, "c_dy_range"));
        SetValue("death_age", GetRawField(detail, "c_death_age"));
        SetValue("death_age_range", GetDisplayOnlyField(detail, "c_death_age_range"));

        SetValue("fl_first_year", GetRawField(detail, "c_fl_earliest_year"));
        SetValue("fl_first_nianhao", GetDisplayOnlyField(detail, "c_fl_ey_nh_code"));
        SetValue("fl_first_nianhao_year", GetRawField(detail, "c_fl_ey_nh_year"));
        SetValue("fl_first_notes", GetRawField(detail, "c_fl_ey_notes"));
        SetValue("fl_last_year", GetRawField(detail, "c_fl_latest_year"));
        SetValue("fl_last_nianhao", GetDisplayOnlyField(detail, "c_fl_ly_nh_code"));
        SetValue("fl_last_nianhao_year", GetRawField(detail, "c_fl_ly_nh_year"));
        SetValue("fl_last_notes", GetRawField(detail, "c_fl_ly_notes"));

        SetValue("choronym", GetDisplayOnlyField(detail, "c_choronym_code"));
        SetValue("household", GetDisplayOnlyField(detail, "c_household_status_code"));
        SetValue("ethnicity_tribe", JoinDisplay(GetDisplayOnlyField(detail, "c_ethnicity_code"), GetRawField(detail, "c_tribe")));
        SetValue("notes", GetRawField(detail, "c_notes"));

        SetValue("created_by", GetRawField(detail, "c_created_by"));
        SetValue("created_date", GetRawField(detail, "c_created_date"));
        SetValue("modified_by", GetRawField(detail, "c_modified_by"));
        SetValue("modified_date", GetRawField(detail, "c_modified_date"));
    }

    private void ClearBasicInfo() {
        foreach (var box in _basicValues.Values) {
            box.Text = string.Empty;
        }

        foreach (var check in _basicChecks.Values) {
            check.IsChecked = false;
        }
    }

    private void SetValue(string key, string? value) {
        if (_basicValues.TryGetValue(key, out var box)) {
            box.Text = value ?? string.Empty;
        }
    }

    private void SetCheck(string key, bool isChecked) {
        if (_basicChecks.TryGetValue(key, out var check)) {
            check.IsChecked = isChecked;
        }
    }

    private static bool GetBooleanField(PersonDetail detail, string fieldName) {
        var value = GetRawField(detail, fieldName);
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRawField(PersonDetail detail, string fieldName) {
        return detail.Fields.FirstOrDefault(field => string.Equals(field.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }

    private static string GetDisplayOnlyField(PersonDetail detail, string fieldName) {
        var value = GetRawField(detail, fieldName);
        var separatorIndex = value.IndexOf("|", StringComparison.Ordinal);
        return separatorIndex >= 0 ? value[(separatorIndex + 1)..].Trim() : value;
    }

    private string BasicText(string key) {
        return _localizationService.CurrentLanguage switch {
            UiLanguage.TraditionalChinese => key switch {
                "birth_group" => "生",
                "birth_gregorian" => "公元年份",
                "birth_nianhao" => "年號",
                "birth_nianhao_year" => "年號年",
                "birth_month" => "月",
                "birth_intercalary" => "閏月",
                "birth_day" => "日",
                "birth_ganzhi" => "干支",
                "birth_range" => "時限",
                "death_group" => "卒",
                "death_gregorian" => "公元年份",
                "death_nianhao" => "年號",
                "death_nianhao_year" => "年號年",
                "death_month" => "月",
                "death_intercalary" => "閏月",
                "death_day" => "日",
                "death_ganzhi" => "干支",
                "death_range" => "時限",
                "death_age" => "享年",
                "death_age_range" => "時限",
                "fl_group" => "在世年份",
                "fl_first_year" => "在世始年",
                "fl_first_nianhao" => "年號",
                "fl_first_nianhao_year" => "年號年",
                "fl_first_notes" => "注釋",
                "fl_last_year" => "在世終年",
                "fl_last_nianhao" => "年號",
                "fl_last_nianhao_year" => "年號年",
                "fl_last_notes" => "注釋",
                "identity_group" => "身份與歸屬",
                "choronym" => "郡望",
                "household" => "戶籍",
                "ethnicity_tribe" => "種族／部族",
                "notes" => "備註",
                "notes_group" => "備註",
                "audit_group" => "建立與修改",
                "created_by" => "建立人",
                "created_date" => "建立日期",
                "modified_by" => "修改人",
                "modified_date" => "修改日期",
                _ => key
            },
            UiLanguage.SimplifiedChinese => key switch {
                "birth_group" => "生",
                "birth_gregorian" => "公元年份",
                "birth_nianhao" => "年号",
                "birth_nianhao_year" => "年号年",
                "birth_month" => "月",
                "birth_intercalary" => "闰月",
                "birth_day" => "日",
                "birth_ganzhi" => "干支",
                "birth_range" => "时限",
                "death_group" => "卒",
                "death_gregorian" => "公元年份",
                "death_nianhao" => "年号",
                "death_nianhao_year" => "年号年",
                "death_month" => "月",
                "death_intercalary" => "闰月",
                "death_day" => "日",
                "death_ganzhi" => "干支",
                "death_range" => "时限",
                "death_age" => "享年",
                "death_age_range" => "时限",
                "fl_group" => "在世年份",
                "fl_first_year" => "在世始年",
                "fl_first_nianhao" => "年号",
                "fl_first_nianhao_year" => "年号年",
                "fl_first_notes" => "注释",
                "fl_last_year" => "在世终年",
                "fl_last_nianhao" => "年号",
                "fl_last_nianhao_year" => "年号年",
                "fl_last_notes" => "注释",
                "identity_group" => "身份与归属",
                "choronym" => "郡望",
                "household" => "户籍",
                "ethnicity_tribe" => "种族／部族",
                "notes" => "备注",
                "notes_group" => "备注",
                "audit_group" => "创建与修改",
                "created_by" => "创建人",
                "created_date" => "创建日期",
                "modified_by" => "修改人",
                "modified_date" => "修改日期",
                _ => key
            },
            _ => key switch {
                "birth_group" => "Birth",
                "birth_gregorian" => "Gregorian Year",
                "birth_nianhao" => "Reign Title",
                "birth_nianhao_year" => "Reign Year",
                "birth_month" => "Month",
                "birth_intercalary" => "Intercalary Month",
                "birth_day" => "Day",
                "birth_ganzhi" => "Sexagenary Day",
                "birth_range" => "Range",
                "death_group" => "Death",
                "death_gregorian" => "Gregorian Year",
                "death_nianhao" => "Reign Title",
                "death_nianhao_year" => "Reign Year",
                "death_month" => "Month",
                "death_intercalary" => "Intercalary Month",
                "death_day" => "Day",
                "death_ganzhi" => "Sexagenary Day",
                "death_range" => "Range",
                "death_age" => "Age at Death",
                "death_age_range" => "Age Range",
                "fl_group" => "Years Alive",
                "fl_first_year" => "Earliest Living Year",
                "fl_first_nianhao" => "Reign Title",
                "fl_first_nianhao_year" => "Reign Year",
                "fl_first_notes" => "Notes",
                "fl_last_year" => "Latest Living Year",
                "fl_last_nianhao" => "Reign Title",
                "fl_last_nianhao_year" => "Reign Year",
                "fl_last_notes" => "Notes",
                "identity_group" => "Identity and Origin",
                "choronym" => "Choronym",
                "household" => "Household Status",
                "ethnicity_tribe" => "Ethnicity / Tribe",
                "notes" => "Notes",
                "notes_group" => "Notes",
                "audit_group" => "Created and Modified",
                "created_by" => "Created By",
                "created_date" => "Created Date",
                "modified_by" => "Modified By",
                "modified_date" => "Modified Date",
                _ => key
            }
        };
    }

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
        _lblSurnameChnField = this.FindControl<TextBlock>("LblSurnameChnField") ?? throw new InvalidOperationException("LblSurnameChnField not found.");
        _lblMingziChnField = this.FindControl<TextBlock>("LblMingziChnField") ?? throw new InvalidOperationException("LblMingziChnField not found.");
        _lblNameChnField = this.FindControl<TextBlock>("LblNameChnField") ?? throw new InvalidOperationException("LblNameChnField not found.");
        _lblPersonId = this.FindControl<TextBlock>("LblPersonId") ?? throw new InvalidOperationException("LblPersonId not found.");
        _lblSurnameField = this.FindControl<TextBlock>("LblSurnameField") ?? throw new InvalidOperationException("LblSurnameField not found.");
        _lblMingziField = this.FindControl<TextBlock>("LblMingziField") ?? throw new InvalidOperationException("LblMingziField not found.");
        _lblNameField = this.FindControl<TextBlock>("LblNameField") ?? throw new InvalidOperationException("LblNameField not found.");
        _lblGenderSummary = this.FindControl<TextBlock>("LblGenderSummary") ?? throw new InvalidOperationException("LblGenderSummary not found.");
        _lblSurnameProperField = this.FindControl<TextBlock>("LblSurnameProperField") ?? throw new InvalidOperationException("LblSurnameProperField not found.");
        _lblMingziProperField = this.FindControl<TextBlock>("LblMingziProperField") ?? throw new InvalidOperationException("LblMingziProperField not found.");
        _lblNameProperField = this.FindControl<TextBlock>("LblNameProperField") ?? throw new InvalidOperationException("LblNameProperField not found.");
        _lblBirthYearSummary = this.FindControl<TextBlock>("LblBirthYearSummary") ?? throw new InvalidOperationException("LblBirthYearSummary not found.");
        _lblSurnameRmField = this.FindControl<TextBlock>("LblSurnameRmField") ?? throw new InvalidOperationException("LblSurnameRmField not found.");
        _lblMingziRmField = this.FindControl<TextBlock>("LblMingziRmField") ?? throw new InvalidOperationException("LblMingziRmField not found.");
        _lblNameRmField = this.FindControl<TextBlock>("LblNameRmField") ?? throw new InvalidOperationException("LblNameRmField not found.");
        _lblDeathYearSummary = this.FindControl<TextBlock>("LblDeathYearSummary") ?? throw new InvalidOperationException("LblDeathYearSummary not found.");
        _lblDynastySummary = this.FindControl<TextBlock>("LblDynastySummary") ?? throw new InvalidOperationException("LblDynastySummary not found.");
        _lblIndexYearSummary = this.FindControl<TextBlock>("LblIndexYearSummary") ?? throw new InvalidOperationException("LblIndexYearSummary not found.");
        _lblIndexYearTypeSummary = this.FindControl<TextBlock>("LblIndexYearTypeSummary") ?? throw new InvalidOperationException("LblIndexYearTypeSummary not found.");
        _lblIndexYearSourceSummary = this.FindControl<TextBlock>("LblIndexYearSourceSummary") ?? throw new InvalidOperationException("LblIndexYearSourceSummary not found.");
        _lblIndexAddressSummary = this.FindControl<TextBlock>("LblIndexAddressSummary") ?? throw new InvalidOperationException("LblIndexAddressSummary not found.");
        _lblIndexAddressTypeSummary = this.FindControl<TextBlock>("LblIndexAddressTypeSummary") ?? throw new InvalidOperationException("LblIndexAddressTypeSummary not found.");
        _valPersonId = this.FindControl<TextBox>("ValPersonId") ?? throw new InvalidOperationException("ValPersonId not found.");
        _valSurnameChn = this.FindControl<TextBox>("ValSurnameChn") ?? throw new InvalidOperationException("ValSurnameChn not found.");
        _valMingziChn = this.FindControl<TextBox>("ValMingziChn") ?? throw new InvalidOperationException("ValMingziChn not found.");
        _valNameChn = this.FindControl<TextBox>("ValNameChn") ?? throw new InvalidOperationException("ValNameChn not found.");
        _valSurname = this.FindControl<TextBox>("ValSurname") ?? throw new InvalidOperationException("ValSurname not found.");
        _valMingzi = this.FindControl<TextBox>("ValMingzi") ?? throw new InvalidOperationException("ValMingzi not found.");
        _valName = this.FindControl<TextBox>("ValName") ?? throw new InvalidOperationException("ValName not found.");
        _valSurnameProper = this.FindControl<TextBox>("ValSurnameProper") ?? throw new InvalidOperationException("ValSurnameProper not found.");
        _valMingziProper = this.FindControl<TextBox>("ValMingziProper") ?? throw new InvalidOperationException("ValMingziProper not found.");
        _valNameProper = this.FindControl<TextBox>("ValNameProper") ?? throw new InvalidOperationException("ValNameProper not found.");
        _valSurnameRm = this.FindControl<TextBox>("ValSurnameRm") ?? throw new InvalidOperationException("ValSurnameRm not found.");
        _valMingziRm = this.FindControl<TextBox>("ValMingziRm") ?? throw new InvalidOperationException("ValMingziRm not found.");
        _valNameRm = this.FindControl<TextBox>("ValNameRm") ?? throw new InvalidOperationException("ValNameRm not found.");
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
        _txtNoSelection = this.FindControl<TextBlock>("TxtNoSelection") ?? throw new InvalidOperationException("TxtNoSelection not found.");
        _txtRelatedCounts = this.FindControl<TextBlock>("TxtRelatedCounts") ?? throw new InvalidOperationException("TxtRelatedCounts not found.");
        _txtRelatedCountsMore = this.FindControl<TextBlock>("TxtRelatedCountsMore") ?? throw new InvalidOperationException("TxtRelatedCountsMore not found.");
        _txtRelatedCountsTail = this.FindControl<TextBlock>("TxtRelatedCountsTail") ?? throw new InvalidOperationException("TxtRelatedCountsTail not found.");
        _txtRelatedPlaceholder = this.FindControl<TextBlock>("TxtRelatedPlaceholder") ?? throw new InvalidOperationException("TxtRelatedPlaceholder not found.");
        _txtFooter = this.FindControl<TextBlock>("TxtFooter") ?? throw new InvalidOperationException("TxtFooter not found.");

        _basicGroupHeaders["birth_group"] = this.FindControl<TextBlock>("HdrBasic_birth_group") ?? throw new InvalidOperationException("HdrBasic_birth_group not found.");
        _basicGroupHeaders["death_group"] = this.FindControl<TextBlock>("HdrBasic_death_group") ?? throw new InvalidOperationException("HdrBasic_death_group not found.");
        _basicGroupHeaders["fl_group"] = this.FindControl<TextBlock>("HdrBasic_fl_group") ?? throw new InvalidOperationException("HdrBasic_fl_group not found.");
        _basicGroupHeaders["identity_group"] = this.FindControl<TextBlock>("HdrBasic_identity_group") ?? throw new InvalidOperationException("HdrBasic_identity_group not found.");
        _basicGroupHeaders["notes_group"] = this.FindControl<TextBlock>("HdrBasic_notes_group") ?? throw new InvalidOperationException("HdrBasic_notes_group not found.");
        _basicGroupHeaders["audit_group"] = this.FindControl<TextBlock>("HdrBasic_audit_group") ?? throw new InvalidOperationException("HdrBasic_audit_group not found.");

        RegisterBasicLabel("birth_gregorian", "LblBasic_birth_gregorian");
        RegisterBasicLabel("birth_nianhao", "LblBasic_birth_nianhao");
        RegisterBasicLabel("birth_nianhao_year", "LblBasic_birth_nianhao_year");
        RegisterBasicLabel("birth_month", "LblBasic_birth_month");
        RegisterBasicLabel("birth_day", "LblBasic_birth_day");
        RegisterBasicLabel("birth_ganzhi", "LblBasic_birth_ganzhi");
        RegisterBasicLabel("birth_range", "LblBasic_birth_range");
        RegisterBasicLabel("death_gregorian", "LblBasic_death_gregorian");
        RegisterBasicLabel("death_nianhao", "LblBasic_death_nianhao");
        RegisterBasicLabel("death_nianhao_year", "LblBasic_death_nianhao_year");
        RegisterBasicLabel("death_month", "LblBasic_death_month");
        RegisterBasicLabel("death_day", "LblBasic_death_day");
        RegisterBasicLabel("death_ganzhi", "LblBasic_death_ganzhi");
        RegisterBasicLabel("death_range", "LblBasic_death_range");
        RegisterBasicLabel("death_age", "LblBasic_death_age");
        RegisterBasicLabel("death_age_range", "LblBasic_death_age_range");
        RegisterBasicLabel("fl_first_year", "LblBasic_fl_first_year");
        RegisterBasicLabel("fl_first_nianhao", "LblBasic_fl_first_nianhao");
        RegisterBasicLabel("fl_first_nianhao_year", "LblBasic_fl_first_nianhao_year");
        RegisterBasicLabel("fl_first_notes", "LblBasic_fl_first_notes");
        RegisterBasicLabel("fl_last_year", "LblBasic_fl_last_year");
        RegisterBasicLabel("fl_last_nianhao", "LblBasic_fl_last_nianhao");
        RegisterBasicLabel("fl_last_nianhao_year", "LblBasic_fl_last_nianhao_year");
        RegisterBasicLabel("fl_last_notes", "LblBasic_fl_last_notes");
        RegisterBasicLabel("choronym", "LblBasic_choronym");
        RegisterBasicLabel("household", "LblBasic_household");
        RegisterBasicLabel("ethnicity_tribe", "LblBasic_ethnicity_tribe");
        RegisterBasicLabel("notes", "LblBasic_notes");
        RegisterBasicLabel("created_by", "LblBasic_created_by");
        RegisterBasicLabel("created_date", "LblBasic_created_date");
        RegisterBasicLabel("modified_by", "LblBasic_modified_by");
        RegisterBasicLabel("modified_date", "LblBasic_modified_date");

        RegisterBasicValue("birth_year", "ValBasic_birth_year");
        RegisterBasicValue("birth_nianhao", "ValBasic_birth_nianhao");
        RegisterBasicValue("birth_nianhao_year", "ValBasic_birth_nianhao_year");
        RegisterBasicValue("birth_month", "ValBasic_birth_month");
        RegisterBasicValue("birth_day", "ValBasic_birth_day");
        RegisterBasicValue("birth_ganzhi", "ValBasic_birth_ganzhi");
        RegisterBasicValue("birth_range", "ValBasic_birth_range");
        RegisterBasicValue("death_year", "ValBasic_death_year");
        RegisterBasicValue("death_nianhao", "ValBasic_death_nianhao");
        RegisterBasicValue("death_nianhao_year", "ValBasic_death_nianhao_year");
        RegisterBasicValue("death_month", "ValBasic_death_month");
        RegisterBasicValue("death_day", "ValBasic_death_day");
        RegisterBasicValue("death_ganzhi", "ValBasic_death_ganzhi");
        RegisterBasicValue("death_range", "ValBasic_death_range");
        RegisterBasicValue("death_age", "ValBasic_death_age");
        RegisterBasicValue("death_age_range", "ValBasic_death_age_range");
        RegisterBasicValue("fl_first_year", "ValBasic_fl_first_year");
        RegisterBasicValue("fl_first_nianhao", "ValBasic_fl_first_nianhao");
        RegisterBasicValue("fl_first_nianhao_year", "ValBasic_fl_first_nianhao_year");
        RegisterBasicValue("fl_first_notes", "ValBasic_fl_first_notes");
        RegisterBasicValue("fl_last_year", "ValBasic_fl_last_year");
        RegisterBasicValue("fl_last_nianhao", "ValBasic_fl_last_nianhao");
        RegisterBasicValue("fl_last_nianhao_year", "ValBasic_fl_last_nianhao_year");
        RegisterBasicValue("fl_last_notes", "ValBasic_fl_last_notes");
        RegisterBasicValue("choronym", "ValBasic_choronym");
        RegisterBasicValue("household", "ValBasic_household");
        RegisterBasicValue("ethnicity_tribe", "ValBasic_ethnicity_tribe");
        RegisterBasicValue("notes", "ValBasic_notes");
        RegisterBasicValue("created_by", "ValBasic_created_by");
        RegisterBasicValue("created_date", "ValBasic_created_date");
        RegisterBasicValue("modified_by", "ValBasic_modified_by");
        RegisterBasicValue("modified_date", "ValBasic_modified_date");

        RegisterBasicCheck("birth_intercalary", "ChkBasic_birth_intercalary");
        RegisterBasicCheck("death_intercalary", "ChkBasic_death_intercalary");
    }

    private void RegisterBasicLabel(string key, string controlName) {
        _basicLabels[key] = this.FindControl<TextBlock>(controlName) ?? throw new InvalidOperationException($"{controlName} not found.");
    }

    private void RegisterBasicValue(string key, string controlName) {
        _basicValues[key] = this.FindControl<TextBox>(controlName) ?? throw new InvalidOperationException($"{controlName} not found.");
    }

    private void RegisterBasicCheck(string key, string controlName) {
        _basicChecks[key] = this.FindControl<CheckBox>(controlName) ?? throw new InvalidOperationException($"{controlName} not found.");
    }
}
