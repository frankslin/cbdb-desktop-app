using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class RealFixturePersonBrowserWindowTests {
    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_SearchByPersonId_LoadsDetail() {
        var fixturePath = ResolveFixturePath();
        var personId = await FindPersonIdAsync(
            fixturePath,
            """
EXISTS (SELECT 1 FROM STATUS_DATA sd WHERE sd.c_personid = b.c_personid)
AND b.c_dy IS NOT NULL
AND b.c_index_addr_id IS NOT NULL
AND COALESCE(TRIM(b.c_name_chn), '') <> ''
AND EXISTS (
    SELECT 1
    FROM ADDR_CODES ac
    WHERE ac.c_addr_id = b.c_index_addr_id
      AND COALESCE(NULLIF(TRIM(ac.c_name_chn), ''), NULLIF(TRIM(ac.c_name), '')) IS NOT NULL
)
"""
        );

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        txtKeyword.Text = personId.ToString();
        window.Show();

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text == personId.ToString(),
            TimeSpan.FromSeconds(10),
            "Person detail did not load from the fixture database."
        );

        var valName = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValName");
        var valDynasty = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValDynasty");
        var valIndexAddress = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValIndexAddress");

        Assert.False(string.IsNullOrWhiteSpace(valName.Text));
        Assert.False(string.IsNullOrWhiteSpace(valDynasty.Text));
        Assert.False(string.IsNullOrWhiteSpace(valIndexAddress.Text));

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_RendersLazyTabs() {
        var fixturePath = ResolveFixturePath();
        var personId = await FindPersonIdAsync(
            fixturePath,
            """
EXISTS (SELECT 1 FROM BIOG_ADDR_DATA a WHERE a.c_personid = b.c_personid)
AND EXISTS (SELECT 1 FROM ALTNAME_DATA an WHERE an.c_personid = b.c_personid)
AND EXISTS (SELECT 1 FROM STATUS_DATA sd WHERE sd.c_personid = b.c_personid)
"""
        );

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword").Text = personId.ToString();
        window.Show();

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text == personId.ToString(),
            TimeSpan.FromSeconds(10),
            "Selected person detail did not load from the fixture database."
        );

        var tabs = AvaloniaUiTestHelper.FindRequiredControl<TabControl>(window, "MainTabs");
        var tabAddresses = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAddresses");
        var tabAltNames = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAltNames");
        var tabStatus = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabStatus");

        tabs.SelectedItem = tabAddresses;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AddressesPanel").Children.Count > 0,
            TimeSpan.FromSeconds(10),
            "Addresses tab did not render fixture-backed records."
        );

        tabs.SelectedItem = tabAltNames;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AltNamesPanel").Children.Count > 0,
            TimeSpan.FromSeconds(10),
            "Alt names tab did not render fixture-backed records."
        );

        tabs.SelectedItem = tabStatus;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "StatusPanel").Children.Count > 0,
            TimeSpan.FromSeconds(10),
            "Status tab did not render fixture-backed records."
        );

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_SearchButton_UpdatesSelection() {
        var fixturePath = ResolveFixturePath();
        var firstPersonId = await FindPersonIdAsync(fixturePath, "b.c_personid > 0");
        var secondPersonId = await FindPersonIdAsync(fixturePath, $"b.c_personid > {firstPersonId}");

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        var btnSearch = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnSearch");
        window.Show();

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridPeople").SelectedItem is not null,
            TimeSpan.FromSeconds(10),
            "Initial person list did not load."
        );

        txtKeyword.Text = firstPersonId.ToString();
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text == firstPersonId.ToString(),
            TimeSpan.FromSeconds(10),
            "First search result did not load."
        );

        txtKeyword.Text = secondPersonId.ToString();
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text == secondPersonId.ToString(),
            TimeSpan.FromSeconds(10),
            "Second search result did not replace the selected person."
        );

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_SearchByChineseName_LoadsExpectedPerson() {
        var fixturePath = ResolveFixturePath();
        var sample = await FindPersonSearchSampleAsync(
            fixturePath,
            "COALESCE(TRIM(b.c_name_chn), '') <> '' AND b.c_personid > 0",
            "b.c_name_chn"
        );

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        var btnSearch = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnSearch");
        window.Show();

        txtKeyword.Text = sample.Keyword;
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await WaitForSelectedPersonAsync(window, sample.PersonId, "Chinese-name search did not load the expected person.");

        Assert.Equal(sample.PersonId.ToString(), AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text);
        Assert.Equal(sample.NameChn, AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValNameChn").Text);

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_SearchByAltName_LoadsExpectedPerson() {
        var fixturePath = ResolveFixturePath();
        var sample = await FindAltNameSearchSampleAsync(fixturePath);

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        window.Show();

        txtKeyword.Text = sample.Keyword;
        txtKeyword.RaiseEvent(new global::Avalonia.Input.KeyEventArgs {
            RoutedEvent = global::Avalonia.Input.InputElement.KeyDownEvent,
            Key = global::Avalonia.Input.Key.Enter
        });

        await WaitForSelectedPersonAsync(window, sample.PersonId, "Alt-name search did not load the expected person.");

        Assert.Equal(sample.NameChn, AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValNameChn").Text);

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_ClearButton_RestoresDefaultResults() {
        var fixturePath = ResolveFixturePath();
        var defaultFirstPersonId = await FindPersonIdAsync(fixturePath, "1=1");
        var targetPersonId = await FindPersonIdAsync(fixturePath, $"b.c_personid > {defaultFirstPersonId}");

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        var btnSearch = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnSearch");
        var btnClear = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnClear");
        window.Show();

        txtKeyword.Text = targetPersonId.ToString();
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, targetPersonId, "Targeted search did not load before clearing.");

        btnClear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, defaultFirstPersonId, "Clear did not restore the default result set.");

        Assert.Equal(string.Empty, txtKeyword.Text);
        Assert.StartsWith("Results:", AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtRecord").Text);

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_TabHeaders_ShowFixtureCounts() {
        var fixturePath = ResolveFixturePath();
        var sample = await FindPersonWithCountsAsync(
            fixturePath,
            """
EXISTS (SELECT 1 FROM BIOG_ADDR_DATA a WHERE a.c_personid = b.c_personid)
AND EXISTS (SELECT 1 FROM ALTNAME_DATA an WHERE an.c_personid = b.c_personid)
AND EXISTS (SELECT 1 FROM STATUS_DATA sd WHERE sd.c_personid = b.c_personid)
"""
        );

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword").Text = sample.PersonId.ToString();
        window.Show();

        await WaitForSelectedPersonAsync(window, sample.PersonId, "Fixture person did not load for tab-count assertions.");

        var tabAddresses = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAddresses");
        var tabAltNames = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAltNames");
        var tabStatus = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabStatus");

        Assert.Equal($"Addresses ({sample.AddressCount:N0})", Convert.ToString(tabAddresses.Header));
        Assert.Equal($"Alt. Names ({sample.AltNameCount:N0})", Convert.ToString(tabAltNames.Header));
        Assert.Equal($"Status ({sample.StatusCount:N0})", Convert.ToString(tabStatus.Header));

        window.Close();
    }

    [AvaloniaFact]
    public async Task PersonBrowserWindow_RealFixture_HistoryBackAndForward_RestoreSelectionAndTab() {
        var fixturePath = ResolveFixturePath();
        var firstPersonId = await FindPersonIdAsync(
            fixturePath,
            "EXISTS (SELECT 1 FROM STATUS_DATA sd WHERE sd.c_personid = b.c_personid)"
        );
        var secondPersonId = await FindPersonIdAsync(
            fixturePath,
            $"b.c_personid <> {firstPersonId} AND EXISTS (SELECT 1 FROM BIOG_ADDR_DATA a WHERE a.c_personid = b.c_personid)"
        );

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var window = new PersonBrowserWindow(fixturePath, localization);
        var txtKeyword = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtKeyword");
        var btnSearch = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnSearch");
        var btnBack = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnHistoryBack");
        var btnForward = AvaloniaUiTestHelper.FindRequiredControl<Button>(window, "BtnHistoryForward");
        var tabs = AvaloniaUiTestHelper.FindRequiredControl<TabControl>(window, "MainTabs");
        var tabStatus = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabStatus");
        var tabAddresses = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAddresses");
        window.Show();

        txtKeyword.Text = firstPersonId.ToString();
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, firstPersonId, "First history person did not load.");

        tabs.SelectedItem = tabStatus;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "StatusPanel").Children.Count > 0,
            TimeSpan.FromSeconds(10),
            "Status tab did not load for the first history entry."
        );

        txtKeyword.Text = secondPersonId.ToString();
        btnSearch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, secondPersonId, "Second history person did not load.");

        tabs.SelectedItem = tabAddresses;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AddressesPanel").Children.Count > 0,
            TimeSpan.FromSeconds(10),
            "Addresses tab did not load for the second history entry."
        );

        btnBack.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, firstPersonId, "Back navigation did not restore the first person.");
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => ReferenceEquals(tabs.SelectedItem, tabStatus),
            TimeSpan.FromSeconds(10),
            "Back navigation did not restore the selected tab."
        );

        btnForward.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForSelectedPersonAsync(window, secondPersonId, "Forward navigation did not restore the second person.");
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => ReferenceEquals(tabs.SelectedItem, tabAddresses),
            TimeSpan.FromSeconds(10),
            "Forward navigation did not restore the selected tab."
        );

        window.Close();
    }

    private static string ResolveFixturePath() {
        var directPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "latest-fixture.sqlite3");
        if (File.Exists(directPath)) {
            return directPath;
        }

        var repoPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Cbdb.App.Avalonia.Tests",
            "Fixtures",
            "latest-fixture.sqlite3"
        ));
        if (File.Exists(repoPath)) {
            return repoPath;
        }

        throw new FileNotFoundException("Fixture database not found.");
    }

    private static async Task<int> FindPersonIdAsync(string sqlitePath, string whereClause) {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT b.c_personid
FROM BIOG_MAIN b
WHERE {whereClause}
ORDER BY b.c_personid
LIMIT 1;
""";

        var result = await command.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value) {
            throw new InvalidOperationException($"No fixture person matched: {whereClause}");
        }

        return Convert.ToInt32(result);
    }

    private static async Task WaitForSelectedPersonAsync(PersonBrowserWindow window, int personId, string failureMessage) {
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValPersonId").Text == personId.ToString(),
            TimeSpan.FromSeconds(10),
            failureMessage
        );
    }

    private static async Task<PersonSearchSample> FindPersonSearchSampleAsync(string sqlitePath, string whereClause, string keywordExpression) {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT
    b.c_personid,
    b.c_name_chn,
    {keywordExpression}
FROM BIOG_MAIN b
WHERE {whereClause}
ORDER BY b.c_personid
LIMIT 1;
""";

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) {
            throw new InvalidOperationException($"No fixture search sample matched: {whereClause}");
        }

        return new PersonSearchSample(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
        );
    }

    private static async Task<PersonSearchSample> FindAltNameSearchSampleAsync(string sqlitePath) {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    b.c_personid,
    b.c_name_chn,
    COALESCE(NULLIF(TRIM(a.c_alt_name_chn), ''), NULLIF(TRIM(a.c_alt_name), '')) AS keyword
FROM BIOG_MAIN b
JOIN ALTNAME_DATA a ON a.c_personid = b.c_personid
WHERE COALESCE(NULLIF(TRIM(a.c_alt_name_chn), ''), NULLIF(TRIM(a.c_alt_name), '')) IS NOT NULL
ORDER BY b.c_personid, a.c_sequence
LIMIT 1;
""";

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) {
            throw new InvalidOperationException("No fixture alt-name sample was found.");
        }

        return new PersonSearchSample(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
        );
    }

    private static async Task<PersonCountSample> FindPersonWithCountsAsync(string sqlitePath, string whereClause) {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT
    b.c_personid,
    (SELECT COUNT(*) FROM BIOG_ADDR_DATA a WHERE a.c_personid = b.c_personid),
    (SELECT COUNT(*) FROM ALTNAME_DATA an WHERE an.c_personid = b.c_personid),
    (SELECT COUNT(*) FROM STATUS_DATA sd WHERE sd.c_personid = b.c_personid)
FROM BIOG_MAIN b
WHERE {whereClause}
ORDER BY b.c_personid
LIMIT 1;
""";

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) {
            throw new InvalidOperationException($"No fixture person matched count query: {whereClause}");
        }

        return new PersonCountSample(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3)
        );
    }

    private sealed record PersonSearchSample(int PersonId, string NameChn, string Keyword);
    private sealed record PersonCountSample(int PersonId, int AddressCount, int AltNameCount, int StatusCount);
}
