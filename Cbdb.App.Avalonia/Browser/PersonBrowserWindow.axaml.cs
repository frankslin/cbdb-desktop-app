using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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
    private const int RelatedTabPageSize = 20;

    private readonly AppLocalizationService _localizationService;
    private readonly IPersonBrowserService _personBrowserService;
    private readonly ObservableCollection<PersonListItem> _people = new();
    private readonly string _sqlitePath;
    private readonly Dictionary<string, TextBlock> _basicGroupHeaders = new();
    private readonly Dictionary<string, TextBlock> _basicLabels = new();
    private readonly Dictionary<string, TextBox> _basicValues = new();
    private readonly Dictionary<string, CheckBox> _basicChecks = new();

    private TextBox _txtKeyword = null!;
    private Button _btnHistoryBack = null!;
    private Button _btnHistoryForward = null!;
    private Button _btnLoadFromFile = null!;
    private Button _btnSearch = null!;
    private Button _btnClear = null!;
    private Button _btnSaveToFile = null!;
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
    private Button _btnIndexYearSourceGoTo = null!;
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
    private Border _addressesLoadingHost = null!;
    private TextBlock _txtAddressesLoading = null!;
    private TextBlock _txtAddressesEmpty = null!;
    private StackPanel _addressesPanel = null!;
    private Border _altNamesLoadingHost = null!;
    private TextBlock _txtAltNamesLoading = null!;
    private TextBlock _txtAltNamesEmpty = null!;
    private StackPanel _altNamesPanel = null!;
    private Border _writingsLoadingHost = null!;
    private TextBlock _txtWritingsLoading = null!;
    private TextBlock _txtWritingsEmpty = null!;
    private StackPanel _writingsPanel = null!;
    private Border _postingsLoadingHost = null!;
    private TextBlock _txtPostingsLoading = null!;
    private TextBlock _txtPostingsEmpty = null!;
    private StackPanel _postingsPanel = null!;
    private Border _entryLoadingHost = null!;
    private TextBlock _txtEntryLoading = null!;
    private TextBlock _txtEntryEmpty = null!;
    private StackPanel _entryPanel = null!;
    private Border _eventsLoadingHost = null!;
    private TextBlock _txtEventsLoading = null!;
    private TextBlock _txtEventsEmpty = null!;
    private StackPanel _eventsPanel = null!;
    private Border _statusLoadingHost = null!;
    private TextBlock _txtStatusLoading = null!;
    private TextBlock _txtStatusEmpty = null!;
    private StackPanel _statusPanel = null!;
    private Border _kinshipLoadingHost = null!;
    private TextBlock _txtKinshipLoading = null!;
    private TextBlock _txtKinshipEmpty = null!;
    private CheckBox _chkExpandKinshipNetwork = null!;
    private StackPanel _kinshipPanel = null!;
    private Border _associationsLoadingHost = null!;
    private TextBlock _txtAssociationsLoading = null!;
    private TextBlock _txtAssociationsEmpty = null!;
    private StackPanel _associationsPanel = null!;
    private Border _possessionsLoadingHost = null!;
    private TextBlock _txtPossessionsLoading = null!;
    private TextBlock _txtPossessionsEmpty = null!;
    private StackPanel _possessionsPanel = null!;
    private Border _sourcesLoadingHost = null!;
    private TextBlock _txtSourcesLoading = null!;
    private TextBlock _txtSourcesEmpty = null!;
    private StackPanel _sourcesPanel = null!;
    private Border _institutionsLoadingHost = null!;
    private TextBlock _txtInstitutionsLoading = null!;
    private TextBlock _txtInstitutionsEmpty = null!;
    private StackPanel _institutionsPanel = null!;
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
    private IReadOnlyList<PersonAssociationItem> _currentAssociations = Array.Empty<PersonAssociationItem>();
    private IReadOnlyList<PersonPossessionItem> _currentPossessions = Array.Empty<PersonPossessionItem>();
    private IReadOnlyList<PersonSourceItem> _currentSources = Array.Empty<PersonSourceItem>();
    private IReadOnlyList<PersonInstitutionItem> _currentInstitutions = Array.Empty<PersonInstitutionItem>();
    private readonly HashSet<string> _loadedPersonTabs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _relatedTabPages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StackPanel> _tabPagerHosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _tabPrevButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _tabNextButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBlock> _tabPageLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PersonBrowserHistoryState> _personHistory = new();
    private int _personHistoryIndex = -1;
    private bool _suppressHistoryPush;
    private bool _isRestoringHistory;
    private bool _expandKinshipNetwork;

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
        ToolTip.SetTip(_btnHistoryBack, T("browser.history_back"));
        ToolTip.SetTip(_btnHistoryForward, T("browser.history_forward"));
        _btnLoadFromFile.Content = T("browser.load_from_file");
        _btnSearch.Content = T("browser.search");
        _btnClear.Content = T("browser.clear");
        _btnSaveToFile.Content = T("browser.save_to_file");
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
        _txtNoSelection.IsVisible = _currentDetail is null;
        var loadingText = T("status.checking");
        _txtAddressesLoading.Text = loadingText;
        _txtAddressesEmpty.Text = _currentAddresses.Count == 0 ? T("browser.addresses_none") : string.Empty;
        RenderAddresses();
        _txtAltNamesLoading.Text = loadingText;
        _txtAltNamesEmpty.Text = _currentAltNames.Count == 0 ? T("browser.alt_names_none") : string.Empty;
        RenderAltNames();
        _txtWritingsLoading.Text = loadingText;
        _txtWritingsEmpty.Text = _currentWritings.Count == 0 ? T("browser.writings_none") : string.Empty;
        RenderWritings();
        _txtPostingsLoading.Text = loadingText;
        _txtPostingsEmpty.Text = _currentPostings.Count == 0 ? T("browser.postings_none") : string.Empty;
        RenderPostings();
        _txtEntryLoading.Text = loadingText;
        _txtEntryEmpty.Text = _currentEntries.Count == 0 ? T("browser.entry_none") : string.Empty;
        RenderEntries();
        _txtEventsLoading.Text = loadingText;
        _txtEventsEmpty.Text = _currentEvents.Count == 0 ? T("browser.events_none") : string.Empty;
        RenderEvents();
        _txtStatusLoading.Text = loadingText;
        _txtStatusEmpty.Text = _currentStatuses.Count == 0 ? T("browser.status_none") : string.Empty;
        RenderStatuses();
        _txtKinshipLoading.Text = loadingText;
        _txtKinshipEmpty.Text = _currentKinships.Count == 0 ? T("browser.kinship_none") : string.Empty;
        _chkExpandKinshipNetwork.Content = T("browser.kinship_expand_network");
        _chkExpandKinshipNetwork.IsChecked = _expandKinshipNetwork;
        RenderKinships();
        _txtAssociationsLoading.Text = loadingText;
        _txtAssociationsEmpty.Text = _currentAssociations.Count == 0 ? T("browser.associations_none") : string.Empty;
        RenderAssociations();
        _txtPossessionsLoading.Text = loadingText;
        _txtPossessionsEmpty.Text = _currentPossessions.Count == 0 ? T("browser.possessions_none") : string.Empty;
        RenderPossessions();
        _txtSourcesLoading.Text = loadingText;
        _txtSourcesEmpty.Text = _currentSources.Count == 0 ? T("browser.sources_none") : string.Empty;
        RenderSources();
        _btnIndexYearSourceGoTo.Content = T("browser.jump_to_person");
        _txtInstitutionsLoading.Text = loadingText;
        _txtInstitutionsEmpty.Text = _currentInstitutions.Count == 0 ? T("browser.institutions_none") : string.Empty;
        RenderInstitutions();
        ApplyBasicInfoLocalization();
        UpdateHistoryButtons();

    }

    private async void BtnSearch_Click(object? sender, RoutedEventArgs e) {
        await SearchAsync();
    }

    private async void BtnLoadFromFile_Click(object? sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _txtRecord.Text = T("browser.no_selection");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = T("browser.load_from_file"),
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
            await LoadPeopleByIdsAsync(personIds, T("browser.no_valid_person_ids"));
        } catch (Exception ex) {
            _txtRecord.Text = ex.Message;
        }
    }

    public async Task LoadPeopleByIdsAsync(IReadOnlyList<int> personIds, string? emptyInputMessage = null) {
        if (personIds.Count == 0) {
            _people.Clear();
            _currentKeyword = null;
            _nextOffset = 0;
            _hasMore = false;
            ClearDetail();
            _txtRecord.Text = emptyInputMessage ?? T("browser.no_valid_person_ids");
            return;
        }

        try {
            _btnLoadFromFile.IsEnabled = false;
            _btnSearch.IsEnabled = false;
            _btnClear.IsEnabled = false;

            ReplaceCurrentHistoryState();
            var rows = await _personBrowserService.GetPeopleByIdsAsync(_sqlitePath, personIds);
            _people.Clear();
            foreach (var row in rows) {
                _people.Add(row);
            }

            _currentKeyword = null;
            _txtKeyword.Text = string.Empty;
            _nextOffset = _people.Count;
            _hasMore = false;
            ClearDetail();

            if (_people.Count > 0) {
                var first = _people[0];
                _isRestoringHistory = true;
                try {
                    _gridPeople.SelectedItem = first;
                    _gridPeople.ScrollIntoView(first, _colPersonId);
                } finally {
                    _isRestoringHistory = false;
                }

                var loaded = await LoadSelectedPersonAsync(first, suppressHistoryPush: true);
                if (loaded) {
                    PushCurrentHistoryState();
                } else {
                    ClearDetail();
                    UpdateRecordText();
                }
            } else {
                _txtRecord.Text = T("browser.no_matching_person_ids");
            }
        } finally {
            _btnLoadFromFile.IsEnabled = true;
            _btnSearch.IsEnabled = true;
            _btnClear.IsEnabled = true;
        }
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

    private async void BtnHistoryBack_Click(object? sender, RoutedEventArgs e) {
        await NavigateHistoryAsync(-1);
    }

    private async void BtnHistoryForward_Click(object? sender, RoutedEventArgs e) {
        await NavigateHistoryAsync(1);
    }

    private async Task SearchAsync() {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            _people.Clear();
            ClearDetail();
            return;
        }

        _currentKeyword = string.IsNullOrWhiteSpace(_txtKeyword.Text) ? null : _txtKeyword.Text.Trim();
        _nextOffset = 0;
        _hasMore = true;

        try {
            _btnSearch.IsEnabled = false;
            _btnClear.IsEnabled = false;
            _people.Clear();
            ClearDetail();
            await LoadNextPageAsync(selectFirstRowWhenAvailable: true);
        } catch (Exception ex) {
            _txtRecord.Text = ex.Message;
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
            _txtRecord.Text = ex.Message;
        } finally {
            _isLoadingPage = false;
        }
    }

    private async void GridPeople_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_isRestoringHistory) {
            return;
        }

        if (_gridPeople.SelectedItem is not PersonListItem selected) {
            ClearDetail();
            return;
        }

        try {
            await LoadSelectedPersonAsync(selected, suppressHistoryPush: false);
        } catch (Exception ex) {
            _txtRecord.Text = ex.Message;
        }
    }

    private async Task<bool> LoadSelectedPersonAsync(PersonListItem selected, bool suppressHistoryPush) {
        _selectedPersonId = selected.PersonId;

        var previousSuppress = _suppressHistoryPush;
        _suppressHistoryPush = suppressHistoryPush;
        try {
            var detail = await _personBrowserService.GetDetailAsync(_sqlitePath, selected.PersonId);
            if (detail is null) {
                ClearDetail();
                return false;
            }

            DisplayDetail(detail);
            await EnsureSelectedTabLoadedAsync();
            if (!suppressHistoryPush) {
                RecordHistory(selected.PersonId);
            }
            return true;
        } finally {
            _suppressHistoryPush = previousSuppress;
        }
    }

    private void DisplayDetail(PersonDetail detail) {
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
        ConfigurePersonJumpButton(_btnIndexYearSourceGoTo, TryParseLeadingPersonId(detail.IndexYearSource));
        _valIndexAddress.Text = JoinDisplay(detail.IndexAddressChn, detail.IndexAddress);
        _valIndexAddressType.Text = detail.IndexAddressType ?? string.Empty;

        PopulateBasicInfo(detail);
        ResetLazyTabs();
        _txtNoSelection.Text = string.Empty;
        _txtNoSelection.IsVisible = false;
        UpdateTabHeaders(detail);
    }

    private async Task NavigateHistoryAsync(int delta) {
        ReplaceCurrentHistoryState();

        var targetIndex = _personHistoryIndex + delta;
        if (targetIndex < 0 || targetIndex >= _personHistory.Count) {
            return;
        }

        _personHistoryIndex = targetIndex;
        UpdateHistoryButtons();
        await RestoreHistoryStateAsync(_personHistory[targetIndex]);
    }

    private async Task NavigateToPersonAsync(int personId, bool suppressHistoryPush) {
        var previousSuppress = _suppressHistoryPush;
        _suppressHistoryPush = suppressHistoryPush;

        try {
            _txtKeyword.Text = personId.ToString();
            await SearchAsync();
        } finally {
            _suppressHistoryPush = previousSuppress;
        }
    }

    private void RecordHistory(int personId) {
        if (_suppressHistoryPush) {
            ReplaceCurrentHistoryState();
            return;
        }

        if (_personHistoryIndex >= 0 &&
            _personHistoryIndex < _personHistory.Count &&
            _personHistory[_personHistoryIndex].SelectedPersonId == personId) {
            ReplaceCurrentHistoryState();
            return;
        }

        if (_personHistoryIndex < _personHistory.Count - 1) {
            _personHistory.RemoveRange(_personHistoryIndex + 1, _personHistory.Count - (_personHistoryIndex + 1));
        }

        _personHistory.Add(CaptureCurrentState());
        _personHistoryIndex = _personHistory.Count - 1;
        UpdateHistoryButtons();
    }

    private void PushCurrentHistoryState() {
        if (_personHistoryIndex < _personHistory.Count - 1) {
            _personHistory.RemoveRange(_personHistoryIndex + 1, _personHistory.Count - (_personHistoryIndex + 1));
        }

        _personHistory.Add(CaptureCurrentState());
        _personHistoryIndex = _personHistory.Count - 1;
        UpdateHistoryButtons();
    }

    private void ReplaceCurrentHistoryState() {
        if (_isRestoringHistory) {
            return;
        }

        if (_personHistoryIndex < 0 || _personHistoryIndex >= _personHistory.Count) {
            return;
        }

        _personHistory[_personHistoryIndex] = CaptureCurrentState();
        UpdateHistoryButtons();
    }

    private PersonBrowserHistoryState CaptureCurrentState() {
        return new PersonBrowserHistoryState(
            Keyword: _currentKeyword,
            SearchResults: _people.ToList(),
            NextOffset: _nextOffset,
            HasMore: _hasMore,
            SelectedResultIndex: GetSelectedResultIndex(),
            SelectedPersonId: _selectedPersonId,
            SelectedTabKey: GetSelectedTabKey(),
            RelatedTabPages: new Dictionary<string, int>(_relatedTabPages, StringComparer.OrdinalIgnoreCase),
            LoadedTabs: new HashSet<string>(_loadedPersonTabs, StringComparer.OrdinalIgnoreCase),
            Detail: _currentDetail,
            Addresses: _currentAddresses.ToList(),
            AltNames: _currentAltNames.ToList(),
            Writings: _currentWritings.ToList(),
            Postings: _currentPostings.ToList(),
            Entries: _currentEntries.ToList(),
            Events: _currentEvents.ToList(),
            Statuses: _currentStatuses.ToList(),
            Kinships: _currentKinships.ToList(),
            ExpandKinshipNetwork: _expandKinshipNetwork,
            Associations: _currentAssociations.ToList(),
            Possessions: _currentPossessions.ToList(),
            Sources: _currentSources.ToList(),
            Institutions: _currentInstitutions.ToList()
        );
    }

    private async Task RestoreHistoryStateAsync(PersonBrowserHistoryState state) {
        _isRestoringHistory = true;
        try {
            _txtKeyword.Text = state.Keyword ?? string.Empty;
            _currentKeyword = state.Keyword;
            _nextOffset = state.NextOffset;
            _hasMore = state.HasMore;

            _people.Clear();
            foreach (var item in state.SearchResults) {
                _people.Add(item);
            }
            UpdateRecordText();

            if (state.Detail is null || !state.SelectedPersonId.HasValue) {
                ClearDetail();
                return;
            }

            _selectedPersonId = state.SelectedPersonId;
            DisplayDetail(state.Detail);

            _relatedTabPages.Clear();
            foreach (var entry in state.RelatedTabPages) {
                _relatedTabPages[entry.Key] = entry.Value;
            }

            _loadedPersonTabs.Clear();
            foreach (var tabKey in state.LoadedTabs) {
                _loadedPersonTabs.Add(tabKey);
            }

            _currentAddresses = state.Addresses;
            _currentAltNames = state.AltNames;
            _currentWritings = state.Writings;
            _currentPostings = state.Postings;
            _currentEntries = state.Entries;
            _currentEvents = state.Events;
            _currentStatuses = state.Statuses;
            _currentKinships = state.Kinships;
            _expandKinshipNetwork = state.ExpandKinshipNetwork;
            _chkExpandKinshipNetwork.IsChecked = _expandKinshipNetwork;
            _currentAssociations = state.Associations;
            _currentPossessions = state.Possessions;
            _currentSources = state.Sources;
            _currentInstitutions = state.Institutions;

            if (state.LoadedTabs.Contains("addresses")) {
                RenderAddresses();
            }
            if (state.LoadedTabs.Contains("alt_names")) {
                RenderAltNames();
            }
            if (state.LoadedTabs.Contains("writings")) {
                RenderWritings();
            }
            if (state.LoadedTabs.Contains("postings")) {
                RenderPostings();
            }
            if (state.LoadedTabs.Contains("entry")) {
                RenderEntries();
            }
            if (state.LoadedTabs.Contains("events")) {
                RenderEvents();
            }
            if (state.LoadedTabs.Contains("status")) {
                RenderStatuses();
            }
            if (state.LoadedTabs.Contains("kinship")) {
                RenderKinships();
            }
            if (state.LoadedTabs.Contains("associations")) {
                RenderAssociations();
            }
            if (state.LoadedTabs.Contains("possessions")) {
                RenderPossessions();
            }
            if (state.LoadedTabs.Contains("sources")) {
                RenderSources();
            }
            if (state.LoadedTabs.Contains("institutions")) {
                RenderInstitutions();
            }

            SelectTabByKey(state.SelectedTabKey);

            if (state.SelectedResultIndex >= 0 && state.SelectedResultIndex < _people.Count) {
                _gridPeople.ScrollIntoView(_people[state.SelectedResultIndex], _colPersonId);
            }

            var selectedItem = _people.FirstOrDefault(item => item.PersonId == state.SelectedPersonId.Value);
            _gridPeople.SelectedItem = selectedItem;
            await EnsureSelectedTabLoadedAsync();
        } finally {
            _isRestoringHistory = false;
            UpdateHistoryButtons();
        }
    }

    private void UpdateHistoryButtons() {
        _btnHistoryBack.IsEnabled = _personHistoryIndex > 0;
        _btnHistoryForward.IsEnabled = _personHistoryIndex >= 0 && _personHistoryIndex < _personHistory.Count - 1;
    }

    private int GetSelectedResultIndex() {
        return _gridPeople.SelectedItem is PersonListItem selected
            ? _people.IndexOf(selected)
            : -1;
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
        if (_isRestoringHistory) {
            return;
        }

        if (sender is not ScrollViewer viewer || !_hasMore || _isLoadingPage) {
            ReplaceCurrentHistoryState();
            return;
        }

        if (viewer.Extent.Height <= viewer.Viewport.Height) {
            ReplaceCurrentHistoryState();
            return;
        }

        var remaining = viewer.Extent.Height - (viewer.Offset.Y + viewer.Viewport.Height);
        if (remaining <= 120) {
            await LoadNextPageAsync();
        }

        ReplaceCurrentHistoryState();
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
            _txtRecord.Text = T("browser.no_data_to_export");
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
            _txtRecord.Text = path;
        } catch (Exception ex) {
            _txtRecord.Text = ex.Message;
        }
    }

    private async Task LoadAddressesAsync(int personId) {
        await PrepareTabLoadAsync("addresses");
        try {
            _currentAddresses = await Task.Run(() => _personBrowserService.GetAddressesAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentAddresses = Array.Empty<PersonAddressItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("addresses", false);
        }

        _txtAddressesEmpty.Text = _currentAddresses.Count == 0 ? T("browser.addresses_none") : string.Empty;
        _relatedTabPages["addresses"] = 0;
        RenderAddresses();
    }

    private async Task LoadAltNamesAsync(int personId) {
        await PrepareTabLoadAsync("alt_names");
        try {
            _currentAltNames = await Task.Run(() => _personBrowserService.GetAltNamesAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentAltNames = Array.Empty<PersonAltNameItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("alt_names", false);
        }

        _txtAltNamesEmpty.Text = _currentAltNames.Count == 0 ? T("browser.alt_names_none") : string.Empty;
        _relatedTabPages["alt_names"] = 0;
        RenderAltNames();
    }

    private async Task LoadWritingsAsync(int personId) {
        await PrepareTabLoadAsync("writings");
        try {
            _currentWritings = await Task.Run(() => _personBrowserService.GetWritingsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentWritings = Array.Empty<PersonWritingItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("writings", false);
        }

        _txtWritingsEmpty.Text = _currentWritings.Count == 0 ? T("browser.writings_none") : string.Empty;
        _relatedTabPages["writings"] = 0;
        RenderWritings();
    }

    private async Task LoadPostingsAsync(int personId) {
        await PrepareTabLoadAsync("postings");
        try {
            _currentPostings = await Task.Run(() => _personBrowserService.GetPostingsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentPostings = Array.Empty<PersonPostingItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("postings", false);
        }

        _txtPostingsEmpty.Text = _currentPostings.Count == 0 ? T("browser.postings_none") : string.Empty;
        _relatedTabPages["postings"] = 0;
        RenderPostings();
    }

    private async Task LoadEntriesAsync(int personId) {
        await PrepareTabLoadAsync("entry");
        try {
            _currentEntries = await Task.Run(() => _personBrowserService.GetEntriesAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentEntries = Array.Empty<PersonEntryItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("entry", false);
        }

        _txtEntryEmpty.Text = _currentEntries.Count == 0 ? T("browser.entry_none") : string.Empty;
        _relatedTabPages["entry"] = 0;
        RenderEntries();
    }

    private async Task LoadStatusesAsync(int personId) {
        await PrepareTabLoadAsync("status");
        try {
            _currentStatuses = await Task.Run(() => _personBrowserService.GetStatusesAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentStatuses = Array.Empty<PersonStatusItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("status", false);
        }

        _txtStatusEmpty.Text = _currentStatuses.Count == 0 ? T("browser.status_none") : string.Empty;
        _relatedTabPages["status"] = 0;
        RenderStatuses();
    }

    private async Task LoadEventsAsync(int personId) {
        await PrepareTabLoadAsync("events");
        try {
            _currentEvents = await Task.Run(() => _personBrowserService.GetEventsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentEvents = Array.Empty<PersonEventItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("events", false);
        }

        _txtEventsEmpty.Text = _currentEvents.Count == 0 ? T("browser.events_none") : string.Empty;
        _relatedTabPages["events"] = 0;
        RenderEvents();
    }

    private async Task LoadKinshipsAsync(int personId) {
        await LoadKinshipsAsync(personId, _expandKinshipNetwork);
    }

    private async Task LoadKinshipsAsync(int personId, bool expandNetwork) {
        await PrepareTabLoadAsync("kinship");
        try {
            _currentKinships = await Task.Run(() => _personBrowserService.GetKinshipsAsync(_sqlitePath, personId, expandNetwork));
        } catch (Exception ex) {
            _currentKinships = Array.Empty<PersonKinshipItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("kinship", false);
        }

        _txtKinshipEmpty.Text = _currentKinships.Count == 0 ? T("browser.kinship_none") : string.Empty;
        _relatedTabPages["kinship"] = 0;
        RenderKinships();
    }

    private async Task LoadAssociationsAsync(int personId) {
        await PrepareTabLoadAsync("associations");
        try {
            _currentAssociations = await Task.Run(() => _personBrowserService.GetAssociationsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentAssociations = Array.Empty<PersonAssociationItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("associations", false);
        }

        _txtAssociationsEmpty.Text = _currentAssociations.Count == 0 ? T("browser.associations_none") : string.Empty;
        _relatedTabPages["associations"] = 0;
        RenderAssociations();
    }

    private async Task LoadPossessionsAsync(int personId) {
        await PrepareTabLoadAsync("possessions");
        try {
            _currentPossessions = await Task.Run(() => _personBrowserService.GetPossessionsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentPossessions = Array.Empty<PersonPossessionItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("possessions", false);
        }

        _txtPossessionsEmpty.Text = _currentPossessions.Count == 0 ? T("browser.possessions_none") : string.Empty;
        _relatedTabPages["possessions"] = 0;
        RenderPossessions();
    }

    private async Task LoadSourcesAsync(int personId) {
        await PrepareTabLoadAsync("sources");
        try {
            _currentSources = await Task.Run(() => _personBrowserService.GetSourcesAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentSources = Array.Empty<PersonSourceItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("sources", false);
        }

        _txtSourcesEmpty.Text = _currentSources.Count == 0 ? T("browser.sources_none") : string.Empty;
        _relatedTabPages["sources"] = 0;
        RenderSources();
    }

    private async Task LoadInstitutionsAsync(int personId) {
        await PrepareTabLoadAsync("institutions");
        try {
            _currentInstitutions = await Task.Run(() => _personBrowserService.GetInstitutionsAsync(_sqlitePath, personId));
        } catch (Exception ex) {
            _currentInstitutions = Array.Empty<PersonInstitutionItem>();
            _txtRecord.Text = ex.Message;
        } finally {
            SetTabLoadingState("institutions", false);
        }

        _txtInstitutionsEmpty.Text = _currentInstitutions.Count == 0 ? T("browser.institutions_none") : string.Empty;
        _relatedTabPages["institutions"] = 0;
        RenderInstitutions();
    }

    private async void MainTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (!ReferenceEquals(sender, _mainTabs)) {
            return;
        }

        await EnsureSelectedTabLoadedAsync();
        ReplaceCurrentHistoryState();
    }

    private async Task EnsureSelectedTabLoadedAsync() {
        if (!_selectedPersonId.HasValue || _currentDetail is null || _mainTabs.SelectedItem is not TabItem tab) {
            return;
        }

        var tabKey = GetLazyTabKey(tab);
        if (tabKey is null || _loadedPersonTabs.Contains(tabKey)) {
            return;
        }

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
                await LoadKinshipsAsync(_selectedPersonId.Value, _expandKinshipNetwork);
                break;
            case "associations":
                await LoadAssociationsAsync(_selectedPersonId.Value);
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
    }

    private void ResetLazyTabs() {
        _loadedPersonTabs.Clear();
        _loadedPersonTabs.Add("basic");
        _relatedTabPages.Clear();

        _currentAddresses = Array.Empty<PersonAddressItem>();
        _currentAltNames = Array.Empty<PersonAltNameItem>();
        _currentWritings = Array.Empty<PersonWritingItem>();
        _currentPostings = Array.Empty<PersonPostingItem>();
        _currentEntries = Array.Empty<PersonEntryItem>();
        _currentEvents = Array.Empty<PersonEventItem>();
        _currentStatuses = Array.Empty<PersonStatusItem>();
        _currentKinships = Array.Empty<PersonKinshipItem>();
        _currentAssociations = Array.Empty<PersonAssociationItem>();
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
        _associationsPanel.Children.Clear();
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
        _txtAssociationsEmpty.Text = T("browser.associations_none");
        _txtPossessionsEmpty.Text = T("browser.possessions_none");
        _txtSourcesEmpty.Text = T("browser.sources_none");
        _txtInstitutionsEmpty.Text = T("browser.institutions_none");
        SetTabLoadingState("addresses", false);
        SetTabLoadingState("alt_names", false);
        SetTabLoadingState("writings", false);
        SetTabLoadingState("postings", false);
        SetTabLoadingState("entry", false);
        SetTabLoadingState("events", false);
        SetTabLoadingState("status", false);
        SetTabLoadingState("kinship", false);
        SetTabLoadingState("associations", false);
        SetTabLoadingState("possessions", false);
        SetTabLoadingState("sources", false);
        SetTabLoadingState("institutions", false);
    }

    private async Task PrepareTabLoadAsync(string tabKey) {
        SetTabLoadingState(tabKey, true);
        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
    }

    private void SetTabLoadingState(string tabKey, bool isLoading) {
        var (host, empty, panel) = GetTabLoadingTargets(tabKey);
        if (host is null || empty is null || panel is null) {
            return;
        }

        host.IsVisible = isLoading;
        if (isLoading) {
            empty.IsVisible = false;
            panel.IsVisible = false;
        } else {
            empty.IsVisible = true;
            panel.IsVisible = true;
        }
    }

    private (Border? Host, TextBlock? Empty, StackPanel? Panel) GetTabLoadingTargets(string tabKey) {
        return tabKey switch {
            "addresses" => (_addressesLoadingHost, _txtAddressesEmpty, _addressesPanel),
            "alt_names" => (_altNamesLoadingHost, _txtAltNamesEmpty, _altNamesPanel),
            "writings" => (_writingsLoadingHost, _txtWritingsEmpty, _writingsPanel),
            "postings" => (_postingsLoadingHost, _txtPostingsEmpty, _postingsPanel),
            "entry" => (_entryLoadingHost, _txtEntryEmpty, _entryPanel),
            "events" => (_eventsLoadingHost, _txtEventsEmpty, _eventsPanel),
            "status" => (_statusLoadingHost, _txtStatusEmpty, _statusPanel),
            "kinship" => (_kinshipLoadingHost, _txtKinshipEmpty, _kinshipPanel),
            "associations" => (_associationsLoadingHost, _txtAssociationsEmpty, _associationsPanel),
            "possessions" => (_possessionsLoadingHost, _txtPossessionsEmpty, _possessionsPanel),
            "sources" => (_sourcesLoadingHost, _txtSourcesEmpty, _sourcesPanel),
            "institutions" => (_institutionsLoadingHost, _txtInstitutionsEmpty, _institutionsPanel),
            _ => (null, null, null)
        };
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
        if (ReferenceEquals(tab, _tabAssociations)) {
            return "associations";
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

    private string? GetSelectedTabKey() {
        return _mainTabs.SelectedItem is TabItem tab ? GetLazyTabKey(tab) : null;
    }

    private void SelectTabByKey(string? tabKey) {
        _mainTabs.SelectedItem = tabKey switch {
            "addresses" => _tabAddresses,
            "alt_names" => _tabAltNames,
            "writings" => _tabWritings,
            "postings" => _tabPostings,
            "entry" => _tabEntry,
            "events" => _tabEvents,
            "status" => _tabStatus,
            "kinship" => _tabKinship,
            "associations" => _tabAssociations,
            "possessions" => _tabPossessions,
            "sources" => _tabSources,
            "institutions" => _tabInstitutions,
            _ => _tabBasic
        };
    }

    private void RenderAddresses() {
        if (_addressesPanel is null) {
            return;
        }

        _addressesPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentAddresses, "addresses")) {
            _addressesPanel.Children.Add(BuildAddressCard(item));
        }
        UpdateEmptyState(_txtAddressesEmpty, _currentAddresses.Count == 0, T("browser.addresses_none"));
        UpdateTabPager("addresses", _currentAddresses.Count);
    }

    private void RenderAltNames() {
        if (_altNamesPanel is null) {
            return;
        }

        _altNamesPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentAltNames, "alt_names")) {
            _altNamesPanel.Children.Add(BuildAltNameCard(item));
        }
        UpdateEmptyState(_txtAltNamesEmpty, _currentAltNames.Count == 0, T("browser.alt_names_none"));
        UpdateTabPager("alt_names", _currentAltNames.Count);
    }

    private void RenderWritings() {
        if (_writingsPanel is null) {
            return;
        }

        _writingsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentWritings, "writings")) {
            _writingsPanel.Children.Add(BuildWritingCard(item));
        }
        UpdateEmptyState(_txtWritingsEmpty, _currentWritings.Count == 0, T("browser.writings_none"));
        UpdateTabPager("writings", _currentWritings.Count);
    }

    private void RenderPostings() {
        if (_postingsPanel is null) {
            return;
        }

        _postingsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentPostings, "postings")) {
            _postingsPanel.Children.Add(BuildPostingCard(item));
        }
        UpdateEmptyState(_txtPostingsEmpty, _currentPostings.Count == 0, T("browser.postings_none"));
        UpdateTabPager("postings", _currentPostings.Count);
    }

    private void RenderEntries() {
        if (_entryPanel is null) {
            return;
        }

        _entryPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentEntries, "entry")) {
            _entryPanel.Children.Add(BuildEntryCard(item));
        }
        UpdateEmptyState(_txtEntryEmpty, _currentEntries.Count == 0, T("browser.entry_none"));
        UpdateTabPager("entry", _currentEntries.Count);
    }

    private void RenderEvents() {
        if (_eventsPanel is null) {
            return;
        }

        _eventsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentEvents, "events")) {
            _eventsPanel.Children.Add(BuildEventCard(item));
        }
        UpdateEmptyState(_txtEventsEmpty, _currentEvents.Count == 0, T("browser.events_none"));
        UpdateTabPager("events", _currentEvents.Count);
    }

    private void RenderStatuses() {
        if (_statusPanel is null) {
            return;
        }

        _statusPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentStatuses, "status")) {
            _statusPanel.Children.Add(BuildStatusCard(item));
        }
        UpdateEmptyState(_txtStatusEmpty, _currentStatuses.Count == 0, T("browser.status_none"));
        UpdateTabPager("status", _currentStatuses.Count);
    }

    private void RenderKinships() {
        if (_kinshipPanel is null) {
            return;
        }

        _kinshipPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentKinships, "kinship")) {
            _kinshipPanel.Children.Add(BuildKinshipCard(item));
        }
        UpdateEmptyState(_txtKinshipEmpty, _currentKinships.Count == 0, T("browser.kinship_none"));
        UpdateTabPager("kinship", _currentKinships.Count);
    }

    private void RenderAssociations() {
        if (_associationsPanel is null) {
            return;
        }

        _associationsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentAssociations, "associations")) {
            _associationsPanel.Children.Add(BuildAssociationCard(item));
        }
        UpdateEmptyState(_txtAssociationsEmpty, _currentAssociations.Count == 0, T("browser.associations_none"));
        UpdateTabPager("associations", _currentAssociations.Count);
    }

    private void RenderPossessions() {
        if (_possessionsPanel is null) {
            return;
        }

        _possessionsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentPossessions, "possessions")) {
            _possessionsPanel.Children.Add(BuildPossessionCard(item));
        }
        UpdateEmptyState(_txtPossessionsEmpty, _currentPossessions.Count == 0, T("browser.possessions_none"));
        UpdateTabPager("possessions", _currentPossessions.Count);
    }

    private void RenderSources() {
        if (_sourcesPanel is null) {
            return;
        }

        _sourcesPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentSources, "sources")) {
            _sourcesPanel.Children.Add(BuildSourceCard(item));
        }
        UpdateEmptyState(_txtSourcesEmpty, _currentSources.Count == 0, T("browser.sources_none"));
        UpdateTabPager("sources", _currentSources.Count);
    }

    private void RenderInstitutions() {
        if (_institutionsPanel is null) {
            return;
        }

        _institutionsPanel.Children.Clear();
        foreach (var item in GetPageItems(_currentInstitutions, "institutions")) {
            _institutionsPanel.Children.Add(BuildInstitutionCard(item));
        }
        UpdateEmptyState(_txtInstitutionsEmpty, _currentInstitutions.Count == 0, T("browser.institutions_none"));
        UpdateTabPager("institutions", _currentInstitutions.Count);
    }

    private static void UpdateEmptyState(TextBlock emptyText, bool isEmpty, string message) {
        emptyText.Text = isEmpty ? message : string.Empty;
        emptyText.IsVisible = isEmpty;
    }

    private IReadOnlyList<T> GetPageItems<T>(IReadOnlyList<T> items, string tabKey) {
        var page = _relatedTabPages.GetValueOrDefault(tabKey, 0);
        return items.Skip(page * RelatedTabPageSize).Take(RelatedTabPageSize).ToArray();
    }

    private void UpdateTabPager(string tabKey, int totalCount) {
        if (!_tabPagerHosts.TryGetValue(tabKey, out var host) ||
            !_tabPrevButtons.TryGetValue(tabKey, out var prevButton) ||
            !_tabNextButtons.TryGetValue(tabKey, out var nextButton) ||
            !_tabPageLabels.TryGetValue(tabKey, out var pageLabel)) {
            return;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)RelatedTabPageSize));
        var currentPage = Math.Min(_relatedTabPages.GetValueOrDefault(tabKey, 0), totalPages - 1);
        _relatedTabPages[tabKey] = currentPage;
        host.IsVisible = totalCount > RelatedTabPageSize;
        prevButton.Content = T("browser.page_prev");
        nextButton.Content = T("browser.page_next");
        prevButton.IsEnabled = currentPage > 0;
        nextButton.IsEnabled = currentPage < totalPages - 1;
        pageLabel.Text = string.Format(T("browser.page_status"), currentPage + 1, totalPages, totalCount);
    }

    private void TabPrevPage_Click(object? sender, RoutedEventArgs e) {
        ChangeRelatedTabPage(sender, -1);
    }

    private void TabNextPage_Click(object? sender, RoutedEventArgs e) {
        ChangeRelatedTabPage(sender, 1);
    }

    private void ChangeRelatedTabPage(object? sender, int delta) {
        if (sender is not Button button || button.Tag is not string tabKey) {
            return;
        }

        var totalCount = GetRelatedTabCount(tabKey);
        if (totalCount <= RelatedTabPageSize) {
            return;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)RelatedTabPageSize));
        var currentPage = _relatedTabPages.GetValueOrDefault(tabKey, 0);
        var nextPage = Math.Clamp(currentPage + delta, 0, totalPages - 1);
        if (nextPage == currentPage) {
            return;
        }

        _relatedTabPages[tabKey] = nextPage;
        RenderRelatedTab(tabKey);
        ReplaceCurrentHistoryState();
    }

    private int GetRelatedTabCount(string tabKey) {
        return tabKey switch {
            "addresses" => _currentAddresses.Count,
            "alt_names" => _currentAltNames.Count,
            "writings" => _currentWritings.Count,
            "postings" => _currentPostings.Count,
            "entry" => _currentEntries.Count,
            "events" => _currentEvents.Count,
            "status" => _currentStatuses.Count,
            "kinship" => _currentKinships.Count,
            "associations" => _currentAssociations.Count,
            "possessions" => _currentPossessions.Count,
            "sources" => _currentSources.Count,
            "institutions" => _currentInstitutions.Count,
            _ => 0
        };
    }

    private void RenderRelatedTab(string tabKey) {
        switch (tabKey) {
            case "addresses":
                RenderAddresses();
                break;
            case "alt_names":
                RenderAltNames();
                break;
            case "writings":
                RenderWritings();
                break;
            case "postings":
                RenderPostings();
                break;
            case "entry":
                RenderEntries();
                break;
            case "events":
                RenderEvents();
                break;
            case "status":
                RenderStatuses();
                break;
            case "kinship":
                RenderKinships();
                break;
            case "associations":
                RenderAssociations();
                break;
            case "possessions":
                RenderPossessions();
                break;
            case "sources":
                RenderSources();
                break;
            case "institutions":
                RenderInstitutions();
                break;
        }
    }

    private async void ChkExpandKinshipNetwork_Changed(object? sender, RoutedEventArgs e) {
        if (_isRestoringHistory) {
            return;
        }

        _expandKinshipNetwork = _chkExpandKinshipNetwork.IsChecked == true;
        _loadedPersonTabs.Remove("kinship");
        _relatedTabPages["kinship"] = 0;

        if (_selectedPersonId.HasValue && ReferenceEquals(_mainTabs.SelectedItem, _tabKinship)) {
            await LoadKinshipsAsync(_selectedPersonId.Value, _expandKinshipNetwork);
        } else {
            RenderKinships();
        }

        ReplaceCurrentHistoryState();
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
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.writing_title_chn"), item.TitleChn);
        AddReadOnlyField(grid, 0, 2, T("browser.writing_title"), item.Title);
        AddReadOnlyField(grid, 1, 0, T("browser.writing_role"), item.Role);
        AddReadOnlyField(grid, 1, 2, "ID", item.TextId.ToString());
        AddReadOnlyField(grid, 2, 0, T("browser.writing_year"), item.Year?.ToString());
        AddReadOnlyField(grid, 2, 2, T("browser.writing_nianhao"), JoinCompactDate(item.Nianhao, item.NianhaoYear, null, null));
        AddReadOnlyField(grid, 3, 0, T("browser.writing_range"), item.Range);
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
        var primaryGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,160,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddReadOnlyField(primaryGrid, 0, 0, T("browser.kinship_relation"), item.Kinship);
        AddReadOnlyField(primaryGrid, 0, 2, T("browser.kinship_data_type"), item.IsDerived ? T("browser.kinship_derived") : T("browser.kinship_direct"));
        AddReadOnlyField(primaryGrid, 0, 4, T("browser.kinship_steps"), FormatKinshipSteps(item));

        var relatedRow = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,90,Auto"),
            RowDefinitions = new RowDefinitions("Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(relatedRow, 0, 0, T("browser.kinship_person"), JoinDisplay(item.KinNameChn, item.KinName));
        AddReadOnlyField(relatedRow, 0, 2, T("browser.person_id"), item.KinPersonId.ToString());
        var jumpButton = new Button {
            Height = 30,
            MinWidth = 76,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = T("browser.jump_to_person"),
            Tag = item.KinPersonId
        };
        jumpButton.Click += PersonJumpButton_Click;
        Grid.SetColumn(jumpButton, 4);
        relatedRow.Children.Add(jumpButton);

        var detailsGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        AddReadOnlyField(detailsGrid, 0, 0, T("browser.address_source"), item.Source, 1, 28, false);
        AddReadOnlyField(detailsGrid, 0, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(detailsGrid, 1, 0, T("browser.address_notes"), item.Notes, 3, 64, true);

        var stack = new StackPanel();
        stack.Children.Add(primaryGrid);
        stack.Children.Add(relatedRow);
        stack.Children.Add(detailsGrid);

        return WrapCard(stack);
    }

    private Control BuildAssociationCard(PersonAssociationItem item) {
        var stack = new StackPanel {
            Spacing = 8
        };

        stack.Children.Add(BuildAssociationSection(T("browser.association_core"), BuildAssociationCoreGrid(item)));
        stack.Children.Add(BuildAssociationSection(T("browser.association_related_people"), BuildAssociationPeopleGrid(item)));
        stack.Children.Add(BuildAssociationSection(T("browser.association_place_date"), BuildAssociationPlaceDateGrid(item)));
        stack.Children.Add(BuildAssociationSection(T("browser.association_context"), BuildAssociationContextGrid(item)));
        stack.Children.Add(BuildAssociationSection(T("browser.association_source_notes"), BuildAssociationSourceGrid(item)));

        return WrapCard(stack);
    }

    private Control BuildAssociationSection(string title, Control content) {
        var stack = new StackPanel {
            Spacing = 6
        };
        stack.Children.Add(new TextBlock {
            Text = title,
            FontWeight = FontWeight.SemiBold
        });
        stack.Children.Add(content);
        return stack;
    }

    private Control BuildAssociationCoreGrid(PersonAssociationItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,90,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 4, T("browser.association_count"), item.Count?.ToString());
        AddReadOnlyPersonField(grid, 1, 0, T("browser.association_associate"), JoinDisplay(item.AssociateNameChn, item.AssociateName), item.AssociatePersonId);
        AddReadOnlyField(grid, 1, 4, T("browser.association_label"), item.Association);
        return grid;
    }

    private Control BuildAssociationPeopleGrid(PersonAssociationItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,90,Auto,220,Auto,90"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.association_kinship"), item.Kinship);
        AddReadOnlyPersonField(grid, 0, 4, T("browser.association_kin_person"), JoinDisplay(item.KinNameChn, item.KinName), item.KinPersonId);
        AddReadOnlyField(grid, 1, 0, T("browser.association_assoc_kinship"), item.AssociateKinship);
        AddReadOnlyPersonField(grid, 1, 4, T("browser.association_assoc_kin_person"), JoinDisplay(item.AssociateKinNameChn, item.AssociateKinName), item.AssociateKinPersonId);
        AddReadOnlyPersonField(grid, 2, 0, T("browser.association_claimer"), JoinDisplay(item.ClaimerNameChn, item.ClaimerName), item.ClaimerPersonId, 5);
        return grid;
    }

    private Control BuildAssociationPlaceDateGrid(PersonAssociationItem item) {
        var stack = new StackPanel {
            Spacing = 8
        };

        var placeGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto")
        };
        AddReadOnlyField(placeGrid, 0, 0, T("browser.association_place"), JoinDisplay(item.AddressNameChn, item.AddressName));
        stack.Children.Add(placeGrid);

        var dateGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,120,Auto,220,Auto,100,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };
        AddReadOnlyField(dateGrid, 0, 0, T("browser.association_year"), item.Year?.ToString());
        AddReadOnlyField(dateGrid, 0, 2, T("browser.association_nianhao"), item.Nianhao);
        AddReadOnlyField(dateGrid, 0, 4, T("browser.association_nianhao_year"), item.NianhaoYear?.ToString());
        AddReadOnlyField(dateGrid, 1, 0, T("browser.association_month"), item.Month?.ToString());
        AddReadOnlyField(dateGrid, 1, 2, T("browser.association_range"), item.Range);
        AddReadOnlyField(dateGrid, 1, 4, T("browser.association_day"), item.Day?.ToString());
        AddReadOnlyField(dateGrid, 1, 6, T("browser.association_ganzhi"), item.Ganzhi);
        stack.Children.Add(dateGrid);

        var intercalaryGrid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto")
        };
        AddReadOnlyCheck(intercalaryGrid, 0, 0, T("browser.association_intercalary"), item.Intercalary);
        stack.Children.Add(intercalaryGrid);

        return stack;
    }

    private Control BuildAssociationContextGrid(PersonAssociationItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.association_topic"), JoinDisplay(item.TopicChn, item.Topic));
        AddReadOnlyField(grid, 0, 2, T("browser.association_institution"), JoinDisplay(item.InstitutionNameChn, item.InstitutionName));
        AddReadOnlyField(grid, 1, 0, T("browser.association_occasion"), JoinDisplay(item.OccasionChn, item.Occasion));
        AddReadOnlyField(grid, 1, 2, T("browser.association_genre"), JoinDisplay(item.LiteraryGenreChn, item.LiteraryGenre));
        AddReadOnlyField(grid, 2, 0, T("browser.association_text_title"), item.TextTitle, 3, 28, false);
        return grid;
    }

    private Control BuildAssociationSourceGrid(PersonAssociationItem item) {
        var grid = new Grid {
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_source"), JoinDisplay(item.SourceTitleChn, item.SourceTitle));
        AddReadOnlyField(grid, 0, 2, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 1, 0, T("browser.address_notes"), item.Notes, 3, 64, true);
        return grid;
    }

    private async void PersonJumpButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is not Button { Tag: int personId } || personId < 0) {
            return;
        }

        await NavigateToPersonAsync(personId, false);
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
            ColumnDefinitions = new ColumnDefinitions("Auto,220,Auto,220,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.source_title_chn"), item.TitleChn);
        AddReadOnlyField(grid, 0, 2, T("browser.source_title"), item.Title);
        AddReadOnlyField(grid, 1, 0, T("browser.address_pages"), item.Pages);
        AddReadOnlyField(grid, 1, 2, T("browser.source_hyperlink"), item.Hyperlink);
        var openButton = BuildExternalLinkButton(item.Hyperlink);
        Grid.SetRow(openButton, 1);
        Grid.SetColumn(openButton, 4);
        grid.Children.Add(openButton);
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
            ColumnDefinitions = new ColumnDefinitions("Auto,90,Auto,220,Auto,*,Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto")
        };

        AddReadOnlyField(grid, 0, 0, T("browser.address_sequence"), item.Sequence.ToString());
        AddReadOnlyField(grid, 0, 2, T("browser.address_type"), item.AddressType);
        AddReadOnlyField(grid, 0, 4, T("browser.address_name"), JoinDisplay(item.AddressNameChn, item.AddressName));

        var label = new TextBlock {
            Text = T("browser.address_maternal"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 2, 8, 0)
        };
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, 6);
        grid.Children.Add(label);

        var check = new CheckBox {
            IsEnabled = false,
            IsChecked = item.Natal ?? false,
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetRow(check, 0);
        Grid.SetColumn(check, 7);
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
        var effectiveMinHeight = multiline
            ? Math.Max(minHeight, 64)
            : Math.Max(minHeight, 32);

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
            MinHeight = effectiveMinHeight,
            Padding = new Thickness(6, 4),
            VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 6)
        };
        Grid.SetRow(valueBox, row);
        Grid.SetColumn(valueBox, column + 1);
        Grid.SetColumnSpan(valueBox, valueColumnSpan);
        grid.Children.Add(valueBox);
    }

    private void AddReadOnlyPersonField(
        Grid grid,
        int row,
        int column,
        string label,
        string? value,
        int? personId,
        int valueColumnSpan = 1
    ) {
        AddReadOnlyField(grid, row, column, label, value, valueColumnSpan, 28, false);

        var jumpButton = BuildPersonJumpButton(personId);
        Grid.SetRow(jumpButton, row);
        Grid.SetColumn(jumpButton, column + 2);
        grid.Children.Add(jumpButton);
    }

    private Button BuildPersonJumpButton(int? personId) {
        var button = new Button {
            Height = 30,
            MinWidth = 76,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ConfigurePersonJumpButton(button, personId);
        button.Click += PersonJumpButton_Click;
        return button;
    }

    private void ConfigurePersonJumpButton(Button button, int? personId) {
        button.Content = T("browser.jump_to_person");
        button.Tag = personId ?? -1;
        button.IsEnabled = personId.HasValue && personId.Value >= 0;
    }

    private static int? TryParseLeadingPersonId(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var match = Regex.Match(value, @"^\s*(\d+)\s*/");
        return match.Success && int.TryParse(match.Groups[1].Value, out var personId)
            ? personId
            : null;
    }

    private Button BuildExternalLinkButton(string? hyperlink) {
        var button = new Button {
            Height = 30,
            MinWidth = 76,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = T("browser.open_link"),
            Tag = hyperlink ?? string.Empty,
            IsEnabled = !string.IsNullOrWhiteSpace(hyperlink)
        };
        button.Click += ExternalLinkButton_Click;
        return button;
    }

    private void ExternalLinkButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is not Button { Tag: string hyperlink } || string.IsNullOrWhiteSpace(hyperlink)) {
            return;
        }

        try {
            OpenExternalTarget(hyperlink);
        } catch (Exception ex) {
            _txtRecord.Text = ex.Message;
        }
    }

    private static void OpenExternalTarget(string target) {
        Process.Start(new ProcessStartInfo {
            FileName = target,
            UseShellExecute = true
        });
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
        ConfigurePersonJumpButton(_btnIndexYearSourceGoTo, null);
        _valIndexAddress.Text = string.Empty;
        _valIndexAddressType.Text = string.Empty;

        ClearBasicInfo();
        ResetLazyTabs();
        _txtNoSelection.Text = T("browser.no_selection");
        _txtNoSelection.IsVisible = true;
        UpdateTabHeaders(null);
        UpdateRecordText();
    }

    private void UpdateRecordText() {
        var key = _hasMore ? "browser.search_result_count_continued" : "browser.search_result_count";
        _txtRecord.Text = string.Format(T(key), _people.Count);
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
        _btnHistoryBack = this.FindControl<Button>("BtnHistoryBack") ?? throw new InvalidOperationException("BtnHistoryBack not found.");
        _btnHistoryForward = this.FindControl<Button>("BtnHistoryForward") ?? throw new InvalidOperationException("BtnHistoryForward not found.");
        _btnLoadFromFile = this.FindControl<Button>("BtnLoadFromFile") ?? throw new InvalidOperationException("BtnLoadFromFile not found.");
        _btnSearch = this.FindControl<Button>("BtnSearch") ?? throw new InvalidOperationException("BtnSearch not found.");
        _btnClear = this.FindControl<Button>("BtnClear") ?? throw new InvalidOperationException("BtnClear not found.");
        _btnSaveToFile = this.FindControl<Button>("BtnSaveToFile") ?? throw new InvalidOperationException("BtnSaveToFile not found.");
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
        _btnIndexYearSourceGoTo = this.FindControl<Button>("BtnIndexYearSourceGoTo") ?? throw new InvalidOperationException("BtnIndexYearSourceGoTo not found.");
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
        _addressesLoadingHost = this.FindControl<Border>("AddressesLoadingHost") ?? throw new InvalidOperationException("AddressesLoadingHost not found.");
        _txtAddressesLoading = this.FindControl<TextBlock>("TxtAddressesLoading") ?? throw new InvalidOperationException("TxtAddressesLoading not found.");
        _txtAddressesEmpty = this.FindControl<TextBlock>("TxtAddressesEmpty") ?? throw new InvalidOperationException("TxtAddressesEmpty not found.");
        _addressesPanel = this.FindControl<StackPanel>("AddressesPanel") ?? throw new InvalidOperationException("AddressesPanel not found.");
        _altNamesLoadingHost = this.FindControl<Border>("AltNamesLoadingHost") ?? throw new InvalidOperationException("AltNamesLoadingHost not found.");
        _txtAltNamesLoading = this.FindControl<TextBlock>("TxtAltNamesLoading") ?? throw new InvalidOperationException("TxtAltNamesLoading not found.");
        _txtAltNamesEmpty = this.FindControl<TextBlock>("TxtAltNamesEmpty") ?? throw new InvalidOperationException("TxtAltNamesEmpty not found.");
        _altNamesPanel = this.FindControl<StackPanel>("AltNamesPanel") ?? throw new InvalidOperationException("AltNamesPanel not found.");
        _writingsLoadingHost = this.FindControl<Border>("WritingsLoadingHost") ?? throw new InvalidOperationException("WritingsLoadingHost not found.");
        _txtWritingsLoading = this.FindControl<TextBlock>("TxtWritingsLoading") ?? throw new InvalidOperationException("TxtWritingsLoading not found.");
        _txtWritingsEmpty = this.FindControl<TextBlock>("TxtWritingsEmpty") ?? throw new InvalidOperationException("TxtWritingsEmpty not found.");
        _writingsPanel = this.FindControl<StackPanel>("WritingsPanel") ?? throw new InvalidOperationException("WritingsPanel not found.");
        _postingsLoadingHost = this.FindControl<Border>("PostingsLoadingHost") ?? throw new InvalidOperationException("PostingsLoadingHost not found.");
        _txtPostingsLoading = this.FindControl<TextBlock>("TxtPostingsLoading") ?? throw new InvalidOperationException("TxtPostingsLoading not found.");
        _txtPostingsEmpty = this.FindControl<TextBlock>("TxtPostingsEmpty") ?? throw new InvalidOperationException("TxtPostingsEmpty not found.");
        _postingsPanel = this.FindControl<StackPanel>("PostingsPanel") ?? throw new InvalidOperationException("PostingsPanel not found.");
        _entryLoadingHost = this.FindControl<Border>("EntryLoadingHost") ?? throw new InvalidOperationException("EntryLoadingHost not found.");
        _txtEntryLoading = this.FindControl<TextBlock>("TxtEntryLoading") ?? throw new InvalidOperationException("TxtEntryLoading not found.");
        _txtEntryEmpty = this.FindControl<TextBlock>("TxtEntryEmpty") ?? throw new InvalidOperationException("TxtEntryEmpty not found.");
        _entryPanel = this.FindControl<StackPanel>("EntryPanel") ?? throw new InvalidOperationException("EntryPanel not found.");
        _eventsLoadingHost = this.FindControl<Border>("EventsLoadingHost") ?? throw new InvalidOperationException("EventsLoadingHost not found.");
        _txtEventsLoading = this.FindControl<TextBlock>("TxtEventsLoading") ?? throw new InvalidOperationException("TxtEventsLoading not found.");
        _txtEventsEmpty = this.FindControl<TextBlock>("TxtEventsEmpty") ?? throw new InvalidOperationException("TxtEventsEmpty not found.");
        _eventsPanel = this.FindControl<StackPanel>("EventsPanel") ?? throw new InvalidOperationException("EventsPanel not found.");
        _statusLoadingHost = this.FindControl<Border>("StatusLoadingHost") ?? throw new InvalidOperationException("StatusLoadingHost not found.");
        _txtStatusLoading = this.FindControl<TextBlock>("TxtStatusLoading") ?? throw new InvalidOperationException("TxtStatusLoading not found.");
        _txtStatusEmpty = this.FindControl<TextBlock>("TxtStatusEmpty") ?? throw new InvalidOperationException("TxtStatusEmpty not found.");
        _statusPanel = this.FindControl<StackPanel>("StatusPanel") ?? throw new InvalidOperationException("StatusPanel not found.");
        _kinshipLoadingHost = this.FindControl<Border>("KinshipLoadingHost") ?? throw new InvalidOperationException("KinshipLoadingHost not found.");
        _txtKinshipLoading = this.FindControl<TextBlock>("TxtKinshipLoading") ?? throw new InvalidOperationException("TxtKinshipLoading not found.");
        _txtKinshipEmpty = this.FindControl<TextBlock>("TxtKinshipEmpty") ?? throw new InvalidOperationException("TxtKinshipEmpty not found.");
        _chkExpandKinshipNetwork = this.FindControl<CheckBox>("ChkExpandKinshipNetwork") ?? throw new InvalidOperationException("ChkExpandKinshipNetwork not found.");
        _kinshipPanel = this.FindControl<StackPanel>("KinshipPanel") ?? throw new InvalidOperationException("KinshipPanel not found.");
        _associationsLoadingHost = this.FindControl<Border>("AssociationsLoadingHost") ?? throw new InvalidOperationException("AssociationsLoadingHost not found.");
        _txtAssociationsLoading = this.FindControl<TextBlock>("TxtAssociationsLoading") ?? throw new InvalidOperationException("TxtAssociationsLoading not found.");
        _txtAssociationsEmpty = this.FindControl<TextBlock>("TxtAssociationsEmpty") ?? throw new InvalidOperationException("TxtAssociationsEmpty not found.");
        _associationsPanel = this.FindControl<StackPanel>("AssociationsPanel") ?? throw new InvalidOperationException("AssociationsPanel not found.");
        _possessionsLoadingHost = this.FindControl<Border>("PossessionsLoadingHost") ?? throw new InvalidOperationException("PossessionsLoadingHost not found.");
        _txtPossessionsLoading = this.FindControl<TextBlock>("TxtPossessionsLoading") ?? throw new InvalidOperationException("TxtPossessionsLoading not found.");
        _txtPossessionsEmpty = this.FindControl<TextBlock>("TxtPossessionsEmpty") ?? throw new InvalidOperationException("TxtPossessionsEmpty not found.");
        _possessionsPanel = this.FindControl<StackPanel>("PossessionsPanel") ?? throw new InvalidOperationException("PossessionsPanel not found.");
        _sourcesLoadingHost = this.FindControl<Border>("SourcesLoadingHost") ?? throw new InvalidOperationException("SourcesLoadingHost not found.");
        _txtSourcesLoading = this.FindControl<TextBlock>("TxtSourcesLoading") ?? throw new InvalidOperationException("TxtSourcesLoading not found.");
        _txtSourcesEmpty = this.FindControl<TextBlock>("TxtSourcesEmpty") ?? throw new InvalidOperationException("TxtSourcesEmpty not found.");
        _sourcesPanel = this.FindControl<StackPanel>("SourcesPanel") ?? throw new InvalidOperationException("SourcesPanel not found.");
        _institutionsLoadingHost = this.FindControl<Border>("InstitutionsLoadingHost") ?? throw new InvalidOperationException("InstitutionsLoadingHost not found.");
        _txtInstitutionsLoading = this.FindControl<TextBlock>("TxtInstitutionsLoading") ?? throw new InvalidOperationException("TxtInstitutionsLoading not found.");
        _txtInstitutionsEmpty = this.FindControl<TextBlock>("TxtInstitutionsEmpty") ?? throw new InvalidOperationException("TxtInstitutionsEmpty not found.");
        _institutionsPanel = this.FindControl<StackPanel>("InstitutionsPanel") ?? throw new InvalidOperationException("InstitutionsPanel not found.");
        RegisterTabPager("addresses", "AddressesPager", "BtnAddressesPrev", "BtnAddressesNext", "TxtAddressesPage");
        RegisterTabPager("alt_names", "AltNamesPager", "BtnAltNamesPrev", "BtnAltNamesNext", "TxtAltNamesPage");
        RegisterTabPager("writings", "WritingsPager", "BtnWritingsPrev", "BtnWritingsNext", "TxtWritingsPage");
        RegisterTabPager("postings", "PostingsPager", "BtnPostingsPrev", "BtnPostingsNext", "TxtPostingsPage");
        RegisterTabPager("entry", "EntryPager", "BtnEntryPrev", "BtnEntryNext", "TxtEntryPage");
        RegisterTabPager("events", "EventsPager", "BtnEventsPrev", "BtnEventsNext", "TxtEventsPage");
        RegisterTabPager("status", "StatusPager", "BtnStatusPrev", "BtnStatusNext", "TxtStatusPage");
        RegisterTabPager("kinship", "KinshipPager", "BtnKinshipPrev", "BtnKinshipNext", "TxtKinshipPage");
        RegisterTabPager("associations", "AssociationsPager", "BtnAssociationsPrev", "BtnAssociationsNext", "TxtAssociationsPage");
        RegisterTabPager("possessions", "PossessionsPager", "BtnPossessionsPrev", "BtnPossessionsNext", "TxtPossessionsPage");
        RegisterTabPager("sources", "SourcesPager", "BtnSourcesPrev", "BtnSourcesNext", "TxtSourcesPage");
        RegisterTabPager("institutions", "InstitutionsPager", "BtnInstitutionsPrev", "BtnInstitutionsNext", "TxtInstitutionsPage");

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

    private void RegisterTabPager(string tabKey, string hostName, string prevButtonName, string nextButtonName, string labelName) {
        _tabPagerHosts[tabKey] = this.FindControl<StackPanel>(hostName) ?? throw new InvalidOperationException($"{hostName} not found.");
        _tabPrevButtons[tabKey] = this.FindControl<Button>(prevButtonName) ?? throw new InvalidOperationException($"{prevButtonName} not found.");
        _tabNextButtons[tabKey] = this.FindControl<Button>(nextButtonName) ?? throw new InvalidOperationException($"{nextButtonName} not found.");
        _tabPageLabels[tabKey] = this.FindControl<TextBlock>(labelName) ?? throw new InvalidOperationException($"{labelName} not found.");
        _tabPagerHosts[tabKey].IsVisible = false;
    }

    private void RegisterBasicValue(string key, string controlName) {
        _basicValues[key] = this.FindControl<TextBox>(controlName) ?? throw new InvalidOperationException($"{controlName} not found.");
    }

    private sealed record PersonBrowserHistoryState(
        string? Keyword,
        IReadOnlyList<PersonListItem> SearchResults,
        int NextOffset,
        bool HasMore,
        int SelectedResultIndex,
        int? SelectedPersonId,
        string? SelectedTabKey,
        Dictionary<string, int> RelatedTabPages,
        HashSet<string> LoadedTabs,
        PersonDetail? Detail,
        IReadOnlyList<PersonAddressItem> Addresses,
        IReadOnlyList<PersonAltNameItem> AltNames,
        IReadOnlyList<PersonWritingItem> Writings,
        IReadOnlyList<PersonPostingItem> Postings,
        IReadOnlyList<PersonEntryItem> Entries,
        IReadOnlyList<PersonEventItem> Events,
        IReadOnlyList<PersonStatusItem> Statuses,
        IReadOnlyList<PersonKinshipItem> Kinships,
        bool ExpandKinshipNetwork,
        IReadOnlyList<PersonAssociationItem> Associations,
        IReadOnlyList<PersonPossessionItem> Possessions,
        IReadOnlyList<PersonSourceItem> Sources,
        IReadOnlyList<PersonInstitutionItem> Institutions
    );

    private void RegisterBasicCheck(string key, string controlName) {
        _basicChecks[key] = this.FindControl<CheckBox>(controlName) ?? throw new InvalidOperationException($"{controlName} not found.");
    }
}
