using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using System.Reflection;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class OfficeQueryWindowTests {
    [AvaloniaFact]
    public async Task OfficeQueryWindow_LoadsFiltersAndRunsQuery_WithInjectedServices() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "office-query-window-fixture.sqlite3");
        File.WriteAllText(sqlitePath, "fixture");

        var officeService = new FakeOfficeQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new OfficeQueryWindow(sqlitePath, localization, officeService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 office codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Office query window did not finish loading filters."
            );

            var txtSelectedPersonPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPersonPlaces");
            var txtSelectedOfficePlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedOfficePlaces");
            Assert.Equal("All people places", txtSelectedPersonPlaces.Text);
            Assert.Equal("All office places", txtSelectedOfficePlaces.Text);

            var btnRunQuery = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnRunQuery");
            btnRunQuery.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var gridRecords = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridRecords");
            var gridPeople = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridPeople");
            var txtStatusBar = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar");

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => ((IEnumerable<object>?)gridRecords.ItemsSource)?.Count() == 1
                      && ((IEnumerable<object>?)gridPeople.ItemsSource)?.Count() == 1
                      && txtStatusBar.Text?.Contains("Loaded 1 office records for 1 people.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Office query window did not populate query results."
            );

            Assert.Equal(1, officeService.QueryCallCount);
            Assert.Equal(sqlitePath, officeService.LastSqlitePath);
        } finally {
            window.Close();
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    [AvaloniaFact]
    public async Task OfficeQueryWindow_BuildRequest_KeepsPersonAndOfficePlaceSemanticsSeparate() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "office-query-window-build-request.sqlite3");
        File.WriteAllText(sqlitePath, "fixture");

        var officeService = new FakeOfficeQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new OfficeQueryWindow(sqlitePath, localization, officeService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 office codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Office query window did not finish loading filters."
            );

            SetPrivateField(window, "_selectedPersonPlaceIds", new List<int> { 10 });
            SetPrivateField(window, "_selectedOfficePlaceIds", new List<int> { 20 });

            var txtSelectedPersonPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPersonPlaces");
            var txtSelectedOfficePlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedOfficePlaces");
            txtSelectedPersonPlaces.Text = "福州 / Fu Zhou (10)";
            txtSelectedOfficePlaces.Text = "臨安 / Lin An (20)";

            var chkIncludePersonSubUnits = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkIncludePersonSubUnits");
            var chkIncludeOfficeSubUnits = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkIncludeOfficeSubUnits");
            chkIncludePersonSubUnits.IsChecked = true;
            chkIncludeOfficeSubUnits.IsChecked = true;

            var chkUseIndexYear = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkUseIndexYear");
            var txtIndexYearFrom = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtIndexYearFrom");
            var txtIndexYearTo = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtIndexYearTo");
            chkUseIndexYear.IsChecked = true;
            txtIndexYearFrom.Text = "1000";
            txtIndexYearTo.Text = "1100";

            var chkUseOfficeYear = AvaloniaUiTestHelper.FindRequiredControl<CheckBox>(window, "ChkUseOfficeYear");
            var txtOfficeYearFrom = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtOfficeYearFrom");
            var txtOfficeYearTo = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtOfficeYearTo");
            chkUseOfficeYear.IsChecked = true;
            txtOfficeYearFrom.Text = "1070";
            txtOfficeYearTo.Text = "1080";

            var buildRequestMethod = typeof(OfficeQueryWindow).GetMethod("BuildRequest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(buildRequestMethod);

            var request = Assert.IsType<OfficeQueryRequest>(buildRequestMethod!.Invoke(window, null));
            Assert.Equal(new[] { 10 }, request.PersonPlaceIds);
            Assert.Equal(new[] { 20 }, request.OfficePlaceIds);
            Assert.True(request.IncludeSubordinatePersonUnits);
            Assert.True(request.IncludeSubordinateOfficeUnits);
            Assert.True(request.UseIndexYearRange);
            Assert.Equal(1000, request.IndexYearFrom);
            Assert.Equal(1100, request.IndexYearTo);
            Assert.True(request.UseOfficeYearRange);
            Assert.Equal(1070, request.OfficeYearFrom);
            Assert.Equal(1080, request.OfficeYearTo);
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

    private sealed class FakeOfficeQueryService : IOfficeQueryService {
        public int QueryCallCount { get; private set; }
        public string? LastSqlitePath { get; private set; }

        public Task<OfficePickerData> GetOfficePickerDataAsync(string sqlitePath, CancellationToken cancellationToken = default) {
            var root = new OfficeTypeNode(
                OfficePickerData.RootCode,
                null,
                null,
                new[] {
                    new OfficeTypeNode("01", "Civil Office", "文職", Array.Empty<OfficeTypeNode>(), new[] { "100" }),
                    new OfficeTypeNode("02", "Military Office", "武職", Array.Empty<OfficeTypeNode>(), new[] { "200" })
                },
                new[] { "100", "200" }
            );

            var result = new OfficePickerData(
                root,
                new[] {
                    new OfficeCodeOption("100", "Prefect", "知州", null, null, "Song", "宋", 2),
                    new OfficeCodeOption("200", "Commander", "都統", null, null, "Song", "宋", 1)
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    ["100"] = "01",
                    ["200"] = "02"
                }
            );

            return Task.FromResult(result);
        }

        public Task<OfficeQueryResult> QueryAsync(string sqlitePath, OfficeQueryRequest request, CancellationToken cancellationToken = default) {
            QueryCallCount++;
            LastSqlitePath = sqlitePath;

            var records = new[] {
                new OfficeQueryRecord(
                    PersonId: 1,
                    NameChn: "蘇軾",
                    Name: "Su Shi",
                    IndexYear: 1080,
                    IndexYearType: "指數年",
                    Sex: "M",
                    Dynasty: "宋",
                    PostingDynasty: "宋",
                    IndexAddressId: 10,
                    IndexAddress: "福州",
                    IndexAddressType: "籍貫",
                    PostingId: 1000,
                    Sequence: 1,
                    OfficeCode: "100",
                    Office: "知州",
                    AppointmentCode: 1,
                    AppointmentType: "任命",
                    AssumeOfficeCode: 1,
                    AssumeOffice: "到任",
                    OfficeCategoryId: 1,
                    Category: "文職",
                    FirstYear: 1071,
                    FirstNianhaoCode: 1,
                    FirstNianhao: "元祐",
                    FirstNianhaoPinyin: "yuanyou",
                    FirstNianhaoYear: 3,
                    FirstRangeCode: 1,
                    FirstRangeDesc: "Range",
                    FirstRange: "時限",
                    FirstMonth: 4,
                    FirstIntercalary: false,
                    FirstDay: 15,
                    FirstGanzhiCode: 1,
                    FirstGanzhi: "甲子",
                    FirstGanzhiPinyin: "jiazi",
                    LastYear: 1074,
                    LastNianhaoCode: 2,
                    LastNianhao: "紹聖",
                    LastNianhaoPinyin: "shaosheng",
                    LastNianhaoYear: 1,
                    LastRangeCode: 2,
                    LastRangeDesc: "Range B",
                    LastRange: "時限乙",
                    LastMonth: 8,
                    LastIntercalary: false,
                    LastDay: 22,
                    LastGanzhiCode: 2,
                    LastGanzhi: "乙丑",
                    LastGanzhiPinyin: "yichou",
                    InstitutionCode: 0,
                    InstitutionNameCode: 1,
                    Institution: "中書省",
                    OfficeAddressId: 20,
                    OfficeAddress: "臨安",
                    OfficeXCoord: 120.2,
                    OfficeYCoord: 30.3,
                    OfficeXyCount: 1,
                    SourceId: 1,
                    Source: "來源",
                    Pages: "12-13",
                    Notes: "note"
                )
            };

            var people = new[] {
                new OfficeQueryPerson(
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
                    OfficeAddressId: 20,
                    OfficeAddress: "臨安",
                    OfficeXCoord: 120.2,
                    OfficeYCoord: 30.3,
                    XyCount: 1,
                    PostingCount: 1
                )
            };

            return Task.FromResult(new OfficeQueryResult(records, people));
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
}
