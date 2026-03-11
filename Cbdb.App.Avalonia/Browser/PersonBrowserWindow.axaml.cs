using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Browser;

public partial class PersonBrowserWindow : Window {
    private const int PageSize = 300;

    private readonly AppLocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService;
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
    private DataGrid _gridPeople = null!;
    private DataGridTextColumn _colPersonId = null!;
    private DataGridTextColumn _colNameChn = null!;
    private DataGridTextColumn _colNameRm = null!;
    private DataGridTextColumn _colIndexYear = null!;
    private DataGridTextColumn _colIndexAddress = null!;
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
    private TabControl _mainTabs = null!;
    private TabItem _tabBasic = null!;
    private TabItem _tabAddresses = null!;
    private TabItem _tabAltNames = null!;
    private TabItem _tabWritings = null!;
    private TabItem _tabPostings = null!;
    private TabItem _tabEntry = null!;
    private TabItem _tabEvents = null!;
    private TabItem _tabStatus = null!;
    private TabItem _tabKinship = null!;
    private TabItem _tabAssociations = null!;
    private TabItem _tabPossessions = null!;
    private TabItem _tabSources = null!;
    private TabItem _tabInstitutions = null!;
    private TextBlock _txtNoSelection = null!;
    private TextBlock _txtAddressesEmpty = null!;
    private StackPanel _addressesPanel = null!;
    private TextBlock _txtAltNamesEmpty = null!;
    private StackPanel _altNamesPanel = null!;
    private TextBlock _txtWritingsEmpty = null!;
    private StackPanel _writingsPanel = null!;
    private TextBlock _txtPostingsEmpty = null!;
    private StackPanel _postingsPanel = null!;
    private TextBlock _txtEntryEmpty = null!;
    private StackPanel _entryPanel = null!;
    private TextBlock _txtEventsEmpty = null!;
    private StackPanel _eventsPanel = null!;
    private TextBlock _txtStatusEmpty = null!;
    private StackPanel _statusPanel = null!;
    private TextBlock _txtKinshipEmpty = null!;
    private StackPanel _kinshipPanel = null!;
    private TextBlock _txtTabAssociationsPlaceholder = null!;
    private TextBlock _txtPossessionsEmpty = null!;
    private StackPanel _possessionsPanel = null!;
    private TextBlock _txtSourcesEmpty = null!;
    private StackPanel _sourcesPanel = null!;
    private TextBlock _txtInstitutionsEmpty = null!;
    private StackPanel _institutionsPanel = null!;
    private TextBlock _txtFooter = null!;
    private readonly List<ScrollViewer> _peopleScrollViewers = new();

    private string? _currentKeyword;
    private int _nextOffset;
    private bool _hasMore;
    private bool _isLoadingPage;
    private bool _pendingLoadMore;
    private int? _selectedPersonId;
    private PersonDetail? _currentDetail;
    private IReadOnlyList<PersonAddressItem> _currentAddresses = Array.Empty<PersonAddressItem>();
    private IReadOnlyList<PersonAltNameItem> _currentAltNames = Array.Empty<PersonAltNameItem>();
    private IReadOnlyList<PersonWritingItem> _currentWritings = Array.Empty<PersonWritingItem>();
    private IReadOnlyList<PersonPostingItem> _currentPostings = Array.Empty<PersonPostingItem>();
    private IReadOnlyList<PersonEntryItem> _currentEntries = Array.Empty<PersonEntryItem>();
    private IReadOnlyList<PersonEventItem> _currentEvents = Array.Empty<PersonEventItem>();
    private IReadOnlyList<PersonStatusItem> _currentStatuses = Array.Empty<PersonStatusItem>();
    private IReadOnlyList<PersonKinshipItem> _currentKinships = Array.Empty<PersonKinshipItem>();
    private IReadOnlyList<PersonPossessionItem> _currentPossessions = Array.Empty<PersonPossessionItem>();
    private IReadOnlyList<PersonSourceItem> _currentSources = Array.Empty<PersonSourceItem>();
    private IReadOnlyList<PersonInstitutionItem> _currentInstitutions = Array.Empty<PersonInstitutionItem>();
    private readonly HashSet<string> _loadedPersonTabs = new(StringComparer.OrdinalIgnoreCase);

    public PersonBrowserWindow() : this(string.Empty, new AppLocalizationService()) {
    }

    public PersonBrowserWindow(string sqlitePath, AppLocalizationService localizationService, IPersonBrowserService? personBrowserService = null) {
        _sqlitePath = NormalizeSqlitePath(sqlitePath);
        _localizationService = localizationService;
        _personBrowserService = personBrowserService ?? new SqlitePersonBrowserService();

        InitializeComponent();
        InitializeControls();

        _gridPeople.ItemsSource = _people;

        _localizationService.LanguageChanged += OnLanguageChanged;
        ApplyLocalization();

        Opened += async (_, _) => {
            Dispatcher.UIThread.Post(AttachPeopleScrollMonitor, DispatcherPriority.Loaded);
            await SearchAsync();
        };
        Closed += (_, _) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateRecordText();
        UpdateTabHeaders(_currentDetail);
    }

    private void ApplyLocalization() {
        Title = T("browser.window_title");
        _btnSearch.Content = T("browser.search");
        _btnClear.Content = T("browser.clear");
        _btnSaveToFile.Content = T("browser.save_to_file");
        _lblSearchByName.Text = T("browser.keyword_tooltip");
        _txtKeyword.Watermark = T("browser.keyword_tooltip");

        _colPersonId.Header = T("browser.grid_person_id");
        _colNameChn.Header = T("browser.grid_name_chn");
        _colNameRm.Header = T("browser.grid_name_rm");
        _colIndexYear.Header = T("browser.grid_index_year");
        _colIndexAddress.Header = T("browser.grid_index_address");

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
        UpdateTabHeaders(_currentDetail);
        _txtNoSelection.Text = _currentDetail is null ? T("browser.no_selection") : string.Empty;
        _txtAddressesEmpty.Text = _currentAddresses.Count == 0 ? T("browser.addresses_none") : string.Empty;
        RenderAddresses();
        _txtAltNamesEmpty.Text = _currentAltNames.Count == 0 ? T("browser.alt_names_none") : string.Empty;
        RenderAltNames();
        var placeholder = T("browser.tab_placeholder");
        _txtWritingsEmpty.Text = _currentWritings.Count == 0 ? T("browser.writings_none") : string.Empty;
        RenderWritings();
        _txtPostingsEmpty.Text = _currentPostings.Count == 0 ? T("browser.postings_none") : string.Empty;
        RenderPostings();
        _txtEntryEmpty.Text = _currentEntries.Count == 0 ? T("browser.entry_none") : string.Empty;
        RenderEntries();
        _txtEventsEmpty.Text = _currentEvents.Count == 0 ? T("browser.events_none") : string.Empty;
        RenderEvents();
        _txtStatusEmpty.Text = _currentStatuses.Count == 0 ? T("browser.status_none") : string.Empty;
        RenderStatuses();
        _txtKinshipEmpty.Text = _currentKinships.Count == 0 ? T("browser.kinship_none") : string.Empty;
        RenderKinships();
        _txtTabAssociationsPlaceholder.Text = placeholder;
        _txtPossessionsEmpty.Text = _currentPossessions.Count == 0 ? T("browser.possessions_none") : string.Empty;
        RenderPossessions();
        _txtSourcesEmpty.Text = _currentSources.Count == 0 ? T("browser.sources_none") : string.Empty;
        RenderSources();
        _txtInstitutionsEmpty.Text = _currentInstitutions.Count == 0 ? T("browser.institutions_none") : string.Empty;
        RenderInstitutions();
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

        _currentKeyword = string.IsNullOrWhiteSpace(_txtKeyword.Text) ? null : _txtKeyword.Text.Trim();
        _nextOffset = 0;
        _hasMore = true;

        try {
            _btnSearch.IsEnabled = false;
            _btnClear.IsEnabled = false;
            _txtFooter.Text = T("status.checking");

            _people.Clear();
            ClearDetail();
            await LoadNextPageAsync(selectFirstRowWhenAvailable: true);
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        } finally {
            _btnSearch.IsEnabled = true;
            _btnClear.IsEnabled = true;
        }
    }

    private async Task LoadNextPageAsync(bool selectFirstRowWhenAvailable = false) {
        if (_isLoadingPage || !_hasMore) {
            return;
        }

        try {
            _isLoadingPage = true;
            _txtFooter.Text = T("status.checking");

            var rows = await _personBrowserService.SearchAsync(_sqlitePath, _currentKeyword, PageSize, _nextOffset);
            foreach (var row in rows) {
                _people.Add(row);
            }

            _nextOffset += rows.Count;
            _hasMore = rows.Count == PageSize;

            UpdateRecordText();

            if (selectFirstRowWhenAvailable && _people.Count > 0) {
                _gridPeople.SelectedIndex = 0;
            }
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        } finally {
            _isLoadingPage = false;
        }
    }

    private async void GridPeople_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_gridPeople.SelectedItem is not PersonListItem selected) {
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
            ResetLazyTabs();
            await EnsureSelectedTabLoadedAsync();
            _txtNoSelection.Text = string.Empty;
            UpdateTabHeaders(detail);
            _txtFooter.Text = string.Format(T("browser.search_result_count"), _people.Count);
        } catch (Exception ex) {
            _txtFooter.Text = ex.Message;
        }
    }

    private void AttachPeopleScrollMonitor() {
        foreach (var viewer in _peopleScrollViewers) {
            viewer.ScrollChanged -= PeopleScrollViewer_ScrollChanged;
        }

        _peopleScrollViewers.Clear();

        foreach (var viewer in _gridPeople.GetVisualDescendants().OfType<ScrollViewer>()) {
            viewer.ScrollChanged += PeopleScrollViewer_ScrollChanged;
            _peopleScrollViewers.Add(viewer);
        }
    }

    private async void PeopleScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e) {
        if (sender is not ScrollViewer viewer || !_hasMore || _isLoadingPage) {
            return;
        }

        if (viewer.Extent.Height <= viewer.Viewport.Height) {
            return;
        }

        var remaining = viewer.Extent.Height - (viewer.Offset.Y + viewer.Viewport.Height);
        if (remaining <= 120) {
            await LoadNextPageAsync();
        }
    }

    private void GridPeople_LoadingRow(object? sender, DataGridRowEventArgs e) {
        if (!_hasMore || _isLoadingPage || _pendingLoadMore) {
            return;
        }

        if (e.Row.Index < _people.Count - 20) {
            return;
        }

        _pendingLoadMore = true;
        Dispatcher.UIThread.Post(
            async () => {
                try {
                    await LoadNextPageAsync();
                } finally {
                    _pendingLoadMore = false;
                }
            },
            DispatcherPriority.Background
        );
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

    private async Task LoadAddressesAsync(int personId) {
        try {
            _currentAddresses = await _personBrowserService.GetAddressesAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentAddresses = Array.Empty<PersonAddressItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtAddressesEmpty.Text = _currentAddresses.Count == 0 ? T("browser.addresses_none") : string.Empty;
        RenderAddresses();
    }

    private async Task LoadAltNamesAsync(int personId) {
        try {
            _currentAltNames = await _personBrowserService.GetAltNamesAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentAltNames = Array.Empty<PersonAltNameItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtAltNamesEmpty.Text = _currentAltNames.Count == 0 ? T("browser.alt_names_none") : string.Empty;
        RenderAltNames();
    }

    private async Task LoadWritingsAsync(int personId) {
        try {
            _currentWritings = await _personBrowserService.GetWritingsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentWritings = Array.Empty<PersonWritingItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtWritingsEmpty.Text = _currentWritings.Count == 0 ? T("browser.writings_none") : string.Empty;
        RenderWritings();
    }

    private async Task LoadPostingsAsync(int personId) {
        try {
            _currentPostings = await _personBrowserService.GetPostingsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentPostings = Array.Empty<PersonPostingItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtPostingsEmpty.Text = _currentPostings.Count == 0 ? T("browser.postings_none") : string.Empty;
        RenderPostings();
    }

    private async Task LoadEntriesAsync(int personId) {
        try {
            _currentEntries = await _personBrowserService.GetEntriesAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentEntries = Array.Empty<PersonEntryItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtEntryEmpty.Text = _currentEntries.Count == 0 ? T("browser.entry_none") : string.Empty;
        RenderEntries();
    }

    private async Task LoadStatusesAsync(int personId) {
        try {
            _currentStatuses = await _personBrowserService.GetStatusesAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentStatuses = Array.Empty<PersonStatusItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtStatusEmpty.Text = _currentStatuses.Count == 0 ? T("browser.status_none") : string.Empty;
        RenderStatuses();
    }

    private async Task LoadEventsAsync(int personId) {
        try {
            _currentEvents = await _personBrowserService.GetEventsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentEvents = Array.Empty<PersonEventItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtEventsEmpty.Text = _currentEvents.Count == 0 ? T("browser.events_none") : string.Empty;
        RenderEvents();
    }

    private async Task LoadKinshipsAsync(int personId) {
        try {
            _currentKinships = await _personBrowserService.GetKinshipsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentKinships = Array.Empty<PersonKinshipItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtKinshipEmpty.Text = _currentKinships.Count == 0 ? T("browser.kinship_none") : string.Empty;
        RenderKinships();
    }

    private async Task LoadPossessionsAsync(int personId) {
        try {
            _currentPossessions = await _personBrowserService.GetPossessionsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentPossessions = Array.Empty<PersonPossessionItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtPossessionsEmpty.Text = _currentPossessions.Count == 0 ? T("browser.possessions_none") : string.Empty;
        RenderPossessions();
    }

    private async Task LoadSourcesAsync(int personId) {
        try {
            _currentSources = await _personBrowserService.GetSourcesAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentSources = Array.Empty<PersonSourceItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtSourcesEmpty.Text = _currentSources.Count == 0 ? T("browser.sources_none") : string.Empty;
        RenderSources();
    }

    private async Task LoadInstitutionsAsync(int personId) {
        try {
            _currentInstitutions = await _personBrowserService.GetInstitutionsAsync(_sqlitePath, personId);
        } catch (Exception ex) {
            _currentInstitutions = Array.Empty<PersonInstitutionItem>();
            _txtFooter.Text = ex.Message;
        }

        _txtInstitutionsEmpty.Text = _currentInstitutions.Count == 0 ? T("browser.institutions_none") : string.Empty;
        RenderInstitutions();
    }

    private async void MainTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (!ReferenceEquals(sender, _mainTabs)) {
            return;
        }

        await EnsureSelectedTabLoadedAsync();
    }

    private async Task EnsureSelectedTabLoadedAsync() {
        if (!_selectedPersonId.HasValue || _currentDetail is null || _mainTabs.SelectedItem is not TabItem tab) {
            return;
        }

        var tabKey = GetLazyTabKey(tab);
        if (tabKey is null || _loadedPersonTabs.Contains(tabKey)) {
            return;
        }

        _txtFooter.Text = T("status.checking");

        switch (tabKey) {
            case "addresses":
                await LoadAddressesAsync(_selectedPersonId.Value);
                break;
            case "alt_names":
                await LoadAltNamesAsync(_selectedPersonId.Value);
                break;
            case "writings":
                await LoadWritingsAsync(_selectedPersonId.Value);
                break;
            case "postings":
                await LoadPostingsAsync(_selectedPersonId.Value);
                break;
            case "entry":
                await LoadEntriesAsync(_selectedPersonId.Value);
                break;
            case "events":
                await LoadEventsAsync(_selectedPersonId.Value);
                break;
            case "status":
                await LoadStatusesAsync(_selectedPersonId.Value);
                break;
            case "kinship":
                await LoadKinshipsAsync(_selectedPersonId.Value);
                break;
            case "possessions":
                await LoadPossessionsAsync(_selectedPersonId.Value);
                break;
            case "sources":
                await LoadSourcesAsync(_selectedPersonId.Value);
                break;
            case "institutions":
                await LoadInstitutionsAsync(_selectedPersonId.Value);
                break;
        }

        _loadedPersonTabs.Add(tabKey);
        _txtFooter.Text = string.Format(T("browser.search_result_count"), _people.Count);
    }

    private void ResetLazyTabs() {
        _loadedPersonTabs.Clear();
        _loadedPersonTabs.Add("basic");

        _currentAddresses = Array.Empty<PersonAddressItem>();
        _currentAltNames = Array.Empty<PersonAltNameItem>();
        _currentWritings = Array.Empty<PersonWritingItem>();
        _currentPostings = Array.Empty<PersonPostingItem>();
        _currentEntries = Array.Empty<PersonEntryItem>();
        _currentEvents = Array.Empty<PersonEventItem>();
        _currentStatuses = Array.Empty<PersonStatusItem>();
        _currentKinships = Array.Empty<PersonKinshipItem>();
        _currentPossessions = Array.Empty<PersonPossessionItem>();
        _currentSources = Array.Empty<PersonSourceItem>();
        _currentInstitutions = Array.Empty<PersonInstitutionItem>();

        _addressesPanel.Children.Clear();
        _altNamesPanel.Children.Clear();
        _writingsPanel.Children.Clear();
        _postingsPanel.Children.Clear();
        _entryPanel.Children.Clear();
        _eventsPanel.Children.Clear();
        _statusPanel.Children.Clear();
        _kinshipPanel.Children.Clear();
        _possessionsPanel.Children.Clear();
        _sourcesPanel.Children.Clear();
        _institutionsPanel.Children.Clear();

        _txtAddressesEmpty.Text = T("browser.addresses_none");
        _txtAltNamesEmpty.Text = T("browser.alt_names_none");
        _txtWritingsEmpty.Text = T("browser.writings_none");
        _txtPostingsEmpty.Text = T("browser.postings_none");
        _txtEntryEmpty.Text = T("browser.entry_none");
        _txtEventsEmpty.Text = T("browser.events_none");
        _txtStatusEmpty.Text = T("browser.status_none");
        _txtKinshipEmpty.Text = T("browser.kinship_none");
        _txtPossessionsEmpty.Text = T("browser.possessions_none");
        _txtSourcesEmpty.Text = T("browser.sources_none");
        _txtInstitutionsEmpty.Text = T("browser.institutions_none");
    }

    private string? GetLazyTabKey(TabItem tab) {
        if (ReferenceEquals(tab, _tabBasic)) {
            return "basic";
        }
        if (ReferenceEquals(tab, _tabAddresses)) {
            return "addresses";
        }
        if (ReferenceEquals(tab, _tabAltNames)) {
            return "alt_names";
        }
        if (ReferenceEquals(tab, _tabWritings)) {
            return "writings";
        }
        if (ReferenceEquals(tab, _tabPostings)) {
            return "postings";
        }
        if (ReferenceEquals(tab, _tabEntry)) {
            return "entry";
        }
        if (ReferenceEquals(tab, _tabEvents)) {
            return "events";
        }
        if (ReferenceEquals(tab, _tabStatus)) {
            return "status";
        }
        if (ReferenceEquals(tab, _tabKinship)) {
            return "kinship";
        }
        if (ReferenceEquals(tab, _tabPossessions)) {
            return "possessions";
        }
        if (ReferenceEquals(tab, _tabSources)) {
            return "sources";
        }
        if (ReferenceEquals(tab, _tabInstitutions)) {
            return "institutions";
        }

        return null;
    }

    private void RenderAddresses() {
        if (_addressesPanel is null) {
            return;
        }

        _addressesPanel.Children.Clear();
        foreach (var item in _currentAddresses) {
            _addressesPanel.Children.Add(BuildAddressCard(item));
        }
    }

    private void RenderAltNames() {
        if (_altNamesPanel is null) {
            return;
        }

        _altNamesPanel.Children.Clear();
        foreach (var item in _currentAltNames) {
            _altNamesPanel.Children.Add(BuildAltNameCard(item));
        }
    }

    private void RenderWritings() {
        if (_writingsPanel is null) {
            return;
        }

        _writingsPanel.Children.Clear();
        foreach (var item in _currentWritings) {
            _writingsPanel.Children.Add(BuildWritingCard(item));
        }
    }

    private void RenderPostings() {
        if (_postingsPanel is null) {
            return;
        }

        _postingsPanel.Children.Clear();
        foreach (var item in _currentPostings) {
            _postingsPanel.Children.Add(BuildPostingCard(item));
        }
    }

    private void RenderEntries() {
        if (_entryPanel is null) {
            return;
        }

        _entryPanel.Children.Clear();
        foreach (var item in _currentEntries) {
            _entryPanel.Children.Add(BuildEntryCard(item));
        }
    }

    private void RenderEvents() {
        if (_eventsPanel is null) {
            return;
        }

        _eventsPanel.Children.Clear();
        foreach (var item in _currentEvents) {
            _eventsPanel.Children.Add(BuildEventCard(item));
        }
    }

    private void RenderStatuses() {
        if (_statusPanel is null) {
            return;
        }

        _statusPanel.Children.Clear();
        foreach (var item in _currentStatuses) {
            _statusPanel.Children.Add(BuildStatusCard(item));
        }
    }

    private void RenderKinships() {
        if (_kinshipPanel is null) {
            return;
        }

        _kinshipPanel.Children.Clear();
        foreach (var item in _currentKinships) {
            _kinshipPanel.Children.Add(BuildKinshipCard(item));
        }
    }

    private void RenderPossessions() {
        if (_possessionsPanel is null) {
            return;
        }

        _possessionsPanel.Children.Clear();
        foreach (var item in _currentPossessions) {
            _possessionsPanel.Children.Add(BuildPossessionCard(item));
        }
    }

    private void RenderSources() {
        if (_sourcesPanel is null) {
            return;
        }

        _sourcesPanel.Children.Clear();
        foreach (var item in _currentSources) {
            _sourcesPanel.Children.Add(BuildSourceCard(item));
        }
    }

    private void RenderInstitutions() {
        if (_institutionsPanel is null) {
            return;
        }

        _institutionsPanel.Children.Clear();
        foreach (var item in _currentInstitutions) {
            _institutionsPanel.Children.Add(BuildInstitutionCard(item));
        }
    }

    private Control BuildAddressCard(PersonAddressItem item) {
        var root = new StackPanel {
            Spacing = 8
        };

        root.Children.Add(BuildAddressHeader(item));
        root.Children.Add(BuildAddressDateGrid(item));
        root.Children.Add(BuildAddressMetaGrid(item));

        return new Border {
            BorderBrush = Brush.Parse("#CFCFCF"),
            BorderThickness = new Thickness(1),
            Background = Brush.Parse("#FAFAFA"),
            Padding = new Thickness(10),
            Child = root
        };
    }

    private Control BuildAltNameCard(PersonAltNameItem item) {
        var root = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,100,Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
        };

        AddReadOnlyField(root, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(root, 0, 2, T("browser.alt_name_type"), item.NameType);
        AddReadOnlyField(root, 1, 0, T("browser.alt_name_chn"), item.AltNameChn, 1, 28, false);
        AddReadOnlyField(root, 1, 2, T("browser.alt_name"), item.AltName, 3, 28, false);
        AddReadOnlyField(root, 2, 0, T("browser.address_source"), item.Source, 3, 28, false);
        AddReadOnlyField(root, 2, 4, T("browser.address_pages"), item.Pages, 1, 28, false);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_notes"), item.Notes, 1, 64, true);

        var stack = new StackPanel { Spacing = 0 };
        stack.Children.Add(root);
        stack.Children.Add(notesGrid);

        return new Border {
            BorderBrush = Brush.Parse("#CFCFCF"),
            BorderThickness = new Thickness(1),
            Background = Brush.Parse("#FAFAFA"),
            Padding = new Thickness(10),
            Child = stack
        };
    }

    private Control BuildWritingCard(PersonWritingItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.writing_title_chn"), item.TitleChn);
        AddReadOnlyField(grid, 0, 2, T("browser.writing_title"), item.Title);
        AddReadOnlyField(grid, 1, 0, T("browser.writing_role"), item.Role);
        AddReadOnlyField(grid, 1, 2, "ID", item.TextId.ToString());
        AddReadOnlyField(grid, 2, 0, T("browser.writing_year"), item.Year?.ToString());
        AddReadOnlyField(grid, 2, 2, T("browser.writing_nianhao"), JoinCompactDate(item.Nianhao, item.NianhaoYear, item.Range));
        AddReadOnlyField(grid, 3, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 3, 2, T("browser.address_pages"), item.Pages);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_notes"), item.Notes, 1, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(notesGrid);

        return WrapCard(stack);
    }

    private Control BuildPostingCard(PersonPostingItem item) {
        var stack = new StackPanel {
            Spacing = 8
        };

        var header = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,120,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto")
        };
        AddReadOnlyField(header, 0, 0, T("browser.posting_id"), item.PostingId.ToString());
        AddReadOnlyField(header, 0, 2, T("browser.posting_office_count"), item.Offices.Count.ToString());
        stack.Children.Add(header);

        foreach (var office in item.Offices) {
            stack.Children.Add(BuildPostingOfficeCard(office));
        }

        return WrapCard(stack);
    }

    private Control BuildPostingOfficeCard(PersonPostingOfficeItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.posting_office"), JoinDisplay(item.OfficeNameChn, item.OfficeName));
        AddReadOnlyField(grid, 1, 0, T("browser.posting_appointment"), item.AppointmentType);
        AddReadOnlyField(grid, 1, 2, T("browser.posting_assume_office"), item.AssumeOffice);
        AddReadOnlyField(grid, 2, 0, T("browser.posting_category"), item.Category);
        AddReadOnlyField(grid, 2, 2, T("browser.entry_dynasty"), item.Dynasty);
        AddReadOnlyField(grid, 3, 0, T("browser.address_first"), FormatPostingDate(item, true), 1, 28, false);
        AddReadOnlyField(grid, 3, 2, T("browser.address_last"), FormatPostingDate(item, false), 1, 28, false);
        AddReadOnlyField(grid, 4, 0, T("browser.posting_addresses"), JoinPostingAddresses(item), 3, 28, false);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(notesGrid, 0, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(notesGrid, 1, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(notesGrid);

        var auditControl = BuildPostingAuditExpander(item);
        if (auditControl is not null) {
            stack.Children.Add(auditControl);
        }

        return new Border {
            BorderBrush = Brush.Parse("#DDDDDD"),
            BorderThickness = new Thickness(1),
            Background = Brush.Parse("#FFFFFF"),
            Padding = new Thickness(10),
            Child = stack
        };
    }

    private Control BuildEntryCard(PersonEntryItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,200,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.entry_method"), item.EntryMethod);
        AddReadOnlyField(grid, 1, 0, T("browser.entry_year"), item.Year?.ToString());
        AddReadOnlyField(grid, 1, 2, T("browser.entry_nianhao"), JoinCompactDate(item.Nianhao, item.NianhaoYear, item.Range, null));
        AddReadOnlyField(grid, 2, 0, T("browser.entry_exam_rank"), item.ExamRank);
        AddReadOnlyField(grid, 2, 2, T("browser.entry_age"), item.Age?.ToString());
        AddReadOnlyField(grid, 3, 0, T("browser.entry_dynasty"), item.Dynasty);
        AddReadOnlyField(grid, 3, 2, T("browser.entry_parental_status"), item.ParentalStatus);
        AddReadOnlyField(grid, 4, 0, T("browser.entry_kinship"), item.Kinship);
        AddReadOnlyField(grid, 4, 2, T("browser.entry_kin_person"), JoinDisplay(item.KinNameChn, item.KinName));
        AddReadOnlyField(grid, 5, 0, T("browser.entry_association"), item.Association);
        AddReadOnlyField(grid, 5, 2, T("browser.entry_associate_person"), JoinDisplay(item.AssociateNameChn, item.AssociateName));

        var lowerGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,200,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(lowerGrid, 0, 0, T("browser.entry_institution"), JoinDisplay(item.InstitutionNameChn, item.InstitutionName));
        AddReadOnlyField(lowerGrid, 0, 2, T("browser.entry_address"), JoinDisplay(item.EntryAddressChn, item.EntryAddress));
        AddReadOnlyField(lowerGrid, 1, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(lowerGrid, 1, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(lowerGrid, 2, 0, T("browser.entry_posting_notes"), item.PostingNotes, 3, 64, true);
        AddReadOnlyField(lowerGrid, 3, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(lowerGrid);

        return WrapCard(stack);
    }

    private Control BuildEventCard(PersonEventItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.event_name"), item.EventName);
        AddReadOnlyField(grid, 1, 0, T("browser.event_role"), item.Role);
        AddReadOnlyField(grid, 1, 2, T("browser.event_date"), FormatEventDate(item));
        AddReadOnlyField(grid, 2, 0, T("browser.event_address"), JoinDisplay(item.AddressNameChn, item.AddressName), 3, 28, false);
        AddReadOnlyField(grid, 3, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 3, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 4, 0, T("browser.event_text"), item.EventText, 3, 64, true);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_notes"), item.Notes, 1, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(notesGrid);

        return WrapCard(stack);
    }

    private Control BuildStatusCard(PersonStatusItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.status_name"), item.Status);
        AddReadOnlyField(grid, 1, 0, T("browser.address_first"), JoinCompactDate(item.FirstNianhao, item.FirstNianhaoYear, item.FirstRange, item.FirstYear));
        AddReadOnlyField(grid, 1, 2, T("browser.address_last"), JoinCompactDate(item.LastNianhao, item.LastNianhaoYear, item.LastRange, item.LastYear));
        AddReadOnlyField(grid, 2, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 2, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 3, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        return WrapCard(grid);
    }

    private Control BuildKinshipCard(PersonKinshipItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.kinship_relation"), item.Kinship);
        AddReadOnlyField(grid, 0, 2, T("browser.kinship_person"), JoinDisplay(item.KinNameChn, item.KinName));
        AddReadOnlyField(grid, 1, 0, T("browser.person_id"), item.KinPersonId.ToString());
        AddReadOnlyField(grid, 1, 2, T("browser.kinship_steps"), FormatKinshipSteps(item));
        AddReadOnlyField(grid, 2, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 2, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 3, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        return WrapCard(grid);
    }

    private Control BuildPossessionCard(PersonPossessionItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,200,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence?.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.possession_name"), item.Possession);
        AddReadOnlyField(grid, 1, 0, T("browser.possession_action"), item.PossessionAction);
        AddReadOnlyField(grid, 1, 2, T("browser.possession_quantity"), JoinDisplay(item.Quantity, item.Measure));
        AddReadOnlyField(grid, 2, 0, T("browser.possession_date"), JoinCompactDate(item.Nianhao, item.NianhaoYear, item.Range, item.Year));
        AddReadOnlyField(grid, 2, 2, T("browser.possession_address"), JoinDisplay(item.AddressNameChn, item.AddressName));
        AddReadOnlyField(grid, 3, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 3, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 4, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        return WrapCard(grid);
    }

    private Control BuildSourceCard(PersonSourceItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.source_title_chn"), item.TitleChn);
        AddReadOnlyField(grid, 0, 2, T("browser.source_title"), item.Title);
        AddReadOnlyField(grid, 1, 0, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 1, 2, T("browser.source_hyperlink"), item.Hyperlink);
        AddReadOnlyCheck(grid, 2, 0, T("browser.source_main"), item.MainSource);
        AddReadOnlyCheck(grid, 2, 2, T("browser.source_self_bio"), item.SelfBio);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_notes"), item.Notes, 1, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(notesGrid);

        return WrapCard(stack);
    }

    private Control BuildInstitutionCard(PersonInstitutionItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.institution_name_chn"), item.InstitutionNameChn);
        AddReadOnlyField(grid, 0, 2, T("browser.institution_name"), item.InstitutionName);
        AddReadOnlyField(grid, 1, 0, T("browser.institution_role"), item.Role);
        AddReadOnlyField(grid, 1, 2, T("browser.institution_place"), JoinDisplay(item.PlaceNameChn, item.PlaceName));
        AddReadOnlyField(grid, 2, 0, T("browser.institution_place_type"), item.PlaceType);
        AddReadOnlyField(grid, 2, 2, T("browser.institution_coords"), FormatCoords(item.XCoord, item.YCoord));
        AddReadOnlyField(grid, 3, 0, T("browser.institution_begin"), JoinCompactDate(item.BeginNianhao, item.BeginNianhaoYear, item.BeginRange, item.BeginYear));
        AddReadOnlyField(grid, 3, 2, T("browser.institution_end"), JoinCompactDate(item.EndNianhao, item.EndNianhaoYear, item.EndRange, item.EndYear));
        AddReadOnlyField(grid, 4, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(grid, 4, 2, T("browser.address_pages"), item.Pages);

        var notesGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(notesGrid, 0, 0, T("browser.address_notes"), item.Notes, 1, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(notesGrid);

        return WrapCard(stack);
    }

    private static Control WrapCard(Control child) {
        return new Border {
            BorderBrush = Brush.Parse("#CFCFCF"),
            BorderThickness = new Thickness(1),
            Background = Brush.Parse("#FAFAFA"),
            Padding = new Thickness(10),
            Child = child
        };
    }

    private void AddReadOnlyCheck(Grid grid, int row, int column, string label, bool? value) {
        var labelBlock = new TextBlock {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 2, 8, 6)
        };
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, column);
        grid.Children.Add(labelBlock);

        var check = new CheckBox {
            IsEnabled = false,
            IsChecked = value ?? false,
            Margin = new Thickness(0, 2, 12, 6)
        };
        Grid.SetRow(check, row);
        Grid.SetColumn(check, column + 1);
        grid.Children.Add(check);
    }

    private Control BuildAddressHeader(PersonAddressItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,90,Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.address_type"), item.AddressType);
        AddReadOnlyField(grid, 0, 4, T("browser.address_name"), JoinDisplay(item.AddressNameChn, item.AddressName));

        var label = new TextBlock {
            Text = T("browser.address_maternal"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 2, 8, 0)
        };
        Grid.SetRow(label, 1);
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        var check = new CheckBox {
            IsEnabled = false,
            IsChecked = item.Natal ?? false,
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetRow(check, 1);
        Grid.SetColumn(check, 1);
        grid.Children.Add(check);

        return grid;
    }

    private Control BuildAddressDateGrid(PersonAddressItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_first"), FormatAddressDate(item, true));
        AddReadOnlyField(grid, 1, 0, T("browser.address_last"), FormatAddressDate(item, false));
        return grid;
    }

    private Control BuildAddressMetaGrid(PersonAddressItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_source"), item.Source);
        AddReadOnlyField(grid, 0, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 1, 0, T("browser.address_notes"), item.Notes, 3, 64, multiline: true);
        return grid;
    }

    private void AddReadOnlyField(
        Grid grid,
        int row,
        int column,
        string label,
        string? value,
        int valueColumnSpan = 1,
        double minHeight = 28,
        bool multiline = false
    ) {
        var labelBlock = new TextBlock {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 2, 8, 6)
        };
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, column);
        grid.Children.Add(labelBlock);

        var valueBox = new TextBox {
            Text = value ?? string.Empty,
            IsReadOnly = true,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            AcceptsReturn = multiline,
            MinHeight = minHeight,
            MaxLines = multiline ? 0 : 1,
            Margin = new Thickness(0, 0, 12, 6)
        };
        Grid.SetRow(valueBox, row);
        Grid.SetColumn(valueBox, column + 1);
        Grid.SetColumnSpan(valueBox, valueColumnSpan);
        grid.Children.Add(valueBox);
    }

    private string FormatAddressDate(PersonAddressItem item, bool first) {
        var parts = new List<string>();
        var year = first ? item.FirstYear : item.LastYear;
        var nianhao = first ? item.FirstNianhao : item.LastNianhao;
        var nianhaoYear = first ? item.FirstNianhaoYear : item.LastNianhaoYear;
        var month = first ? item.FirstMonth : item.LastMonth;
        var intercalary = first ? item.FirstIntercalary : item.LastIntercalary;
        var day = first ? item.FirstDay : item.LastDay;
        var ganzhi = first ? item.FirstGanzhi : item.LastGanzhi;
        var range = first ? item.FirstRange : item.LastRange;

        if (year.HasValue) {
            parts.Add(year.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(nianhao)) {
            parts.Add(nianhao);
        }
        if (nianhaoYear.HasValue) {
            parts.Add(nianhaoYear.Value.ToString());
        }
        if (month.HasValue) {
            parts.Add(month.Value.ToString());
        }
        if (intercalary == true) {
            parts.Add(T("browser.address_intercalary"));
        }
        if (day.HasValue) {
            parts.Add(day.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(ganzhi)) {
            parts.Add(ganzhi);
        }
        if (!string.IsNullOrWhiteSpace(range)) {
            parts.Add(range);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private Control? BuildPostingAuditExpander(PersonPostingOfficeItem item) {
        var hasOfficeAudit =
            !string.IsNullOrWhiteSpace(item.CreatedBy) ||
            !string.IsNullOrWhiteSpace(item.CreatedDate) ||
            !string.IsNullOrWhiteSpace(item.ModifiedBy) ||
            !string.IsNullOrWhiteSpace(item.ModifiedDate);
        var hasAddressAudit = item.Addresses.Any(address =>
            !string.IsNullOrWhiteSpace(address.CreatedBy) ||
            !string.IsNullOrWhiteSpace(address.CreatedDate) ||
            !string.IsNullOrWhiteSpace(address.ModifiedBy) ||
            !string.IsNullOrWhiteSpace(address.ModifiedDate));

        if (!hasOfficeAudit && !hasAddressAudit) {
            return null;
        }

        var content = new StackPanel {
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0)
        };

        if (hasOfficeAudit) {
            var officeAudit = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
                RowDefinitions = new RowDefinitions("Auto")
            };
            AddReadOnlyField(officeAudit, 0, 0, T("browser.audit_created"), JoinDisplay(item.CreatedDate, item.CreatedBy));
            AddReadOnlyField(officeAudit, 0, 2, T("browser.audit_modified"), JoinDisplay(item.ModifiedDate, item.ModifiedBy));
            content.Children.Add(officeAudit);
        }

        foreach (var address in item.Addresses.Where(address =>
                     !string.IsNullOrWhiteSpace(address.CreatedBy) ||
                     !string.IsNullOrWhiteSpace(address.CreatedDate) ||
                     !string.IsNullOrWhiteSpace(address.ModifiedBy) ||
                     !string.IsNullOrWhiteSpace(address.ModifiedDate))) {
            var addressAudit = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
                RowDefinitions = new RowDefinitions("Auto,Auto")
            };
            AddReadOnlyField(addressAudit, 0, 0, T("browser.posting_address_audit"), JoinDisplay(address.AddressNameChn, address.AddressName), 3, 28, false);
            AddReadOnlyField(addressAudit, 1, 0, T("browser.audit_created"), JoinDisplay(address.CreatedDate, address.CreatedBy));
            AddReadOnlyField(addressAudit, 1, 2, T("browser.audit_modified"), JoinDisplay(address.ModifiedDate, address.ModifiedBy));
            content.Children.Add(addressAudit);
        }

        return new Expander {
            Header = T("browser.audit_details"),
            IsExpanded = false,
            Margin = new Thickness(0, 4, 0, 0),
            Content = content
        };
    }

    private string FormatEventDate(PersonEventItem item) {
        var parts = new List<string>();
        if (item.Year.HasValue) {
            parts.Add(item.Year.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(item.Nianhao)) {
            parts.Add(item.Nianhao);
        }
        if (item.NianhaoYear.HasValue) {
            parts.Add(item.NianhaoYear.Value.ToString());
        }
        if (item.Month.HasValue) {
            parts.Add(item.Month.Value.ToString());
        }
        if (item.Intercalary == true) {
            parts.Add(T("browser.address_intercalary"));
        }
        if (item.Day.HasValue) {
            parts.Add(item.Day.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(item.Ganzhi)) {
            parts.Add(item.Ganzhi);
        }
        if (!string.IsNullOrWhiteSpace(item.Range)) {
            parts.Add(item.Range);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private string FormatPostingDate(PersonPostingOfficeItem item, bool first) {
        var parts = new List<string>();
        var year = first ? item.FirstYear : item.LastYear;
        var nianhao = first ? item.FirstNianhao : item.LastNianhao;
        var nianhaoYear = first ? item.FirstNianhaoYear : item.LastNianhaoYear;
        var month = first ? item.FirstMonth : item.LastMonth;
        var intercalary = first ? item.FirstIntercalary : item.LastIntercalary;
        var day = first ? item.FirstDay : item.LastDay;
        var ganzhi = first ? item.FirstGanzhi : item.LastGanzhi;
        var range = first ? item.FirstRange : item.LastRange;

        if (year.HasValue) {
            parts.Add(year.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(nianhao)) {
            parts.Add(nianhao);
        }
        if (nianhaoYear.HasValue) {
            parts.Add(nianhaoYear.Value.ToString());
        }
        if (month.HasValue) {
            parts.Add(month.Value.ToString());
        }
        if (intercalary == true) {
            parts.Add(T("browser.address_intercalary"));
        }
        if (day.HasValue) {
            parts.Add(day.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(ganzhi)) {
            parts.Add(ganzhi);
        }
        if (!string.IsNullOrWhiteSpace(range)) {
            parts.Add(range);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private static string JoinPostingAddresses(PersonPostingOfficeItem item) {
        if (item.Addresses.Count == 0) {
            return string.Empty;
        }

        return string.Join(" ; ", item.Addresses
            .Select(address => JoinDisplay(address.AddressNameChn, address.AddressName))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private string FormatKinshipSteps(PersonKinshipItem item) {
        var parts = new List<string>();
        var upStep = item.UpStep.GetValueOrDefault();
        if (upStep > 0) {
            parts.Add($"{T("browser.kinship_up")} {upStep}");
        }
        var downStep = item.DownStep.GetValueOrDefault();
        if (downStep > 0) {
            parts.Add($"{T("browser.kinship_down")} {downStep}");
        }
        var marriageStep = item.MarriageStep.GetValueOrDefault();
        if (marriageStep > 0) {
            parts.Add($"{T("browser.kinship_marriage")} {marriageStep}");
        }
        var collateralStep = item.CollateralStep.GetValueOrDefault();
        if (collateralStep > 0) {
            parts.Add($"{T("browser.kinship_collateral")} {collateralStep}");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private static string JoinCompactDate(string? nianhao, int? nianhaoYear, string? range, int? year = null) {
        var parts = new List<string>();
        if (year.HasValue) {
            parts.Add(year.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(nianhao)) {
            parts.Add(nianhao);
        }
        if (nianhaoYear.HasValue) {
            parts.Add(nianhaoYear.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(range)) {
            parts.Add(range);
        }
        return parts.Count == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private static string FormatCoords(double? x, double? y) {
        if (!x.HasValue && !y.HasValue) {
            return string.Empty;
        }
        return $"{x?.ToString() ?? string.Empty}, {y?.ToString() ?? string.Empty}".Trim(' ', ',');
    }

    private void ClearDetail() {
        _selectedPersonId = null;
        _currentDetail = null;
        _loadedPersonTabs.Clear();

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
        ResetLazyTabs();
        _txtNoSelection.Text = T("browser.no_selection");
        UpdateTabHeaders(null);
        UpdateRecordText();
    }

    private void UpdateRecordText() {
        _txtRecord.Text = string.Format(T("browser.search_result_count"), _people.Count);
        _txtFooter.Text = string.Format(T("browser.search_result_count"), _people.Count);
    }

    private void UpdateTabHeaders(PersonDetail? detail) {
        _tabAddresses.Header = WithCount(T("browser.tab_addresses"), detail?.AddressCount);
        _tabAltNames.Header = WithCount(T("browser.tab_alt_names"), detail?.AltNameCount);
        _tabWritings.Header = WithCount(T("browser.tab_writings"), detail?.TextCount);
        _tabPostings.Header = WithCount(T("browser.tab_postings"), detail?.OfficeCount);
        _tabEntry.Header = WithCount(T("browser.tab_entry"), detail?.EntryCount);
        _tabEvents.Header = WithCount(T("browser.tab_events"), detail?.EventCount);
        _tabStatus.Header = WithCount(T("browser.tab_status"), detail?.StatusCount);
        _tabKinship.Header = WithCount(T("browser.tab_kinship"), detail?.KinCount);
        _tabAssociations.Header = WithCount(T("browser.tab_associations"), detail?.AssocCount);
        _tabPossessions.Header = WithCount(T("browser.tab_possessions"), detail?.PossessionCount);
        _tabSources.Header = WithCount(T("browser.tab_sources"), detail?.SourceCount);
        _tabInstitutions.Header = WithCount(T("browser.tab_institutions"), detail?.InstitutionCount);
    }

    private static string WithCount(string label, int? count) {
        return count.HasValue ? $"{label} ({count.Value:N0})" : label;
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
        _gridPeople = this.FindControl<DataGrid>("GridPeople") ?? throw new InvalidOperationException("GridPeople not found.");
        if (_gridPeople.Columns.Count < 5) {
            throw new InvalidOperationException("GridPeople columns are not initialized.");
        }

        _colPersonId = (DataGridTextColumn)_gridPeople.Columns[0];
        _colNameChn = (DataGridTextColumn)_gridPeople.Columns[1];
        _colNameRm = (DataGridTextColumn)_gridPeople.Columns[2];
        _colIndexYear = (DataGridTextColumn)_gridPeople.Columns[3];
        _colIndexAddress = (DataGridTextColumn)_gridPeople.Columns[4];
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
        _mainTabs = this.FindControl<TabControl>("MainTabs") ?? throw new InvalidOperationException("MainTabs not found.");
        _tabBasic = this.FindControl<TabItem>("TabBasic") ?? throw new InvalidOperationException("TabBasic not found.");
        _tabAddresses = this.FindControl<TabItem>("TabAddresses") ?? throw new InvalidOperationException("TabAddresses not found.");
        _tabAltNames = this.FindControl<TabItem>("TabAltNames") ?? throw new InvalidOperationException("TabAltNames not found.");
        _tabWritings = this.FindControl<TabItem>("TabWritings") ?? throw new InvalidOperationException("TabWritings not found.");
        _tabPostings = this.FindControl<TabItem>("TabPostings") ?? throw new InvalidOperationException("TabPostings not found.");
        _tabEntry = this.FindControl<TabItem>("TabEntry") ?? throw new InvalidOperationException("TabEntry not found.");
        _tabEvents = this.FindControl<TabItem>("TabEvents") ?? throw new InvalidOperationException("TabEvents not found.");
        _tabStatus = this.FindControl<TabItem>("TabStatus") ?? throw new InvalidOperationException("TabStatus not found.");
        _tabKinship = this.FindControl<TabItem>("TabKinship") ?? throw new InvalidOperationException("TabKinship not found.");
        _tabAssociations = this.FindControl<TabItem>("TabAssociations") ?? throw new InvalidOperationException("TabAssociations not found.");
        _tabPossessions = this.FindControl<TabItem>("TabPossessions") ?? throw new InvalidOperationException("TabPossessions not found.");
        _tabSources = this.FindControl<TabItem>("TabSources") ?? throw new InvalidOperationException("TabSources not found.");
        _tabInstitutions = this.FindControl<TabItem>("TabInstitutions") ?? throw new InvalidOperationException("TabInstitutions not found.");
        _txtNoSelection = this.FindControl<TextBlock>("TxtNoSelection") ?? throw new InvalidOperationException("TxtNoSelection not found.");
        _txtAddressesEmpty = this.FindControl<TextBlock>("TxtAddressesEmpty") ?? throw new InvalidOperationException("TxtAddressesEmpty not found.");
        _addressesPanel = this.FindControl<StackPanel>("AddressesPanel") ?? throw new InvalidOperationException("AddressesPanel not found.");
        _txtAltNamesEmpty = this.FindControl<TextBlock>("TxtAltNamesEmpty") ?? throw new InvalidOperationException("TxtAltNamesEmpty not found.");
        _altNamesPanel = this.FindControl<StackPanel>("AltNamesPanel") ?? throw new InvalidOperationException("AltNamesPanel not found.");
        _txtWritingsEmpty = this.FindControl<TextBlock>("TxtWritingsEmpty") ?? throw new InvalidOperationException("TxtWritingsEmpty not found.");
        _writingsPanel = this.FindControl<StackPanel>("WritingsPanel") ?? throw new InvalidOperationException("WritingsPanel not found.");
        _txtPostingsEmpty = this.FindControl<TextBlock>("TxtPostingsEmpty") ?? throw new InvalidOperationException("TxtPostingsEmpty not found.");
        _postingsPanel = this.FindControl<StackPanel>("PostingsPanel") ?? throw new InvalidOperationException("PostingsPanel not found.");
        _txtEntryEmpty = this.FindControl<TextBlock>("TxtEntryEmpty") ?? throw new InvalidOperationException("TxtEntryEmpty not found.");
        _entryPanel = this.FindControl<StackPanel>("EntryPanel") ?? throw new InvalidOperationException("EntryPanel not found.");
        _txtEventsEmpty = this.FindControl<TextBlock>("TxtEventsEmpty") ?? throw new InvalidOperationException("TxtEventsEmpty not found.");
        _eventsPanel = this.FindControl<StackPanel>("EventsPanel") ?? throw new InvalidOperationException("EventsPanel not found.");
        _txtStatusEmpty = this.FindControl<TextBlock>("TxtStatusEmpty") ?? throw new InvalidOperationException("TxtStatusEmpty not found.");
        _statusPanel = this.FindControl<StackPanel>("StatusPanel") ?? throw new InvalidOperationException("StatusPanel not found.");
        _txtKinshipEmpty = this.FindControl<TextBlock>("TxtKinshipEmpty") ?? throw new InvalidOperationException("TxtKinshipEmpty not found.");
        _kinshipPanel = this.FindControl<StackPanel>("KinshipPanel") ?? throw new InvalidOperationException("KinshipPanel not found.");
        _txtTabAssociationsPlaceholder = this.FindControl<TextBlock>("TxtTabAssociationsPlaceholder") ?? throw new InvalidOperationException("TxtTabAssociationsPlaceholder not found.");
        _txtPossessionsEmpty = this.FindControl<TextBlock>("TxtPossessionsEmpty") ?? throw new InvalidOperationException("TxtPossessionsEmpty not found.");
        _possessionsPanel = this.FindControl<StackPanel>("PossessionsPanel") ?? throw new InvalidOperationException("PossessionsPanel not found.");
        _txtSourcesEmpty = this.FindControl<TextBlock>("TxtSourcesEmpty") ?? throw new InvalidOperationException("TxtSourcesEmpty not found.");
        _sourcesPanel = this.FindControl<StackPanel>("SourcesPanel") ?? throw new InvalidOperationException("SourcesPanel not found.");
        _txtInstitutionsEmpty = this.FindControl<TextBlock>("TxtInstitutionsEmpty") ?? throw new InvalidOperationException("TxtInstitutionsEmpty not found.");
        _institutionsPanel = this.FindControl<StackPanel>("InstitutionsPanel") ?? throw new InvalidOperationException("InstitutionsPanel not found.");
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
