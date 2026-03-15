using Cbdb.App.Core;
using Cbdb.App.Data;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class EntryQueryServiceTests {
    [Fact]
    public async Task FixtureDatabase_EntryQuery_RunSuccessfully() {
        var fixturePath = ResolveFixturePath();
        var service = new SqliteEntryQueryService();
        var pickerData = await service.GetEntryPickerDataAsync(fixturePath);

        Assert.NotEmpty(pickerData.AllEntryCodes);

        var selectedCodes = pickerData.AllEntryCodes
            .Where(option => option.UsageCount > 0)
            .Take(3)
            .Select(option => option.Code)
            .ToArray();
        Assert.NotEmpty(selectedCodes);
        var result = await service.QueryAsync(fixturePath, new EntryQueryRequest(
            PersonKeyword: null,
            EntryCodes: selectedCodes,
            PlaceIds: Array.Empty<int>(),
            IncludeSubordinateUnits: false,
            UseIndexYearRange: false,
            IndexYearFrom: -200,
            IndexYearTo: 1911,
            UseDynastyRange: false,
            DynastyFrom: null,
            DynastyTo: null,
            Limit: 50
        ));

        Assert.NotEmpty(result.Records);
        Assert.NotEmpty(result.People);
    }

    private static string ResolveFixturePath() {
        var candidates = new[] {
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "latest-fixture.sqlite3"),
            Path.Combine(AppContext.BaseDirectory, "latest-fixture.sqlite3"),
            Path.Combine(Directory.GetCurrentDirectory(), "Cbdb.App.Avalonia.Tests", "Fixtures", "latest-fixture.sqlite3"),
            Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "latest-fixture.sqlite3")
        };

        var fixturePath = candidates.FirstOrDefault(File.Exists);
        Assert.False(string.IsNullOrWhiteSpace(fixturePath), "Unable to locate latest-fixture.sqlite3 for test execution.");
        return fixturePath!;
    }
}
