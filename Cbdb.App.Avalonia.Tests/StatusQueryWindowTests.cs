using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class StatusQueryWindowTests {
    [AvaloniaFact]
    public async Task StatusQueryWindow_LoadsFiltersAndRunsQuery_WithInjectedServices() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "status-query-window-fixture.sqlite3");
        CreateDynastyFixture(sqlitePath);

        var statusService = new FakeStatusQueryService();
        var placeService = new FakePlaceLookupService();
        var window = new StatusQueryWindow(sqlitePath, localization, statusService, placeService);

        try {
            window.Show();

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar").Text?.Contains("Loaded 2 status codes and 2 dynasties.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Status query window did not finish loading filters."
            );

            var txtSelectedStatuses = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedStatuses");
            var txtSelectedPlaces = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedPlaces");
            Assert.Equal("All statuses", txtSelectedStatuses.Text);
            Assert.Equal("All places", txtSelectedPlaces.Text);

            var btnRunQuery = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnRunQuery");
            btnRunQuery.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var gridRecords = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridRecords");
            var gridPeople = AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridPeople");
            var txtStatusBar = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtStatusBar");

            await AvaloniaUiTestHelper.WaitUntilAsync(
                () => ((IEnumerable<object>?)gridRecords.ItemsSource)?.Count() == 1
                      && ((IEnumerable<object>?)gridPeople.ItemsSource)?.Count() == 1
                      && txtStatusBar.Text?.Contains("Loaded 1 status records for 1 people.", StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5),
                "Status query window did not populate query results."
            );

            Assert.Equal(1, statusService.QueryCallCount);
            Assert.Equal(sqlitePath, statusService.LastSqlitePath);
        } finally {
            window.Close();
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    private sealed class FakeStatusQueryService : IStatusQueryService {
        public int QueryCallCount { get; private set; }
        public string? LastSqlitePath { get; private set; }

        public Task<IReadOnlyList<StatusCodeOption>> GetStatusCodesAsync(string sqlitePath, CancellationToken cancellationToken = default) {
            IReadOnlyList<StatusCodeOption> codes = new[] {
                new StatusCodeOption("S1", "Scholar", "學者", 2),
                new StatusCodeOption("S2", "Minister", "大臣", 1)
            };
            return Task.FromResult(codes);
        }

        public Task<StatusPickerData> GetStatusPickerDataAsync(string sqlitePath, CancellationToken cancellationToken = default) {
            var root = new StatusTypeNode(
                StatusPickerData.RootCode,
                null,
                null,
                new[] {
                    new StatusTypeNode("11", "Scholarship", "學術", Array.Empty<StatusTypeNode>(), new[] { "S1" }),
                    new StatusTypeNode("12", "Politics", "政治", Array.Empty<StatusTypeNode>(), new[] { "S2" })
                },
                new[] { "S1", "S2" }
            );

            var result = new StatusPickerData(
                root,
                new[] {
                    new StatusCodeOption("S1", "Scholar", "學者", 2),
                    new StatusCodeOption("S2", "Minister", "大臣", 1)
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    ["S1"] = "11",
                    ["S2"] = "12"
                }
            );

            return Task.FromResult(result);
        }

        public Task<StatusQueryResult> QueryAsync(string sqlitePath, StatusQueryRequest request, CancellationToken cancellationToken = default) {
            QueryCallCount++;
            LastSqlitePath = sqlitePath;

            var records = new[] {
                new StatusQueryRecord(
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
                    XCoord: 119.3,
                    YCoord: 26.08,
                    Sequence: 1,
                    Status: "學者",
                    StatusCode: "S1",
                    FirstYear: 1071,
                    FirstNianhaoCode: 1,
                    FirstNianhao: "元祐",
                    FirstNianhaoPinyin: "yuanyou",
                    FirstNianhaoYear: 3,
                    FirstRangeCode: 1,
                    FirstRangeDesc: "Range",
                    FirstRange: "時限",
                    LastYear: 1074,
                    LastNianhaoCode: 2,
                    LastNianhao: "紹聖",
                    LastNianhaoPinyin: "shaosheng",
                    LastNianhaoYear: 1,
                    LastRangeCode: 2,
                    LastRangeDesc: "Range B",
                    LastRange: "時限乙",
                    Supplement: "supplement",
                    SourceId: 1,
                    Source: "來源",
                    Pages: "12-13",
                    Notes: "note"
                )
            };

            var people = new[] {
                new StatusQueryPerson(
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
                    XCoord: 119.3,
                    YCoord: 26.08,
                    XyCount: 1,
                    StatusCount: 1
                )
            };

            return Task.FromResult(new StatusQueryResult(records, people));
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
