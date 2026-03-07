using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Desktop.Browser;

public partial class PersonBrowserWindow : Window {
    private const int PageSize = 300;

    private readonly ILocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService = new SqlitePersonBrowserService();
    private readonly string _sqlitePath;
    private readonly ObservableCollection<PersonListItem> _people = new();
    private readonly ObservableCollection<PersonFieldValue> _detailFields = new();
    private readonly Dictionary<PersonRelatedCategory, DataGrid> _tabGrids = new();
    private readonly HashSet<PersonRelatedCategory> _loadedTabs = new();

    private string? _currentKeyword;
    private int _nextOffset;
    private bool _hasMore;
    private bool _isLoadingPage;
    private bool _isLoadingTab;
    private int? _selectedPersonId;
    private PersonDetail? _currentDetail;

    public PersonBrowserWindow(string sqlitePath, ILocalizationService localizationService) {
        _sqlitePath = NormalizeSqlitePath(sqlitePath);
        _localizationService = localizationService;

        InitializeComponent();

        GridPeople.ItemsSource = _people;
        ItemsBasicFields.ItemsSource = _detailFields;

        _tabGrids[PersonRelatedCategory.Addresses] = GridAddresses;
        _tabGrids[PersonRelatedCategory.AltNames] = GridAltNames;
        _tabGrids[PersonRelatedCategory.Writings] = GridWritings;
        _tabGrids[PersonRelatedCategory.Postings] = GridPostings;
        _tabGrids[PersonRelatedCategory.Entries] = GridEntry;
        _tabGrids[PersonRelatedCategory.Events] = GridEvents;
        _tabGrids[PersonRelatedCategory.Status] = GridStatus;
        _tabGrids[PersonRelatedCategory.Kinship] = GridKinship;
        _tabGrids[PersonRelatedCategory.Associations] = GridAssociations;

        _localizationService.LanguageChanged += OnLanguageChanged;
        ApplyLocalization();

        Loaded += async (_, _) => await SearchAsync();
        Closed += (_, _) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateFooterText();
    }

    private void ApplyLocalization() {
        Title = B("window_title");
        BtnSearch.Content = B("search");
        BtnClear.Content = B("clear");
        BtnSaveToFile.Content = B("save_to_file");
        LblSearchByName.Text = B("keyword_tooltip");
        LblAllFieldsHeader.Text = B("all_fields_header");
        TxtKeyword.ToolTip = B("keyword_tooltip");

        ColPersonId.Header = B("grid_person_id");
        ColNameChn.Header = B("grid_name_chn");
        ColNameRm.Header = B("grid_name_rm");
        ColIndexYear.Header = B("grid_index_year");
        ColIndexAddress.Header = B("grid_index_address");

        LblGenderSummary.Text = B("gender");
        ChkFemale.Content = B("female");
        LblIndexYearSummary.Text = B("index_year");
        LblBirthDeathSummary.Text = B("birth_death");
        LblIndexAddressSummary.Text = B("index_address");
        LblDynastySummary.Text = B("dynasty");
        LblNotesSummary.Text = B("notes");

        UpdateTabHeaders(_currentDetail);

        if (_people.Count == 0 && !_isLoadingPage) {
            TxtFooter.Text = T("status.ready");
        }
    }

    private void UpdateTabHeaders(PersonDetail? detail) {
        TabBirthDeath.Header = B("tab_birth_death");
        TabAddresses.Header = B("tab_addresses");
        TabAltNames.Header = WithCount(B("tab_alt_names"), detail?.AltNameCount);
        TabWritings.Header = WithCount(B("tab_writings"), detail?.TextCount);
        TabPostings.Header = WithCount(B("tab_postings"), detail?.OfficeCount);
        TabEntry.Header = WithCount(B("tab_entry"), detail?.EntryCount);
        TabEvents.Header = B("tab_events");
        TabStatus.Header = WithCount(B("tab_status"), detail?.StatusCount);
        TabKinship.Header = WithCount(B("tab_kinship"), detail?.KinCount);
        TabAssociations.Header = WithCount(B("tab_associations"), detail?.AssocCount);
        TabPossessions.Header = B("tab_possessions");
        TabSources.Header = B("tab_sources");
        TabInstitutions.Header = B("tab_institutions");
    }

    private static string WithCount(string label, int? count) {
        return count.HasValue ? $"{label} ({count.Value:N0})" : label;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) {
        await SearchAsync();
    }

    private async void BtnClear_Click(object sender, RoutedEventArgs e) {
        TxtKeyword.Text = string.Empty;
        await SearchAsync();
    }

    private async void TxtKeyword_KeyDown(object sender, KeyEventArgs e) {
        if (e.Key != Key.Enter || _isLoadingPage) {
            return;
        }

        e.Handled = true;
        await SearchAsync();
    }

    private async Task SearchAsync() {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _people.Clear();
            _selectedPersonId = null;
            ResetTabData();
            ClearDetail();
            TxtFooter.Text = $"{T("browser.sqlite_missing")}: {_sqlitePath}";
            return;
        }

        _currentKeyword = string.IsNullOrWhiteSpace(TxtKeyword.Text) ? null : TxtKeyword.Text.Trim();
        _nextOffset = 0;
        _hasMore = true;

        _people.Clear();
        _selectedPersonId = null;
        ResetTabData();
        ClearDetail();

        await LoadNextPageAsync(selectFirstRowWhenAvailable: true);
    }

    private async Task LoadNextPageAsync(bool selectFirstRowWhenAvailable = false) {
        if (_isLoadingPage || !_hasMore) {
            return;
        }

        try {
            _isLoadingPage = true;
            BtnSearch.IsEnabled = false;
            BtnClear.IsEnabled = false;
            TxtFooter.Text = _people.Count == 0 ? T("status.checking") : B("loading_more");

            var rows = await Task.Run(
                () => _personBrowserService.SearchAsync(_sqlitePath, _currentKeyword, PageSize, _nextOffset)
            );

            foreach (var row in rows) {
                _people.Add(row);
            }

            _nextOffset += rows.Count;
            _hasMore = rows.Count == PageSize;

            if (selectFirstRowWhenAvailable && _people.Count > 0) {
                GridPeople.SelectedIndex = 0;
            }

            UpdateFooterText();
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        } finally {
            _isLoadingPage = false;
            BtnSearch.IsEnabled = true;
            BtnClear.IsEnabled = true;
        }
    }

    private async void GridPeople_LoadingRow(object sender, DataGridRowEventArgs e) {
        if (_hasMore && !_isLoadingPage && e.Row.GetIndex() >= _people.Count - 20) {
            await LoadNextPageAsync();
        }
    }

    private async void GridPeople_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (GridPeople.SelectedItem is not PersonListItem selected) {
            _selectedPersonId = null;
            ResetTabData();
            ClearDetail();
            return;
        }

        _selectedPersonId = selected.PersonId;
        ResetTabData();

        try {
            var detail = await Task.Run(
                () => _personBrowserService.GetDetailAsync(_sqlitePath, selected.PersonId)
            );
            if (detail is null) {
                ClearDetail();
                return;
            }

            _currentDetail = detail;
            UpdateTabHeaders(detail);

            ValPersonId.Text = detail.PersonId.ToString();
            ValSurnameChn.Text = detail.SurnameChn ?? string.Empty;
            ValMingziChn.Text = detail.MingziChn ?? string.Empty;
            ValSurname.Text = detail.Surname ?? string.Empty;
            ValMingzi.Text = detail.Mingzi ?? string.Empty;
            ValSurnameProper.Text = detail.SurnameProper ?? string.Empty;
            ValMingziProper.Text = detail.MingziProper ?? string.Empty;
            ValSurnameRm.Text = detail.SurnameRm ?? string.Empty;
            ValMingziRm.Text = detail.MingziRm ?? string.Empty;
            ValName.Text = detail.Name ?? string.Empty;
            ValNameChn.Text = detail.NameChn ?? string.Empty;
            ValNameProper.Text = GetFieldValue(detail, "c_name_proper");
            ValNameRm.Text = GetFieldValue(detail, "c_name_rm");
            ValDynasty.Text = JoinDisplay(detail.DynastyChn, detail.Dynasty);
            ValIndexYear.Text = detail.IndexYear?.ToString() ?? string.Empty;
            ValBirthDeath.Text = $"{detail.BirthYear?.ToString() ?? "?"} / {detail.DeathYear?.ToString() ?? "?"}";
            ValGender.Text = detail.Gender ?? string.Empty;
            ChkFemale.IsChecked = string.Equals(detail.Gender, "F", StringComparison.OrdinalIgnoreCase);
            ValIndexAddress.Text = JoinDisplay(detail.IndexAddressChn, detail.IndexAddress);
            TxtNotes.Text = GetFieldValue(detail, "c_notes");

            _detailFields.Clear();
            foreach (var field in detail.Fields) {
                _detailFields.Add(field);
            }

            await LoadSelectedTabAsync();
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        }
    }

    private async void TabsDetail_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (!IsLoaded || !_selectedPersonId.HasValue) {
            return;
        }

        await LoadSelectedTabAsync();
    }

    private async Task LoadSelectedTabAsync() {
        var category = GetSelectedCategory();
        if (category is null) {
            return;
        }

        if (_isLoadingTab || _loadedTabs.Contains(category.Value)) {
            return;
        }

        try {
            _isLoadingTab = true;
            TxtFooter.Text = B("loading_related");

            var rows = await Task.Run(
                () => _personBrowserService.GetRelatedItemsAsync(_sqlitePath, _selectedPersonId!.Value, category.Value, 1000)
            );

            _tabGrids[category.Value].ItemsSource = rows.DefaultView;
            _loadedTabs.Add(category.Value);
            TxtFooter.Text = string.Format(B("related_result_count"), rows.Rows.Count);
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        } finally {
            _isLoadingTab = false;
        }
    }

    private void RelatedGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) {
        if (e.Column is DataGridTextColumn textColumn) {
            textColumn.IsReadOnly = true;
            textColumn.Width = IsWideField(e.PropertyName)
                ? DataGridLength.Auto
                : new DataGridLength(Math.Clamp(e.PropertyName.Length * 14, 90, 220));
        }
    }

    private static bool IsWideField(string propertyName) {
        return propertyName.Contains("notes", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("name", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("pages", StringComparison.OrdinalIgnoreCase);
    }

    private PersonRelatedCategory? GetSelectedCategory() {
        if (TabsDetail.SelectedItem == TabAddresses) return PersonRelatedCategory.Addresses;
        if (TabsDetail.SelectedItem == TabAltNames) return PersonRelatedCategory.AltNames;
        if (TabsDetail.SelectedItem == TabWritings) return PersonRelatedCategory.Writings;
        if (TabsDetail.SelectedItem == TabPostings) return PersonRelatedCategory.Postings;
        if (TabsDetail.SelectedItem == TabEntry) return PersonRelatedCategory.Entries;
        if (TabsDetail.SelectedItem == TabEvents) return PersonRelatedCategory.Events;
        if (TabsDetail.SelectedItem == TabStatus) return PersonRelatedCategory.Status;
        if (TabsDetail.SelectedItem == TabKinship) return PersonRelatedCategory.Kinship;
        if (TabsDetail.SelectedItem == TabAssociations) return PersonRelatedCategory.Associations;

        return null;
    }

    private void ResetTabData() {
        _loadedTabs.Clear();
        foreach (var grid in _tabGrids.Values) {
            grid.ItemsSource = null;
        }
    }

    private void UpdateFooterText() {
        TxtFooter.Text = _hasMore
            ? string.Format(B("search_result_count_more"), _people.Count)
            : string.Format(B("search_result_count"), _people.Count);
        TxtRecord.Text = $"Record: {_people.Count}";
    }

    private void ClearDetail() {
        _currentDetail = null;
        UpdateTabHeaders(null);

        ValPersonId.Text = string.Empty;
        ValSurnameChn.Text = string.Empty;
        ValMingziChn.Text = string.Empty;
        ValSurname.Text = string.Empty;
        ValMingzi.Text = string.Empty;
        ValSurnameProper.Text = string.Empty;
        ValMingziProper.Text = string.Empty;
        ValSurnameRm.Text = string.Empty;
        ValMingziRm.Text = string.Empty;
        ValName.Text = string.Empty;
        ValNameChn.Text = string.Empty;
        ValNameProper.Text = string.Empty;
        ValNameRm.Text = string.Empty;
        ValDynasty.Text = string.Empty;
        ValIndexYear.Text = string.Empty;
        ValBirthDeath.Text = string.Empty;
        ValGender.Text = string.Empty;
        ChkFemale.IsChecked = false;
        ValIndexAddress.Text = string.Empty;
        TxtNotes.Text = string.Empty;
        TxtRecord.Text = "Record: 0";
        _detailFields.Clear();
    }

    private static string GetFieldValue(PersonDetail detail, string fieldName) {
        return detail.Fields.FirstOrDefault(field => string.Equals(field.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
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

        return path.Trim().Trim('"');
    }

    private string T(string key) => _localizationService.Get(key);

    private string B(string key) {
        return _localizationService.CurrentLanguage switch {
            UiLanguage.TraditionalChinese => key switch {
                "window_title" => "人物瀏覽",
                "search" => "查詢",
                "clear" => "清除",
                "save_to_file" => "存成檔案",
                "keyword_tooltip" => "可用姓名、中文名、拼音或別名查詢",
                "all_fields_header" => "BIOG_MAIN 全部欄位",
                "grid_person_id" => "人物 ID",
                "grid_name_chn" => "中文姓名",
                "grid_name_rm" => "拼音",
                "grid_index_year" => "索引年",
                "grid_index_address" => "索引地址",
                "gender" => "性別",
                "female" => "女性",
                "index_year" => "索引年",
                "birth_death" => "生 / 卒",
                "index_address" => "索引地址",
                "dynasty" => "朝代",
                "notes" => "備註",
                "tab_birth_death" => "基本資料",
                "tab_addresses" => "地址",
                "tab_alt_names" => "別名",
                "tab_writings" => "著作",
                "tab_postings" => "任官",
                "tab_entry" => "入仕",
                "tab_events" => "事件",
                "tab_status" => "身份",
                "tab_kinship" => "親屬",
                "tab_associations" => "社會關係",
                "tab_possessions" => "財產",
                "tab_sources" => "來源",
                "tab_institutions" => "機構",
                "search_result_count" => "結果數：{0}",
                "search_result_count_more" => "已載入：{0}（向下捲動可繼續載入）",
                "loading_more" => "載入更多中...",
                "loading_related" => "載入明細中...",
                "related_result_count" => "明細筆數：{0}",
                _ => T("browser." + key)
            },
            UiLanguage.SimplifiedChinese => key switch {
                "window_title" => "人物浏览",
                "search" => "查询",
                "clear" => "清除",
                "save_to_file" => "存成文件",
                "keyword_tooltip" => "可用姓名、中文名、拼音或别名查询",
                "all_fields_header" => "BIOG_MAIN 全部字段",
                "grid_person_id" => "人物 ID",
                "grid_name_chn" => "中文姓名",
                "grid_name_rm" => "拼音",
                "grid_index_year" => "索引年",
                "grid_index_address" => "索引地址",
                "gender" => "性别",
                "female" => "女性",
                "index_year" => "索引年",
                "birth_death" => "生 / 卒",
                "index_address" => "索引地址",
                "dynasty" => "朝代",
                "notes" => "备注",
                "tab_birth_death" => "基本资料",
                "tab_addresses" => "地址",
                "tab_alt_names" => "别名",
                "tab_writings" => "著作",
                "tab_postings" => "任官",
                "tab_entry" => "入仕",
                "tab_events" => "事件",
                "tab_status" => "身份",
                "tab_kinship" => "亲属",
                "tab_associations" => "社会关系",
                "tab_possessions" => "财产",
                "tab_sources" => "来源",
                "tab_institutions" => "机构",
                "search_result_count" => "结果数：{0}",
                "search_result_count_more" => "已加载：{0}（向下滚动可继续加载）",
                "loading_more" => "加载更多中...",
                "loading_related" => "加载明细中...",
                "related_result_count" => "明细笔数：{0}",
                _ => T("browser." + key)
            },
            _ => key switch {
                "window_title" => "Person Browser",
                "search" => "Search",
                "clear" => "Clear",
                "save_to_file" => "Save to File",
                "keyword_tooltip" => "Search by name, Chinese name, pinyin, or alt names",
                "all_fields_header" => "All BIOG_MAIN Fields",
                "grid_person_id" => "ID",
                "grid_name_chn" => "Chinese Name",
                "grid_name_rm" => "Pinyin",
                "grid_index_year" => "Index Year",
                "grid_index_address" => "Index Address",
                "gender" => "Gender",
                "female" => "Female",
                "index_year" => "Index Year",
                "birth_death" => "Birth / Death",
                "index_address" => "Index Address",
                "dynasty" => "Dynasty",
                "notes" => "Notes",
                "tab_birth_death" => "Basic Information",
                "tab_addresses" => "Addresses",
                "tab_alt_names" => "Alt. Names",
                "tab_writings" => "Writings",
                "tab_postings" => "Postings",
                "tab_entry" => "Entry",
                "tab_events" => "Events",
                "tab_status" => "Status",
                "tab_kinship" => "Kinship",
                "tab_associations" => "Associations",
                "tab_possessions" => "Possessions",
                "tab_sources" => "Sources",
                "tab_institutions" => "Institutions",
                "search_result_count" => "Results: {0}",
                "search_result_count_more" => "Loaded: {0} (scroll down to load more)",
                "loading_more" => "Loading more...",
                "loading_related" => "Loading details...",
                "related_result_count" => "Details: {0}",
                _ => T("browser." + key)
            }
        };
    }
}



