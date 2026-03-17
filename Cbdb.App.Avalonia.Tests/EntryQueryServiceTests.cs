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

    [Fact]
    public async Task QueryAsync_MapsExamFieldAttemptCountAndXyCount() {
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
CREATE TABLE ALTNAME_DATA (c_personid INTEGER, c_alt_name TEXT, c_alt_name_chn TEXT);
CREATE TABLE ENTRY_DATA (
    c_personid INTEGER,
    c_entry_code INTEGER,
    c_sequence INTEGER,
    c_exam_rank TEXT,
    c_kin_code INTEGER,
    c_kin_id INTEGER,
    c_assoc_code INTEGER,
    c_assoc_id INTEGER,
    c_year INTEGER,
    c_age INTEGER,
    c_entry_nh_id INTEGER,
    c_entry_nh_year INTEGER,
    c_entry_range INTEGER,
    c_inst_code INTEGER,
    c_inst_name_code INTEGER,
    c_exam_field TEXT,
    c_entry_addr_id INTEGER,
    c_parental_status_code INTEGER,
    c_attempt_count INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT,
    c_posting_notes TEXT
);
CREATE TABLE ENTRY_CODES (c_entry_code INTEGER PRIMARY KEY, c_entry_desc TEXT, c_entry_desc_chn TEXT);
CREATE TABLE DYNASTIES (c_dy INTEGER PRIMARY KEY, c_dynasty TEXT, c_dynasty_chn TEXT, c_start INTEGER, c_end INTEGER);
CREATE TABLE ADDR_CODES (c_addr_id INTEGER PRIMARY KEY, c_name TEXT, c_name_chn TEXT, x_coord REAL, y_coord REAL);
CREATE TABLE BIOG_ADDR_CODES (c_addr_type INTEGER PRIMARY KEY, c_addr_desc TEXT, c_addr_desc_chn TEXT);
CREATE TABLE INDEXYEAR_TYPE_CODES (c_index_year_type_code INTEGER PRIMARY KEY, c_index_year_type_desc TEXT, c_index_year_type_hz TEXT);
CREATE TABLE NIAN_HAO (c_nianhao_id INTEGER PRIMARY KEY, c_nianhao_chn TEXT, c_nianhao_pin TEXT);
CREATE TABLE YEAR_RANGE_CODES (c_range_code INTEGER PRIMARY KEY, c_range TEXT, c_range_chn TEXT);
CREATE TABLE KINSHIP_CODES (c_kincode INTEGER PRIMARY KEY, c_kinrel TEXT, c_kinrel_chn TEXT);
CREATE TABLE ASSOC_CODES (c_assoc_code INTEGER PRIMARY KEY, c_assoc_desc TEXT, c_assoc_desc_chn TEXT);
CREATE TABLE SOCIAL_INSTITUTION_NAME_CODES (c_inst_name_code INTEGER PRIMARY KEY, c_inst_name_hz TEXT, c_inst_name_py TEXT);
CREATE TABLE PARENTAL_STATUS_CODES (c_parental_status_code INTEGER PRIMARY KEY, c_parental_status_desc TEXT, c_parental_status_desc_chn TEXT);
CREATE TABLE TEXT_CODES (c_textid INTEGER PRIMARY KEY, c_title TEXT, c_title_chn TEXT);
INSERT INTO DYNASTIES VALUES (1, 'Song', '宋', 960, 1279);
INSERT INTO INDEXYEAR_TYPE_CODES VALUES (1, 'Index Year', '指數年');
INSERT INTO BIOG_ADDR_CODES VALUES (7, 'Native place', '籍貫');
INSERT INTO ENTRY_CODES VALUES (100, 'Examination', '科舉');
INSERT INTO ADDR_CODES VALUES (10, 'Fu Zhou', '福州', 119.3, 26.08);
INSERT INTO PARENTAL_STATUS_CODES VALUES (1, 'Official household', '官戶');
INSERT INTO TEXT_CODES VALUES (1, 'Source', '來源');
INSERT INTO BIOG_MAIN VALUES
    (1, 'Su Shi', '蘇軾', NULL, NULL, NULL, NULL, NULL, NULL, 1080, 1, 10, 7, 0, 1),
    (2, 'Xin Qiji', '辛棄疾', NULL, NULL, NULL, NULL, NULL, NULL, 1170, 1, 10, 7, 0, 1);
INSERT INTO ENTRY_DATA VALUES
    (1, 100, 1, '1', 0, 0, 0, 0, 1070, 21, NULL, NULL, NULL, 0, 0, 'Classics', 10, 1, 3, 1, '12-13', 'note-1', 'posting-1'),
    (2, 100, 1, '2', 0, 0, 0, 0, 1170, 25, NULL, NULL, NULL, 0, 0, 'Policy', 10, 1, 2, 1, '20', 'note-2', 'posting-2');
""");

            var service = new SqliteEntryQueryService();
            var result = await service.QueryAsync(sqlitePath, new EntryQueryRequest(
                PersonKeyword: null,
                EntryCodes: new[] { "100" },
                PlaceIds: Array.Empty<int>(),
                IncludeSubordinateUnits: false,
                UseIndexYearRange: false,
                IndexYearFrom: -200,
                IndexYearTo: 1911,
                UseDynastyRange: false,
                DynastyFrom: null,
                DynastyTo: null
            ));

            var first = Assert.Single(result.Records.Where(record => record.PersonId == 1));
            Assert.Equal("Classics", first.ExamField);
            Assert.Equal(3, first.AttemptCount);
            Assert.Equal(2, first.EntryXyCount);
            Assert.Equal("籍貫", first.IndexAddressType);

            var person = Assert.Single(result.People.Where(item => item.PersonId == 1));
            Assert.Equal("籍貫", person.IndexAddressType);
        } finally {
            TryDelete(sqlitePath);
        }
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

    private static string CreateTempSqlitePath() {
        var fileName = $"cbdb-entry-query-{Guid.NewGuid():N}.sqlite3";
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    private static async Task<Microsoft.Data.Sqlite.SqliteConnection> OpenConnectionAsync(string sqlitePath) {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={sqlitePath}");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteBatchAsync(Microsoft.Data.Sqlite.SqliteConnection connection, string sql) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static void TryDelete(string sqlitePath) {
        TestSqliteFileHelper.Delete(sqlitePath);
    }
}
