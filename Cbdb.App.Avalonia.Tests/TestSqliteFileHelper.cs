using Microsoft.Data.Sqlite;

namespace Cbdb.App.Avalonia.Tests;

internal static class TestSqliteFileHelper {
    public static void Delete(string sqlitePath) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return;
        }

        SqliteConnection.ClearAllPools();

        IOException? lastException = null;
        for (var attempt = 0; attempt < 5; attempt++) {
            try {
                File.Delete(sqlitePath);
                return;
            } catch (IOException ex) {
                lastException = ex;
                Thread.Sleep(100);
            }
        }

        if (File.Exists(sqlitePath)) {
            throw lastException ?? new IOException($"Could not delete temporary SQLite file: {sqlitePath}");
        }
    }
}
