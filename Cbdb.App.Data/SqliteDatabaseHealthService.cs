using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteDatabaseHealthService : IDatabaseHealthService {
    public async Task<DatabaseHealthResult> CheckAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath)) {
            return new DatabaseHealthResult(false, "SQLite path is empty.", null, null, null, null);
        }

        if (!File.Exists(sqlitePath)) {
            return new DatabaseHealthResult(false, $"SQLite file not found: {sqlitePath}", null, null, null, null);
        }

        try {
            var builder = new SqliteConnectionStringBuilder {
                DataSource = sqlitePath,
                Mode = SqliteOpenMode.ReadWrite
            };

            await using var connection = new SqliteConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var personCount = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM BIOG_MAIN", cancellationToken);
            var altNameCount = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM ALTNAME_DATA", cancellationToken);
            var kinCount = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM KIN_DATA", cancellationToken);
            var assocCount = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM ASSOC_DATA", cancellationToken);

            var message = $"Connected. BIOG_MAIN={personCount:N0}, ALTNAME_DATA={altNameCount:N0}, KIN_DATA={kinCount:N0}, ASSOC_DATA={assocCount:N0}";
            return new DatabaseHealthResult(true, message, personCount, altNameCount, kinCount, assocCount);
        } catch (Exception ex) {
            return new DatabaseHealthResult(false, ex.Message, null, null, null, null);
        }
    }

    private static async Task<int> ScalarIntAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }
}
