using Cbdb.App.Core;
using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class DatabaseIndexServiceTests {
    [Fact]
    public async Task CheckRecommendedIndexesAsync_ReturnsMissingIndexes_WhenIndexesDoNotExist() {
        var sqlitePath = await CreateTempDatabaseAsync();

        try {
            var service = new SqliteDatabaseIndexService();
            var result = await service.CheckRecommendedIndexesAsync(sqlitePath);

            Assert.False(result.HasAllIndexes);
            Assert.Contains("idx_posting_data_personid_posting", result.MissingIndexNames);
            Assert.Contains("idx_posted_to_office_personid_posting_office_seq", result.MissingIndexNames);
            Assert.Contains("idx_posted_to_addr_personid_posting_office_addr", result.MissingIndexNames);
        } finally {
            File.Delete(sqlitePath);
        }
    }

    [Fact]
    public async Task EnsureRecommendedIndexesAsync_CreatesMissingIndexes_AndReportsProgress() {
        var sqlitePath = await CreateTempDatabaseAsync();

        try {
            var service = new SqliteDatabaseIndexService();
            var updates = new List<DatabaseIndexProgress>();
            var progress = new Progress<DatabaseIndexProgress>(update => updates.Add(update));

            await service.EnsureRecommendedIndexesAsync(sqlitePath, progress);

            var result = await service.CheckRecommendedIndexesAsync(sqlitePath);
            Assert.True(result.HasAllIndexes);
            Assert.Equal(3, updates.Count);
            Assert.Equal(3, updates[^1].CompletedSteps);
            Assert.Equal(3, updates[^1].TotalSteps);

            var createdIndexes = await LoadIndexNamesAsync(sqlitePath);
            Assert.Contains("idx_posting_data_personid_posting", createdIndexes);
            Assert.Contains("idx_posted_to_office_personid_posting_office_seq", createdIndexes);
            Assert.Contains("idx_posted_to_addr_personid_posting_office_addr", createdIndexes);
        } finally {
            File.Delete(sqlitePath);
        }
    }

    private static async Task<string> CreateTempDatabaseAsync() {
        var path = Path.Combine(Path.GetTempPath(), $"cbdb-index-test-{Guid.NewGuid():N}.sqlite3");

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ConnectionString);
        await connection.OpenAsync();

        var sql = """
CREATE TABLE POSTING_DATA (
    c_personid INTEGER,
    c_posting_id INTEGER NOT NULL,
    PRIMARY KEY (c_posting_id)
);

CREATE TABLE POSTED_TO_OFFICE_DATA (
    c_personid INTEGER,
    c_office_id INTEGER NOT NULL,
    c_posting_id INTEGER NOT NULL,
    c_sequence INTEGER,
    PRIMARY KEY (c_office_id, c_posting_id)
);

CREATE TABLE POSTED_TO_ADDR_DATA (
    c_posting_id INTEGER NOT NULL,
    c_personid INTEGER,
    c_office_id INTEGER NOT NULL,
    c_addr_id INTEGER NOT NULL,
    PRIMARY KEY (c_addr_id, c_office_id, c_posting_id)
);
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();

        return path;
    }

    private static async Task<HashSet<string>> LoadIndexNamesAsync(string sqlitePath) {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT name
FROM sqlite_master
WHERE type = 'index'
  AND name IS NOT NULL;
""";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            if (!reader.IsDBNull(0)) {
                names.Add(reader.GetString(0));
            }
        }

        return names;
    }
}
