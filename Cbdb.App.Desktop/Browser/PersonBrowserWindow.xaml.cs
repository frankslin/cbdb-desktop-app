using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Desktop.Browser;

public partial class PersonBrowserWindow : Window {
    private const int PageSize = 300;

    private readonly ILocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService = new SqlitePersonBrowserService();
    private readonly string _sqlitePath;
    private readonly ObservableCollection<PersonListItem> _people = new();

    private readonly Dictionary<PersonRelatedCategory, ObservableCollection<PersonRelatedItem>> _tabItems = new();
    private readonly HashSet<PersonRelatedCategory> _loadedTabs = new();

    private string? _currentKeyword;
    private int _nextOffset;
    private bool _hasMore;
    private bool _isLoadingPage;
    private bool _isLoadingTab;
    private int? _selectedPersonId;

    public PersonBrowserWindow(string sqlitePath, ILocalizationService localizationService) {
        _sqlitePath = NormalizeSqlitePath(sqlitePath);
        _localizationService = localizationService;

        InitializeComponent();

        GridPeople.ItemsSource = _people;

        _tabItems[PersonRelatedCategory.Addresses] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.AltNames] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Writings] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Postings] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Entries] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Events] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Status] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Kinship] = new ObservableCollection<PersonRelatedItem>();
        _tabItems[PersonRelatedCategory.Associations] = new ObservableCollection<PersonRelatedItem>();

        GridAddresses.ItemsSource = _tabItems[PersonRelatedCategory.Addresses];
        GridAltNames.ItemsSource = _tabItems[PersonRelatedCategory.AltNames];
        GridWritings.ItemsSource = _tabItems[PersonRelatedCategory.Writings];
        GridPostings.ItemsSource = _tabItems[PersonRelatedCategory.Postings];
        GridEntry.ItemsSource = _tabItems[PersonRelatedCategory.Entries];
        GridEvents.ItemsSource = _tabItems[PersonRelatedCategory.Events];
        GridStatus.ItemsSource = _tabItems[PersonRelatedCategory.Status];
        GridKinship.ItemsSource = _tabItems[PersonRelatedCategory.Kinship];
        GridAssociations.ItemsSource = _tabItems[PersonRelatedCategory.Associations];

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
        Title = T("browser.window_title");
        BtnSearch.Content = T("browser.search");
        BtnClear.Content = T("browser.clear");
        BtnSaveToFile.Content = T("browser.save_to_file");
        LblSearchByName.Text = T("browser.search_by_name_office");
        TxtKeyword.ToolTip = T("browser.keyword_tooltip");

        ColPersonId.Header = T("browser.grid_person_id");
        ColName.Header = T("browser.grid_name");
        ColNameChn.Header = T("browser.grid_name_chn");

        TabBirthDeath.Header = T("browser.tab_birth_death");
        TabAddresses.Header = T("browser.tab_addresses");
        TabAltNames.Header = T("browser.tab_alt_names");
        TabWritings.Header = T("browser.tab_writings");
        TabPostings.Header = T("browser.tab_postings");
        TabEntry.Header = T("browser.tab_entry");
        TabEvents.Header = T("browser.tab_events");
        TabStatus.Header = T("browser.tab_status");
        TabKinship.Header = T("browser.tab_kinship");
        TabAssociations.Header = T("browser.tab_associations");

        LocalizeRelatedGridHeaders(GridAddresses);
        LocalizeRelatedGridHeaders(GridAltNames);
        LocalizeRelatedGridHeaders(GridWritings);
        LocalizeRelatedGridHeaders(GridPostings);
        LocalizeRelatedGridHeaders(GridEntry);
        LocalizeRelatedGridHeaders(GridEvents);
        LocalizeRelatedGridHeaders(GridStatus);
        LocalizeRelatedGridHeaders(GridKinship);
        LocalizeRelatedGridHeaders(GridAssociations);

        LblPersonId.Text = T("browser.person_id");
        LblName.Text = T("browser.name");
        LblNameChn.Text = T("browser.name_chn");
        LblDynasty.Text = T("browser.dynasty");
        LblIndexYear.Text = T("browser.index_year");
        LblBirthDeath.Text = T("browser.birth_death");
        LblGender.Text = T("browser.gender");
        LblIndexAddress.Text = T("browser.index_address");

        LblAltNames.Text = T("browser.count_altnames");
        LblKin.Text = T("browser.count_kin");
        LblAssoc.Text = T("browser.count_assoc");

        if (_people.Count == 0 && !_isLoadingPage) {
            TxtFooter.Text = T("status.ready");
        }
    }

    private void LocalizeRelatedGridHeaders(DataGrid grid) {
        if (grid.Columns.Count < 3) {
            return;
        }

        grid.Columns[0].Header = T("browser.related_primary");
        grid.Columns[1].Header = T("browser.related_secondary");
        grid.Columns[2].Header = T("browser.related_note");
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) {
        await SearchAsync();
    }

    private async void BtnClear_Click(object sender, RoutedEventArgs e) {
        TxtKeyword.Text = string.Empty;
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
            TxtFooter.Text = _people.Count == 0 ? T("status.checking") : T("browser.loading_more");

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

            ValPersonId.Text = detail.PersonId.ToString();
            ValName.Text = detail.Name ?? "";
            ValNameChn.Text = detail.NameChn ?? "";
            ValDynasty.Text = $"{detail.Dynasty ?? ""} / {detail.DynastyChn ?? ""}";
            ValIndexYear.Text = detail.IndexYear?.ToString() ?? "";
            ValBirthDeath.Text = $"{detail.BirthYear?.ToString() ?? "?"} / {detail.DeathYear?.ToString() ?? "?"}";
            ValGender.Text = detail.Gender ?? "";
            ValIndexAddress.Text = $"{detail.IndexAddress ?? ""} / {detail.IndexAddressChn ?? ""}";

            ValAltNames.Text = detail.AltNameCount.ToString("N0");
            ValKin.Text = detail.KinCount.ToString("N0");
            ValAssoc.Text = detail.AssocCount.ToString("N0");

            TxtNotes.Text = string.Format(T("browser.search_result_count"), _people.Count);

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
            TxtFooter.Text = T("browser.loading_related");

            var rows = await Task.Run(
                () => _personBrowserService.GetRelatedItemsAsync(_sqlitePath, _selectedPersonId!.Value, category.Value, 1000)
            );
            var list = _tabItems[category.Value];

            list.Clear();
            foreach (var row in rows) {
                list.Add(row);
            }

            _loadedTabs.Add(category.Value);
            TxtFooter.Text = string.Format(T("browser.related_result_count"), rows.Count);
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        } finally {
            _isLoadingTab = false;
        }
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
        foreach (var item in _tabItems.Values) {
            item.Clear();
        }
    }

    private void UpdateFooterText() {
        TxtFooter.Text = _hasMore
            ? string.Format(T("browser.search_result_count_more"), _people.Count)
            : string.Format(T("browser.search_result_count"), _people.Count);
    }

    private void ClearDetail() {
        ValPersonId.Text = "";
        ValName.Text = "";
        ValNameChn.Text = "";
        ValDynasty.Text = "";
        ValIndexYear.Text = "";
        ValBirthDeath.Text = "";
        ValGender.Text = "";
        ValIndexAddress.Text = "";
        ValAltNames.Text = "";
        ValKin.Text = "";
        ValAssoc.Text = "";
        TxtNotes.Text = "";
    }
    private static string NormalizeSqlitePath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        return path.Trim().Trim('"');
    }

    private string T(string key) => _localizationService.Get(key);
}


