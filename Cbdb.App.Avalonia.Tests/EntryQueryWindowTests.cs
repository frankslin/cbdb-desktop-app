using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using System.Reflection;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class EntryQueryWindowTests {
    [AvaloniaFact]
    public async Task EntryQueryWindow_LoadsFiltersAndRunsQuery_WithInjectedServices() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "entry-query-window-fixture.sqlite3");
        CreateDynastyFixture(sqlitePath);

        var entryService = new FakeEntryQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new EntryQueryWindow(sqlitePath, localization, entryService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 entry codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Entry query window did not finish loading filters."
            );

            var txtSelectedEntries = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedEntries");
            var txtSelectedPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPlaces");
            Assert.Equal("All entry methods", txtSelectedEntries.Text);
            Assert.Equal("All places", txtSelectedPlaces.Text);

            var btnRunQuery = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnRunQuery");
            btnRunQuery.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var gridRecords = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridRecords");
            var gridPeople = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridPeople");
            var txtStatusBar = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar");

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => ((IEnumerable<object>?)gridRecords.ItemsSource)?.Count() == 1
                      && ((IEnumerable<object>?)gridPeople.ItemsSource)?.Count() == 1
                      && txtStatusBar.Text?.Contains("Loaded 1 entry records for 1 people.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Entry query window did not populate query results."
            );

            Assert.Equal(1, entryService.QueryCallCount);
            Assert.Equal(sqlitePath, entryService.LastSqlitePath);
        } finally {
            window.Close();
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    [AvaloniaFact]
    public async Task EntryQueryWindow_BuildRequest_IncludesSelectedDynasties() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "entry-query-window-build-request.sqlite3");
        CreateDynastyFixture(sqlitePath);

        var entryService = new FakeEntryQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new EntryQueryWindow(sqlitePath, localization, entryService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 entry codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Entry query window did not finish loading filters."
            );

            SetPrivateField(window, "_selectedPlaceIds", new List<int> { 10 });
            var txtSelectedPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPlaces");
            txtSelectedPlaces.Text = "福州 / Fu Zhou (10)";

            var chkIncludeSubUnits = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkIncludeSubUnits");
            chkIncludeSubUnits.IsChecked = true;

            var chkUseIndexYear = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkUseIndexYear");
            var txtIndexYearFrom = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtIndexYearFrom");
            var txtIndexYearTo = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtIndexYearTo");
            chkUseIndexYear.IsChecked = true;
            txtIndexYearFrom.Text = "1000";
            txtIndexYearTo.Text = "1100";

            var chkUseEntryYear = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkUseEntryYear");
            var txtEntryYearFrom = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtEntryYearFrom");
            var txtEntryYearTo = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtEntryYearTo");
            chkUseEntryYear.IsChecked = true;
            txtEntryYearFrom.Text = "1070";
            txtEntryYearTo.Text = "1080";

            var dynastyPicker = AvaloniaUiTestHelper.FindRequiredControl<Control>(window, "DynastyPicker");
            SetPrivateField(dynastyPicker, "_selectedDynastyIds", new HashSet<int> { 1, 2 });

            var buildRequestMethod = typeof(EntryQueryWindow).GetMethod("BuildRequest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(buildRequestMethod);

            var request = Assert.IsType<EntryQueryRequest>(buildRequestMethod!.Invoke(window, null));
            Assert.Equal(new[] { 10 }, request.PlaceIds);
            Assert.True(request.IncludeSubordinateUnits);
            Assert.True(request.UseIndexYearRange);
            Assert.Equal(1000, request.IndexYearFrom);
            Assert.Equal(1100, request.IndexYearTo);
            Assert.True(request.UseEntryYearRange);
            Assert.Equal(1070, request.EntryYearFrom);
            Assert.Equal(1080, request.EntryYearTo);
            Assert.Equal(new[] { 1, 2 }, request.DynastyIds);
        } finally {
            window.Close();
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    [AvaloniaFact]
    public async Task EntryQueryWindow_RunQuery_PassesPlaceYearAndDynastyFiltersToService() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "entry-query-window-run-filters.sqlite3");
        CreateDynastyFixture(sqlitePath);

        var entryService = new FakeEntryQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new EntryQueryWindow(sqlitePath, localization, entryService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 entry codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Entry query window did not finish loading filters."
            );

            SetPrivateField(window, "_selectedPlaceIds", new List<int> { 20 });
            var txtSelectedPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPlaces");
            txtSelectedPlaces.Text = "臨安 / Lin An (20)";

            var chkIncludeSubUnits = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkIncludeSubUnits");
            chkIncludeSubUnits.IsChecked = false;

            var chkUseEntryYear = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkUseEntryYear");
            var txtEntryYearFrom = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtEntryYearFrom");
            var txtEntryYearTo = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtEntryYearTo");
            chkUseEntryYear.IsChecked = true;
            txtEntryYearFrom.Text = "1075";
            txtEntryYearTo.Text = "1085";

            var dynastyPicker = AvaloniaUiTestHelper.FindRequiredControl<Control>(window, "DynastyPicker");
            SetPrivateField(dynastyPicker, "_selectedDynastyIds", new HashSet<int> { 2 });

            var btnRunQuery = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnRunQuery");
            btnRunQuery.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => entryService.QueryCallCount == 1,
                TimeSpan.FromSeconds(5),
                "Entry query was not invoked."
            );

            var request = Assert.IsType<EntryQueryRequest>(entryService.LastRequest);
            Assert.Equal(new[] { 20 }, request.PlaceIds);
            Assert.False(request.IncludeSubordinateUnits);
            Assert.True(request.UseEntryYearRange);
            Assert.Equal(1075, request.EntryYearFrom);
            Assert.Equal(1085, request.EntryYearTo);
            Assert.Equal(new[] { 2 }, request.DynastyIds);
        } finally {
            window.Close();
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    private static void SetPrivateField(object instance, string fieldName, object value) {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private sealed class FakeEntryQueryService : IEntryQueryService {
        public int QueryCallCount { get; private set; }
        public string? LastSqlitePath { get; private set; }
        public EntryQueryRequest? LastRequest { get; private set; }

        public Task<EntryPickerData> GetEntryPickerDataAsync(string sqlitePath, CancellationToken cancellationToken = default) {
            var root = new EntryTypeNode(
                EntryPickerData.RootCode,
                null,
                null,
                new[] {
                    new EntryTypeNode("21", "Examinations", "科舉", Array.Empty<EntryTypeNode>(), new[] { "E1" }),
                    new EntryTypeNode("22", "Recommendation", "薦舉", Array.Empty<EntryTypeNode>(), new[] { "E2" })
                },
                new[] { "E1", "E2" }
            );

            var result = new EntryPickerData(
                root,
                new[] {
                    new EntryCodeOption("E1", "Metropolitan Exam", "省試", 2),
                    new EntryCodeOption("E2", "Recommendation", "薦舉", 1)
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    ["E1"] = "21",
                    ["E2"] = "22"
                }
            );

            return Task.FromResult(result);
        }

        public Task<EntryQueryResult> QueryAsync(string sqlitePath, EntryQueryRequest request, CancellationToken cancellationToken = default) {
            QueryCallCount++;
            LastSqlitePath = sqlitePath;
            LastRequest = request;

            var records = new[] {
                new EntryQueryRecord(
                    PersonId: 1,
                    NameChn: "蘇軾",
                    Name: "Su Shi",
                    IndexYear: 1080,
                    IndexYearType: "指數年",
                    Sex: "M",
                    Dynasty: "宋",
                    IndexAddressId: 10,
                    IndexAddress: "福州",
                    IndexAddressType: "籍貫",
                    Sequence: 1,
                    EntryCode: "E1",
                    EntryMethod: "省試",
                    EntryYear: 1071,
                    Nianhao: "元祐",
                    NianhaoYear: 3,
                    Range: "時限",
                    ExamRank: "甲等",
                    Age: 35,
                    Kinship: "父",
                    KinPerson: "蘇洵",
                    Association: "師",
                    AssociatePerson: "歐陽脩",
                    Institution: "國子監",
                    ExamField: "進士",
                    EntryAddressId: 20,
                    EntryAddress: "臨安",
                    EntryXCoord: 120.2,
                    EntryYCoord: 30.3,
                    EntryXyCount: 1,
                    ParentalStatus: "官",
                    AttemptCount: 2,
                    Source: "來源",
                    Pages: "12-13",
                    Notes: "note",
                    PostingNotes: "posting-note"
                )
            };

            var people = new[] {
                new EntryQueryPerson(
                    PersonId: 1,
                    NameChn: "蘇軾",
                    Name: "Su Shi",
                    IndexYear: 1080,
                    IndexYearType: "指數年",
                    Sex: "M",
                    Dynasty: "宋",
                    IndexAddressId: 10,
                    IndexAddress: "福州",
                    IndexAddressType: "籍貫",
                    EntryAddressId: 20,
                    EntryAddress: "臨安",
                    EntryXCoord: 120.2,
                    EntryYCoord: 30.3,
                    XyCount: 1,
                    EntryCount: 1
                )
            };

            return Task.FromResult(new EntryQueryResult(records, people));
        }
    }

    private sealed class FakePlaceLookupService : IPlaceLookupService {
        public Task<IReadOnlyList<PlaceOption>> GetPlacesAsync(string sqlitePath, CancellationToken cancellationToken = default) {
            IReadOnlyList<PlaceOption> places = new[] {
                new PlaceOption(10, "Fu Zhou", "福州", null, 960, 1279, null, null, null, null, null, null),
                new PlaceOption(20, "Lin An", "臨安", null, 960, 1279, null, null, null, null, null, null)
            };
            return Task.FromResult(places);
        }
    }

    private static void CreateDynastyFixture(string sqlitePath) {
        TestSqliteFileHelper.Delete(sqlitePath);
        using var connection = new SqliteConnection($"Data Source={sqlitePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE DYNASTIES (
    c_dy INTEGER PRIMARY KEY,
    c_dynasty TEXT,
    c_dynasty_chn TEXT,
    c_start INTEGER,
    c_end INTEGER
);
INSERT INTO DYNASTIES VALUES (1, 'Song', '宋', 960, 1279);
INSERT INTO DYNASTIES VALUES (2, 'Yuan', '元', 1271, 1368);
""";
        command.ExecuteNonQuery();
    }
}
