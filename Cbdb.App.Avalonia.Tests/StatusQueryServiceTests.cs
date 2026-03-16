using Cbdb.App.Core;
using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class StatusQueryServiceTests {
    [Fact]
    public async Task FixtureDatabase_PlaceLookupAndStatusQuery_RunSuccessfully() {
        var fixturePath = ResolveFixturePath();

        var placeService = new SqlitePlaceLookupService();
        var places = await placeService.GetPlacesAsync(fixturePath);
        Assert.NotEmpty(places);

        var selectedStatusCodes = await LoadStatusCodesAsync(fixturePath, limit: 3);
        Assert.NotEmpty(selectedStatusCodes);

        var statusService = new SqliteStatusQueryService();
        var result = await statusService.QueryAsync(fixturePath, new StatusQueryRequest(
            PersonKeyword: null,
            StatusCodes: selectedStatusCodes,
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

    [Fact]
    public async Task GetPlacesAsync_ReturnsPlacesWithDisambiguationFields() {
        var sqlitePath = CreateTempSqlitePath();

        try {
            await using var connection = await OpenConnectionAsync(sqlitePath);
            await ExecuteBatchAsync(connection, """
CREATE TABLE ADDR_CODES (
    c_addr_id INTEGER PRIMARY KEY,
    c_name TEXT,
    c_name_chn TEXT,
    c_admin_type TEXT,
    x_coord REAL,
    y_coord REAL
);
CREATE TABLE ADDR_BELONGS_DATA (
    c_addr_id INTEGER,
    c_belongs_to INTEGER,
    c_firstyear INTEGER,
    c_lastyear INTEGER
);
INSERT INTO ADDR_CODES (c_addr_id, c_name, c_name_chn, c_admin_type, x_coord, y_coord) VALUES
    (1, 'Fu Zhou', '福州', 'Prefecture', 119.30, 26.08),
    (2, 'Min County', '閩縣', 'County', 119.31, 26.09);
INSERT INTO ADDR_BELONGS_DATA (c_addr_id, c_belongs_to, c_firstyear, c_lastyear) VALUES
    (2, 1, 960, 1279);
""");

            var service = new SqlitePlaceLookupService();
            var places = await service.GetPlacesAsync(sqlitePath);

            var minCounty = Assert.Single(places.Where(place => place.AddressId == 2));
            Assert.Equal("County", minCounty.AdminType);
            Assert.Equal(960, minCounty.FirstYear);
            Assert.Equal(1279, minCounty.LastYear);
            Assert.Equal(1, minCounty.BelongsToId);
            Assert.Equal("Fu Zhou", minCounty.BelongsToName);
            Assert.Equal("福州", minCounty.BelongsToNameChn);
        } finally {
            TryDelete(sqlitePath);
        }
    }

    [Fact]
    public async Task QueryAsync_FiltersByPlace_AndIncludesSubordinateUnits() {
        var sqlitePath = CreateTempSqlitePath();

        try {
            await using var connection = await OpenConnectionAsync(sqlitePath);
            await ExecuteBatchAsync(connection, """
CREATE TABLE BIOG_MAIN (
    c_personid INTEGER PRIMARY KEY,
    c_name TEXT,
    c_name_chn TEXT,
    c_name_rm TEXT,
    c_name_proper TEXT,
    c_surname TEXT,
    c_surname_chn TEXT,
    c_mingzi TEXT,
    c_mingzi_chn TEXT,
    c_index_year INTEGER,
    c_dy INTEGER,
    c_index_addr_id INTEGER,
    c_index_addr_type_code INTEGER,
    c_female INTEGER,
    c_index_year_type_code INTEGER
);
CREATE TABLE ALTNAME_DATA (
    c_personid INTEGER,
    c_alt_name TEXT,
    c_alt_name_chn TEXT
);
CREATE TABLE STATUS_DATA (
    c_personid INTEGER,
    c_status_code INTEGER,
    c_sequence INTEGER,
    c_firstyear INTEGER,
    c_fy_nh_code INTEGER,
    c_fy_nh_year INTEGER,
    c_fy_range INTEGER,
    c_lastyear INTEGER,
    c_ly_nh_code INTEGER,
    c_ly_nh_year INTEGER,
    c_ly_range INTEGER,
    c_supplement TEXT,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);
CREATE TABLE STATUS_CODES (
    c_status_code INTEGER PRIMARY KEY,
    c_status_desc TEXT,
    c_status_desc_chn TEXT
);
CREATE TABLE DYNASTIES (
    c_dy INTEGER PRIMARY KEY,
    c_dynasty TEXT,
    c_dynasty_chn TEXT,
    c_start INTEGER,
    c_end INTEGER
);
CREATE TABLE ADDR_CODES (
    c_addr_id INTEGER PRIMARY KEY,
    c_name TEXT,
    c_name_chn TEXT,
    x_coord REAL,
    y_coord REAL
);
CREATE TABLE BIOG_ADDR_CODES (
    c_addr_type INTEGER PRIMARY KEY,
    c_addr_desc TEXT,
    c_addr_desc_chn TEXT
);
CREATE TABLE INDEXYEAR_TYPE_CODES (
    c_index_year_type_code INTEGER PRIMARY KEY,
    c_index_year_type_desc TEXT,
    c_index_year_type_hz TEXT
);
CREATE TABLE NIAN_HAO (
    c_nianhao_id INTEGER PRIMARY KEY,
    c_nianhao_chn TEXT,
    c_nianhao_pin TEXT
);
CREATE TABLE YEAR_RANGE_CODES (
    c_range_code INTEGER PRIMARY KEY,
    c_range TEXT,
    c_range_chn TEXT
);
CREATE TABLE TEXT_CODES (
    c_textid INTEGER PRIMARY KEY,
    c_title TEXT,
    c_title_chn TEXT
);
CREATE TABLE ZZZ_BELONGS_TO (
    c_addr_id INTEGER,
    c_belongs_to INTEGER
);
INSERT INTO DYNASTIES (c_dy, c_dynasty, c_dynasty_chn, c_start, c_end) VALUES
    (1, 'Song', '宋', 960, 1279);
INSERT INTO BIOG_ADDR_CODES (c_addr_type, c_addr_desc, c_addr_desc_chn) VALUES
    (7, 'Native place', '籍貫');
INSERT INTO ADDR_CODES (c_addr_id, c_name, c_name_chn, x_coord, y_coord) VALUES
    (10, 'Fu Zhou', '福州', 119.30, 26.08),
    (11, 'Min County', '閩縣', 119.31, 26.09),
    (12, 'Hang Zhou', '杭州', 120.15, 30.28);
INSERT INTO ZZZ_BELONGS_TO (c_addr_id, c_belongs_to) VALUES
    (11, 10);
INSERT INTO INDEXYEAR_TYPE_CODES (c_index_year_type_code, c_index_year_type_desc, c_index_year_type_hz) VALUES
    (1, 'Index year type', '索引年類型');
INSERT INTO TEXT_CODES (c_textid, c_title, c_title_chn) VALUES
    (1, 'Source', '來源');
INSERT INTO STATUS_CODES (c_status_code, c_status_desc, c_status_desc_chn) VALUES
    (100, 'Official', '官員');
INSERT INTO BIOG_MAIN (c_personid, c_name, c_name_chn, c_index_year, c_dy, c_index_addr_id, c_index_addr_type_code, c_female, c_index_year_type_code) VALUES
    (1, 'Su Shi', '蘇軾', 1080, 1, 11, 7, 0, 1),
    (2, 'Xin Qiji', '辛棄疾', 1170, 1, 12, 7, 0, 1);
INSERT INTO STATUS_DATA (c_personid, c_status_code, c_sequence, c_firstyear, c_lastyear, c_supplement, c_source, c_pages, c_notes) VALUES
    (1, 100, 1, 1070, 1085, 'A', 1, '12-13', 'note-1'),
    (2, 100, 1, 1160, 1180, 'B', 1, '20', 'note-2');
""");

            var service = new SqliteStatusQueryService();

            var directPlaceResult = await service.QueryAsync(sqlitePath, new StatusQueryRequest(
                PersonKeyword: null,
                StatusCodes: new[] { "100" },
                PlaceIds: new[] { 10 },
                IncludeSubordinateUnits: false,
                UseIndexYearRange: false,
                IndexYearFrom: -200,
                IndexYearTo: 1911,
                UseDynastyRange: false,
                DynastyFrom: null,
                DynastyTo: null
            ));

            Assert.Empty(directPlaceResult.People);

            var subordinatePlaceResult = await service.QueryAsync(sqlitePath, new StatusQueryRequest(
                PersonKeyword: null,
                StatusCodes: new[] { "100" },
                PlaceIds: new[] { 10 },
                IncludeSubordinateUnits: true,
                UseIndexYearRange: false,
                IndexYearFrom: -200,
                IndexYearTo: 1911,
                UseDynastyRange: false,
                DynastyFrom: null,
                DynastyTo: null
            ));

            var person = Assert.Single(subordinatePlaceResult.People);
            Assert.Equal(1, person.PersonId);
            Assert.Equal(11, person.IndexAddressId);
            Assert.Equal("M", person.Sex);
            Assert.Equal("籍貫", person.IndexAddressType);

            var record = Assert.Single(subordinatePlaceResult.Records);
            Assert.Equal("籍貫", record.IndexAddressType);
        } finally {
            TryDelete(sqlitePath);
        }
    }

    private static string CreateTempSqlitePath() {
        return Path.Combine(Path.GetTempPath(), $"cbdb-status-tests-{Guid.NewGuid():N}.sqlite3");
    }

    private static async Task<SqliteConnection> OpenConnectionAsync(string sqlitePath) {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ConnectionString);

        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteBatchAsync(SqliteConnection connection, string sql) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string ResolveFixturePath() {
        var directPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "latest-fixture.sqlite3");
        if (File.Exists(directPath)) {
            return directPath;
        }

        var repoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Cbdb.App.Avalonia.Tests", "Fixtures", "latest-fixture.sqlite3"));
        if (File.Exists(repoPath)) {
            return repoPath;
        }

        throw new FileNotFoundException("Fixture database not found.");
    }

    private static async Task<IReadOnlyList<string>> LoadStatusCodesAsync(string sqlitePath, int limit) {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT CAST(sd.c_status_code AS TEXT)
FROM STATUS_DATA sd
GROUP BY sd.c_status_code
ORDER BY COUNT(*) DESC, sd.c_status_code
LIMIT {limit};
""";

        var statusCodes = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            if (!reader.IsDBNull(0)) {
                statusCodes.Add(reader.GetString(0));
            }
        }

        return statusCodes;
    }

    private static void TryDelete(string path) {
        try {
            TestSqliteFileHelper.Delete(path);
        } catch {
        }
    }
}
