using System.Windows;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Desktop.Browser;

public partial class PersonBrowserWindow : Window {
    private readonly ILocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService = new SqlitePersonBrowserService();
    private readonly string _sqlitePath;

    public PersonBrowserWindow(string sqlitePath, ILocalizationService localizationService) {
        _sqlitePath = sqlitePath;
        _localizationService = localizationService;

        InitializeComponent();

        _localizationService.LanguageChanged += OnLanguageChanged;
        ApplyLocalization();

        Loaded += async (_, _) => await SearchAsync();
        Closed += (_, _) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = T("browser.window_title");
        BtnSearch.Content = T("browser.search");
        BtnClear.Content = T("browser.clear");
        TxtKeyword.ToolTip = T("browser.keyword_tooltip");

        TabBasic.Header = T("browser.tab_basic");
        TabStats.Header = T("browser.tab_counts");

        ColPersonId.Header = T("browser.grid_person_id");
        ColName.Header = T("browser.grid_name");
        ColNameChn.Header = T("browser.grid_name_chn");
        ColIndexYear.Header = T("browser.grid_index_year");

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
        LblOffice.Text = T("browser.count_office");
        LblEntry.Text = T("browser.count_entry");
        LblStatus.Text = T("browser.count_status");
        LblTexts.Text = T("browser.count_texts");

        TxtFooter.Text = T("status.ready");
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) {
        await SearchAsync();
    }

    private async void BtnClear_Click(object sender, RoutedEventArgs e) {
        TxtKeyword.Text = string.Empty;
        await SearchAsync();
    }

    private async Task SearchAsync() {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !System.IO.File.Exists(_sqlitePath)) {
            GridPeople.ItemsSource = Array.Empty<PersonListItem>();
            ClearDetail();
            TxtFooter.Text = T("browser.sqlite_missing");
            return;
        }

        try {
            BtnSearch.IsEnabled = false;
            TxtFooter.Text = T("status.checking");

            var rows = await _personBrowserService.SearchAsync(_sqlitePath, TxtKeyword.Text, 300);
            GridPeople.ItemsSource = rows;

            TxtFooter.Text = string.Format(T("browser.search_result_count"), rows.Count);

            if (rows.Count > 0) {
                GridPeople.SelectedIndex = 0;
            } else {
                ClearDetail();
            }
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        } finally {
            BtnSearch.IsEnabled = true;
        }
    }

    private async void GridPeople_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
        if (GridPeople.SelectedItem is not PersonListItem selected) {
            return;
        }

        try {
            var detail = await _personBrowserService.GetDetailAsync(_sqlitePath, selected.PersonId);
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
            ValOffice.Text = detail.OfficeCount.ToString("N0");
            ValEntry.Text = detail.EntryCount.ToString("N0");
            ValStatus.Text = detail.StatusCount.ToString("N0");
            ValTexts.Text = detail.TextCount.ToString("N0");
        } catch (Exception ex) {
            TxtFooter.Text = ex.Message;
        }
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
        ValOffice.Text = "";
        ValEntry.Text = "";
        ValStatus.Text = "";
        ValTexts.Text = "";
    }

    private string T(string key) => _localizationService.Get(key);
}

