using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class PersonBrowserKinshipTests {
    [Fact]
    public async Task GetKinshipsAsync_ExpandedMode_IncludesKinOfKin() {
        var sqlitePath = await CreateKinshipTestDatabaseAsync();

        try {
            var service = new SqlitePersonBrowserService();

            var direct = await service.GetKinshipsAsync(sqlitePath, 1, expandNetwork: false);
            var expanded = await service.GetKinshipsAsync(sqlitePath, 1, expandNetwork: true);

            Assert.Equal(2, direct.Count);
            Assert.Contains(direct, item => item.KinPersonId == 2);
            Assert.Contains(direct, item => item.KinPersonId == 4);
            Assert.DoesNotContain(direct, item => item.KinPersonId == 3);
            Assert.All(direct, item => Assert.False(item.IsDerived));

            Assert.Contains(expanded, item => item.KinPersonId == 2);
            Assert.Contains(expanded, item => item.KinPersonId == 3);
            Assert.Contains(expanded, item => item.KinPersonId == 4);
            Assert.Equal(new[] { 2, 4, 3 }, expanded.Select(item => item.KinPersonId).ToArray());

            var indirect = Assert.Single(expanded.Where(item => item.KinPersonId == 3));
            Assert.True(indirect.IsDerived);
            Assert.Equal("父 / father > 兄 / elder brother", indirect.Kinship);
            Assert.Equal(1, indirect.UpStep);
            Assert.Equal(1, indirect.CollateralStep);
            Assert.Contains("甲 / Jia > 乙 / Yi (父 / father) > 丙 / Bing (兄 / elder brother)", indirect.Notes ?? string.Empty);
            Assert.DoesNotContain("乙 / Yi > 乙 / Yi", indirect.Notes ?? string.Empty);
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    [Fact]
    public async Task GetKinshipsAsync_ExpandedMode_ReducesSiblingChains() {
        var sqlitePath = await CreateSiblingReductionTestDatabaseAsync();

        try {
            var service = new SqlitePersonBrowserService();

            var expanded = await service.GetKinshipsAsync(sqlitePath, 1, expandNetwork: true);

            var reduced = Assert.Single(expanded.Where(item => item.KinPersonId == 3));
            Assert.True(reduced.IsDerived);
            Assert.Equal("兄弟 / brother", reduced.Kinship);
            Assert.Equal(0, reduced.UpStep);
            Assert.Equal(0, reduced.DownStep);
            Assert.Equal(0, reduced.MarriageStep);
            Assert.Equal(1, reduced.CollateralStep);
            Assert.Contains("BB => 兄弟 / brother", reduced.Notes ?? string.Empty);
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    private static async Task<string> CreateKinshipTestDatabaseAsync() {
        var path = Path.Combine(Path.GetTempPath(), $"cbdb-kinship-test-{Guid.NewGuid():N}.sqlite3");

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE BIOG_MAIN (
    c_personid INTEGER PRIMARY KEY,
    c_name_chn TEXT,
    c_name TEXT,
    c_female INTEGER
);

CREATE TABLE KINSHIP_CODES (
    c_kincode INTEGER PRIMARY KEY,
    c_kinrel_simplified TEXT,
    c_kinrel_chn TEXT,
    c_kinrel TEXT,
    c_upstep INTEGER,
    c_dwnstep INTEGER,
    c_marstep INTEGER,
    c_colstep INTEGER
);

CREATE TABLE KIN_DATA (
    c_personid INTEGER,
    c_kin_id INTEGER,
    c_kin_code INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE TEXT_CODES (
    c_textid INTEGER PRIMARY KEY,
    c_title_chn TEXT,
    c_title TEXT
);

INSERT INTO BIOG_MAIN (c_personid, c_name_chn, c_name, c_female) VALUES
(1, '甲', 'Jia', 0),
(2, '乙', 'Yi', 0),
(3, '丙', 'Bing', 0),
(4, '丁', 'Ding', 0);

INSERT INTO KINSHIP_CODES (c_kincode, c_kinrel_simplified, c_kinrel_chn, c_kinrel, c_upstep, c_dwnstep, c_marstep, c_colstep) VALUES
(10, 'F', '父', 'father', 1, 0, 0, 0),
(20, 'B', '兄', 'elder brother', 0, 0, 0, 1),
(30, 'FFF', '高祖', 'great-grandfather', 3, 0, 0, 0);

INSERT INTO TEXT_CODES (c_textid, c_title_chn, c_title) VALUES
(1, '史料', 'Source');

INSERT INTO KIN_DATA (c_personid, c_kin_id, c_kin_code, c_source, c_pages, c_notes) VALUES
(1, 2, 10, 1, '1', 'direct father'),
(2, 3, 20, 1, '2', 'father''s brother'),
(1, 4, 30, 1, '3', 'direct great-grandfather');
""";
        await command.ExecuteNonQueryAsync();

        return path;
    }

    private static async Task<string> CreateSiblingReductionTestDatabaseAsync() {
        var path = Path.Combine(Path.GetTempPath(), $"cbdb-kinship-reduction-test-{Guid.NewGuid():N}.sqlite3");

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE BIOG_MAIN (
    c_personid INTEGER PRIMARY KEY,
    c_name_chn TEXT,
    c_name TEXT,
    c_female INTEGER
);

CREATE TABLE KINSHIP_CODES (
    c_kincode INTEGER PRIMARY KEY,
    c_kinrel_simplified TEXT,
    c_kinrel_chn TEXT,
    c_kinrel TEXT,
    c_upstep INTEGER,
    c_dwnstep INTEGER,
    c_marstep INTEGER,
    c_colstep INTEGER
);

CREATE TABLE KIN_DATA (
    c_personid INTEGER,
    c_kin_id INTEGER,
    c_kin_code INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE TEXT_CODES (
    c_textid INTEGER PRIMARY KEY,
    c_title_chn TEXT,
    c_title TEXT
);

INSERT INTO BIOG_MAIN (c_personid, c_name_chn, c_name, c_female) VALUES
(1, '甲', 'Jia', 0),
(2, '乙', 'Yi', 0),
(3, '丙', 'Bing', 0);

INSERT INTO KINSHIP_CODES (c_kincode, c_kinrel_simplified, c_kinrel_chn, c_kinrel, c_upstep, c_dwnstep, c_marstep, c_colstep) VALUES
(10, 'B', '兄弟', 'brother', 0, 0, 0, 1);

INSERT INTO TEXT_CODES (c_textid, c_title_chn, c_title) VALUES
(1, '史料', 'Source');

INSERT INTO KIN_DATA (c_personid, c_kin_id, c_kin_code, c_source, c_pages, c_notes) VALUES
(1, 2, 10, 1, '1', 'older brother'),
(2, 3, 10, 1, '2', 'brother of brother');
""";
        await command.ExecuteNonQueryAsync();

        return path;
    }
}
