using Cbdb.App.Core;
using Cbdb.App.Data;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class OfficeQueryServiceTests {
    [Fact]
    public async Task FixtureDatabase_OfficeQuery_RunSuccessfully() {
        var fixturePath = ResolveFixturePath();
        var service = new SqliteOfficeQueryService();
        var pickerData = await service.GetOfficePickerDataAsync(fixturePath);

        Assert.NotEmpty(pickerData.AllOfficeCodes);

        var selectedCodes = pickerData.AllOfficeCodes
            .Where(option => option.UsageCount > 0)
            .Take(3)
            .Select(option => option.Code)
            .ToArray();
        Assert.NotEmpty(selectedCodes);

        var result = await service.QueryAsync(fixturePath, new OfficeQueryRequest(
            PersonKeyword: null,
            OfficeCodes: selectedCodes,
            PlaceIds: Array.Empty<int>(),
            IncludeSubordinateUnits: false,
            UseIndexYearRange: false,
            IndexYearFrom: -200,
            IndexYearTo: 1911,
            UseOfficeYearRange: false,
            OfficeYearFrom: -200,
            OfficeYearTo: 1911,
            UseDynastyRange: false,
            DynastyFrom: null,
            DynastyTo: null,
            Limit: 50
        ));

        Assert.NotEmpty(result.Records);
        Assert.NotEmpty(result.People);
    }

    [Fact]
    public async Task QueryAsync_MapsOfficeFieldsAndAggregatesPeople() {
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
CREATE TABLE POSTED_TO_OFFICE_DATA (
    c_personid INTEGER,
    c_office_id INTEGER,
    c_posting_id INTEGER,
    c_sequence INTEGER,
    c_firstyear INTEGER,
    c_fy_nh_code INTEGER,
    c_fy_nh_year INTEGER,
    c_fy_range INTEGER,
    c_fy_month INTEGER,
    c_fy_intercalary INTEGER,
    c_fy_day INTEGER,
    c_fy_day_gz INTEGER,
    c_lastyear INTEGER,
    c_ly_nh_code INTEGER,
    c_ly_nh_year INTEGER,
    c_ly_range INTEGER,
    c_ly_month INTEGER,
    c_ly_intercalary INTEGER,
    c_ly_day INTEGER,
    c_ly_day_gz INTEGER,
    c_appt_type_code INTEGER,
    c_assume_office_code INTEGER,
    c_inst_code INTEGER,
    c_inst_name_code INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT,
    c_office_category_id INTEGER,
    c_dy INTEGER
);
CREATE TABLE POSTED_TO_ADDR_DATA (
    c_posting_id INTEGER,
    c_personid INTEGER,
    c_office_id INTEGER,
    c_addr_id INTEGER
);
CREATE TABLE OFFICE_CODES (
    c_office_id INTEGER PRIMARY KEY,
    c_dy INTEGER,
    c_office_pinyin TEXT,
    c_office_chn TEXT,
    c_office_trans TEXT
);
CREATE TABLE OFFICE_TYPE_TREE (
    c_office_type_node_id TEXT PRIMARY KEY,
    c_office_type_desc TEXT,
    c_office_type_desc_chn TEXT,
    c_parent_id TEXT
);
CREATE TABLE OFFICE_CODE_TYPE_REL (c_office_id INTEGER, c_office_tree_id TEXT);
CREATE TABLE OFFICE_CATEGORIES (c_office_category_id INTEGER PRIMARY KEY, c_category_desc TEXT, c_category_desc_chn TEXT);
CREATE TABLE APPOINTMENT_CODES (c_appt_code INTEGER PRIMARY KEY, c_appt_desc TEXT, c_appt_desc_chn TEXT);
CREATE TABLE ASSUME_OFFICE_CODES (c_assume_office_code INTEGER PRIMARY KEY, c_assume_office_desc TEXT, c_assume_office_desc_chn TEXT);
CREATE TABLE SOCIAL_INSTITUTION_NAME_CODES (c_inst_name_code INTEGER PRIMARY KEY, c_inst_name_hz TEXT, c_inst_name_py TEXT);
CREATE TABLE DYNASTIES (c_dy INTEGER PRIMARY KEY, c_dynasty TEXT, c_dynasty_chn TEXT, c_start INTEGER, c_end INTEGER);
CREATE TABLE ADDR_CODES (c_addr_id INTEGER PRIMARY KEY, c_name TEXT, c_name_chn TEXT, x_coord REAL, y_coord REAL);
CREATE TABLE BIOG_ADDR_CODES (c_addr_type INTEGER PRIMARY KEY, c_addr_desc TEXT, c_addr_desc_chn TEXT);
CREATE TABLE INDEXYEAR_TYPE_CODES (c_index_year_type_code INTEGER PRIMARY KEY, c_index_year_type_desc TEXT, c_index_year_type_hz TEXT);
CREATE TABLE NIAN_HAO (c_nianhao_id INTEGER PRIMARY KEY, c_nianhao_chn TEXT, c_nianhao_pin TEXT);
CREATE TABLE YEAR_RANGE_CODES (c_range_code INTEGER PRIMARY KEY, c_range TEXT, c_range_chn TEXT);
CREATE TABLE GANZHI_CODES (c_ganzhi_code INTEGER PRIMARY KEY, c_ganzhi_chn TEXT, c_ganzhi_py TEXT);
CREATE TABLE TEXT_CODES (c_textid INTEGER PRIMARY KEY, c_title TEXT, c_title_chn TEXT);
CREATE TABLE ZZZ_BELONGS_TO (c_addr_id INTEGER, c_belongs_to INTEGER);

INSERT INTO DYNASTIES VALUES (1, 'Song', '宋', 960, 1279);
INSERT INTO INDEXYEAR_TYPE_CODES VALUES (1, 'Index Year', '指數年');
INSERT INTO BIOG_ADDR_CODES VALUES (7, 'Native place', '籍貫');
INSERT INTO NIAN_HAO VALUES (1, '元祐', 'Yuanyou');
INSERT INTO YEAR_RANGE_CODES VALUES (1, 'Range', '時限');
INSERT INTO GANZHI_CODES VALUES (1, '甲子', 'jiazi');
INSERT INTO GANZHI_CODES VALUES (2, '乙丑', 'yichou');
INSERT INTO APPOINTMENT_CODES VALUES (1, 'Appointment', '任命');
INSERT INTO ASSUME_OFFICE_CODES VALUES (1, 'Assume', '到任');
INSERT INTO OFFICE_CATEGORIES VALUES (1, 'Civil', '文職');
INSERT INTO SOCIAL_INSTITUTION_NAME_CODES VALUES (1, '中書省', 'Zhongshu Sheng');
INSERT INTO TEXT_CODES VALUES (1, 'Source', '來源');
INSERT INTO ADDR_CODES VALUES
    (10, 'Fu Zhou', '福州', 119.3, 26.08),
    (20, 'Lin An', '臨安', 120.2, 30.3);
INSERT INTO BIOG_MAIN VALUES
    (1, 'Su Shi', '蘇軾', NULL, NULL, NULL, NULL, NULL, NULL, 1080, 1, 10, 7, 0, 1),
    (2, 'Xin Qiji', '辛棄疾', NULL, NULL, NULL, NULL, NULL, NULL, 1170, 1, 10, 7, 0, 1);
INSERT INTO OFFICE_CODES VALUES (100, 1, 'zhi zhou', '知州', 'Prefect');
INSERT INTO OFFICE_TYPE_TREE VALUES ('0', 'All Offices Categories', '所有門類', NULL);
INSERT INTO OFFICE_TYPE_TREE VALUES ('01', 'Song Office', '宋代官職', '0');
INSERT INTO OFFICE_CODE_TYPE_REL VALUES (100, '01');
INSERT INTO POSTED_TO_OFFICE_DATA VALUES
    (1, 100, 1000, 1, 1071, 1, 3, 1, 4, 1, 15, 1, 1074, 1, 6, 1, 8, 0, 22, 2, 1, 1, 0, 1, 1, '12-13', 'note-1', 1, 1),
    (2, 100, 1001, 1, 1171, 1, 2, 1, 2, 0, 9, 1, 1173, 1, 4, 1, 5, 0, 11, 2, 1, 1, 0, 1, 1, '20', 'note-2', 1, 1);
INSERT INTO POSTED_TO_ADDR_DATA VALUES
    (1000, 1, 100, 20),
    (1001, 2, 100, 20);
""");

            var service = new SqliteOfficeQueryService();
            var result = await service.QueryAsync(sqlitePath, new OfficeQueryRequest(
                PersonKeyword: null,
                OfficeCodes: new[] { "100" },
                PlaceIds: Array.Empty<int>(),
                IncludeSubordinateUnits: false,
                UseIndexYearRange: false,
                IndexYearFrom: -200,
                IndexYearTo: 1911,
                UseOfficeYearRange: false,
                OfficeYearFrom: -200,
                OfficeYearTo: 1911,
                UseDynastyRange: false,
                DynastyFrom: null,
                DynastyTo: null
            ));

            var first = Assert.Single(result.Records.Where(record => record.PersonId == 1));
            Assert.Equal("知州", first.Office);
            Assert.Equal("任命", first.AppointmentType);
            Assert.Equal("到任", first.AssumeOffice);
            Assert.Equal("文職", first.Category);
            Assert.Equal("中書省", first.Institution);
            Assert.Equal("臨安", first.OfficeAddress);
            Assert.Equal(2, first.OfficeXyCount);
            Assert.Equal(4, first.FirstMonth);
            Assert.True(first.FirstIntercalary);
            Assert.Equal(15, first.FirstDay);
            Assert.Equal("甲子", first.FirstGanzhi);
            Assert.Equal(8, first.LastMonth);
            Assert.False(first.LastIntercalary);
            Assert.Equal(22, first.LastDay);
            Assert.Equal("乙丑", first.LastGanzhi);

            var person = Assert.Single(result.People.Where(item => item.PersonId == 1));
            Assert.Equal("臨安", person.OfficeAddress);
            Assert.Equal(2, person.XyCount);
            Assert.Equal(1, person.PostingCount);
        } finally {
            TryDelete(sqlitePath);
        }
    }

    [Fact]
    public async Task QueryAsync_FiltersByOfficeYearRange() {
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
CREATE TABLE POSTED_TO_OFFICE_DATA (
    c_personid INTEGER,
    c_office_id INTEGER,
    c_posting_id INTEGER,
    c_sequence INTEGER,
    c_firstyear INTEGER,
    c_fy_nh_code INTEGER,
    c_fy_nh_year INTEGER,
    c_fy_range INTEGER,
    c_fy_month INTEGER,
    c_fy_intercalary INTEGER,
    c_fy_day INTEGER,
    c_fy_day_gz INTEGER,
    c_lastyear INTEGER,
    c_ly_nh_code INTEGER,
    c_ly_nh_year INTEGER,
    c_ly_range INTEGER,
    c_ly_month INTEGER,
    c_ly_intercalary INTEGER,
    c_ly_day INTEGER,
    c_ly_day_gz INTEGER,
    c_appt_type_code INTEGER,
    c_assume_office_code INTEGER,
    c_inst_code INTEGER,
    c_inst_name_code INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT,
    c_office_category_id INTEGER,
    c_dy INTEGER
);
CREATE TABLE POSTED_TO_ADDR_DATA (
    c_posting_id INTEGER,
    c_personid INTEGER,
    c_office_id INTEGER,
    c_addr_id INTEGER
);
CREATE TABLE OFFICE_CODES (
    c_office_id INTEGER PRIMARY KEY,
    c_dy INTEGER,
    c_office_pinyin TEXT,
    c_office_chn TEXT,
    c_office_trans TEXT
);
CREATE TABLE OFFICE_TYPE_TREE (
    c_office_type_node_id TEXT PRIMARY KEY,
    c_office_type_desc TEXT,
    c_office_type_desc_chn TEXT,
    c_parent_id TEXT
);
CREATE TABLE OFFICE_CODE_TYPE_REL (c_office_id INTEGER, c_office_tree_id TEXT);
CREATE TABLE OFFICE_CATEGORIES (c_office_category_id INTEGER PRIMARY KEY, c_category_desc TEXT, c_category_desc_chn TEXT);
CREATE TABLE APPOINTMENT_CODES (c_appt_code INTEGER PRIMARY KEY, c_appt_desc TEXT, c_appt_desc_chn TEXT);
CREATE TABLE ASSUME_OFFICE_CODES (c_assume_office_code INTEGER PRIMARY KEY, c_assume_office_desc TEXT, c_assume_office_desc_chn TEXT);
CREATE TABLE SOCIAL_INSTITUTION_NAME_CODES (c_inst_name_code INTEGER PRIMARY KEY, c_inst_name_hz TEXT, c_inst_name_py TEXT);
CREATE TABLE DYNASTIES (c_dy INTEGER PRIMARY KEY, c_dynasty TEXT, c_dynasty_chn TEXT, c_start INTEGER, c_end INTEGER);
CREATE TABLE ADDR_CODES (c_addr_id INTEGER PRIMARY KEY, c_name TEXT, c_name_chn TEXT, x_coord REAL, y_coord REAL);
CREATE TABLE BIOG_ADDR_CODES (c_addr_type INTEGER PRIMARY KEY, c_addr_desc TEXT, c_addr_desc_chn TEXT);
CREATE TABLE INDEXYEAR_TYPE_CODES (c_index_year_type_code INTEGER PRIMARY KEY, c_index_year_type_desc TEXT, c_index_year_type_hz TEXT);
CREATE TABLE NIAN_HAO (c_nianhao_id INTEGER PRIMARY KEY, c_nianhao_chn TEXT, c_nianhao_pin TEXT);
CREATE TABLE YEAR_RANGE_CODES (c_range_code INTEGER PRIMARY KEY, c_range TEXT, c_range_chn TEXT);
CREATE TABLE GANZHI_CODES (c_ganzhi_code INTEGER PRIMARY KEY, c_ganzhi_chn TEXT, c_ganzhi_py TEXT);
CREATE TABLE TEXT_CODES (c_textid INTEGER PRIMARY KEY, c_title TEXT, c_title_chn TEXT);
CREATE TABLE ZZZ_BELONGS_TO (c_addr_id INTEGER, c_belongs_to INTEGER);

INSERT INTO DYNASTIES VALUES (1, 'Song', '宋', 960, 1279);
INSERT INTO BIOG_MAIN VALUES
    (1, 'A', '甲', NULL, NULL, NULL, NULL, NULL, NULL, 1080, 1, NULL, NULL, 0, NULL),
    (2, 'B', '乙', NULL, NULL, NULL, NULL, NULL, NULL, 1080, 1, NULL, NULL, 0, NULL);
INSERT INTO OFFICE_CODES VALUES (100, 1, 'zhi zhou', '知州', 'Prefect');
INSERT INTO POSTED_TO_OFFICE_DATA VALUES
    (1, 100, 1000, 1, 1071, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1074, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1),
    (2, 100, 1001, 1, 1171, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1173, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1);
""");

            var service = new SqliteOfficeQueryService();
            var result = await service.QueryAsync(sqlitePath, new OfficeQueryRequest(
                PersonKeyword: null,
                OfficeCodes: new[] { "100" },
                PlaceIds: Array.Empty<int>(),
                IncludeSubordinateUnits: false,
                UseIndexYearRange: false,
                IndexYearFrom: -200,
                IndexYearTo: 1911,
                UseOfficeYearRange: true,
                OfficeYearFrom: 1070,
                OfficeYearTo: 1100,
                UseDynastyRange: false,
                DynastyFrom: null,
                DynastyTo: null
            ));

            var record = Assert.Single(result.Records);
            Assert.Equal(1, record.PersonId);
            Assert.Single(result.People);
            Assert.Equal(1, result.People[0].PersonId);
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
        var fileName = $"cbdb-office-query-{Guid.NewGuid():N}.sqlite3";
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
