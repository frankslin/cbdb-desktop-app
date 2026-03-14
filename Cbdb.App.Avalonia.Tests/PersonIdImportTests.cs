using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class PersonIdImportTests {
    [Fact]
    public void Parse_WithHeaderedCsv_UsesCPersonIdColumnAndSkipsInvalidRows() {
        const string content = """
c_name,c_personid,c_notes
Alpha,2,ok
Beta,abc,bad
Gamma,3,ok
Delta,,missing
Epsilon,2,duplicate
""";

        var personIds = PersonIdImportParser.Parse(content);

        Assert.Equal(new[] { 2, 3 }, personIds);
    }

    [Fact]
    public void Parse_WithoutHeader_TreatsEachLineAsPersonId() {
        const string content = """
1
bad
2

0
3
""";

        var personIds = PersonIdImportParser.Parse(content);

        Assert.Equal(new[] { 1, 2, 3 }, personIds);
    }

    [Fact]
    public async Task GetPeopleByIdsAsync_PreservesInputOrderAndSkipsMissingIds() {
        var sqlitePath = await CreatePeopleImportTestDatabaseAsync();

        try {
            var service = new SqlitePersonBrowserService();
            var rows = await service.GetPeopleByIdsAsync(sqlitePath, new[] { 3, 99, 1, 3, 2 });

            Assert.Equal(new[] { 3, 1, 2 }, rows.Select(row => row.PersonId).ToArray());
            Assert.Equal("丙", rows[0].NameChn);
            Assert.Equal("甲地", rows[1].IndexAddress);
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    private static async Task<string> CreatePeopleImportTestDatabaseAsync() {
        var path = Path.Combine(Path.GetTempPath(), $"cbdb-people-import-{Guid.NewGuid():N}.sqlite3");

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
    c_index_year INTEGER,
    c_index_addr_id INTEGER
);

CREATE TABLE ADDR_CODES (
    c_addr_id INTEGER PRIMARY KEY,
    c_name_chn TEXT,
    c_name TEXT
);

INSERT INTO ADDR_CODES (c_addr_id, c_name_chn, c_name) VALUES
(10, '甲地', 'Place A'),
(20, '乙地', 'Place B');

INSERT INTO BIOG_MAIN (c_personid, c_name_chn, c_name, c_index_year, c_index_addr_id) VALUES
(1, '甲', 'Jia', 1001, 10),
(2, '乙', 'Yi', 1002, 20),
(3, '丙', 'Bing', 1003, NULL);
""";
        await command.ExecuteNonQueryAsync();

        return path;
    }
}
