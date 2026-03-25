using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class DynastyLookupServiceTests {
    [Fact]
    public async Task GetDynastiesAsync_PutsGoryeoInTrailingDynastyGroup() {
        var sqlitePath = Path.Combine(Path.GetTempPath(), $"cbdb-dynasty-tests-{Guid.NewGuid():N}.sqlite3");

        try {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = sqlitePath
            }.ConnectionString);
            await connection.OpenAsync();

            await using (var command = connection.CreateCommand()) {
                command.CommandText = """
CREATE TABLE DYNASTIES (
    c_dy INTEGER PRIMARY KEY,
    c_dynasty TEXT,
    c_dynasty_chn TEXT,
    c_start INTEGER,
    c_end INTEGER
);
INSERT INTO DYNASTIES VALUES (1, 'unknown', '未詳', NULL, NULL);
INSERT INTO DYNASTIES VALUES (2, 'Song', '宋', 960, 1279);
INSERT INTO DYNASTIES VALUES (3, 'Yuan', '元', 1271, 1368);
INSERT INTO DYNASTIES VALUES (4, 'Silla', '新羅', 668, 935);
INSERT INTO DYNASTIES VALUES (5, 'Goryeo', '高麗', 918, 1392);
INSERT INTO DYNASTIES VALUES (6, 'Joseon', '朝鮮', 1392, 1897);
""";
                await command.ExecuteNonQueryAsync();
            }

            var service = new SqliteDynastyLookupService();
            var result = await service.GetDynastiesAsync(sqlitePath);

            Assert.Equal("未詳", result[0].NameChn);
            Assert.Equal(new[] { "宋", "元" }, result.Skip(1).Take(2).Select(option => option.NameChn));
            Assert.Equal(new[] { "新羅", "高麗", "朝鮮" }, result.Skip(3).Select(option => option.NameChn));
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }
}
