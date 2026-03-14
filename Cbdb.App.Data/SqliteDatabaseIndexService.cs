using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteDatabaseIndexService : IDatabaseIndexService {
    private static readonly IReadOnlyList<(string Name, string Sql)> RecommendedIndexes = new[] {
        (
            "idx_posting_data_personid_posting",
            """
CREATE INDEX IF NOT EXISTS idx_posting_data_personid_posting
ON POSTING_DATA (c_personid, c_posting_id);
"""
        ),
        (
            "idx_posted_to_office_personid_posting_office_seq",
            """
CREATE INDEX IF NOT EXISTS idx_posted_to_office_personid_posting_office_seq
ON POSTED_TO_OFFICE_DATA (c_personid, c_posting_id, c_office_id, c_sequence);
"""
        ),
        (
            "idx_posted_to_addr_personid_posting_office_addr",
            """
CREATE INDEX IF NOT EXISTS idx_posted_to_addr_personid_posting_office_addr
ON POSTED_TO_ADDR_DATA (c_personid, c_posting_id, c_office_id, c_addr_id);
"""
        )
    };

    public async Task<DatabaseIndexCheckResult> CheckRecommendedIndexesAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath)) {
            return new DatabaseIndexCheckResult(RecommendedIndexes.Select(index => index.Name).ToArray());
        }

        var existingIndexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await OpenConnectionAsync(sqlitePath, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT name
FROM sqlite_master
WHERE type = 'index'
  AND name IS NOT NULL;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            if (!reader.IsDBNull(0)) {
                existingIndexNames.Add(reader.GetString(0));
            }
        }

        var missing = RecommendedIndexes
            .Select(index => index.Name)
            .Where(name => !existingIndexNames.Contains(name))
            .ToArray();

        return new DatabaseIndexCheckResult(missing);
    }

    public async Task EnsureRecommendedIndexesAsync(
        string sqlitePath,
        IProgress<DatabaseIndexProgress>? progress = null,
        CancellationToken cancellationToken = default
    ) {
        var check = await CheckRecommendedIndexesAsync(sqlitePath, cancellationToken);
        if (check.HasAllIndexes) {
            return;
        }

        var missing = RecommendedIndexes
            .Where(index => check.MissingIndexNames.Contains(index.Name, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        await using var connection = await OpenConnectionAsync(sqlitePath, cancellationToken);

        for (var i = 0; i < missing.Length; i++) {
            cancellationToken.ThrowIfCancellationRequested();

            var definition = missing[i];
            await using var command = connection.CreateCommand();
            command.CommandText = definition.Sql;
            await command.ExecuteNonQueryAsync(cancellationToken);

            progress?.Report(new DatabaseIndexProgress(i + 1, missing.Length, definition.Name));
        }
    }

    private static async Task<SqliteConnection> OpenConnectionAsync(string sqlitePath, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(sqlitePath)) {
            throw new InvalidOperationException("SQLite path is empty.");
        }

        if (!File.Exists(sqlitePath)) {
            throw new FileNotFoundException($"SQLite file not found: {sqlitePath}", sqlitePath);
        }

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadWrite
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
