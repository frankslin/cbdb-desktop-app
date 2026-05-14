using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

internal static class SqliteSchemaCompatibility {
    public static async Task<string> GetPostingAppointmentCodeExpressionAsync(
        SqliteConnection connection,
        string tableAlias,
        CancellationToken cancellationToken
    ) {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"POSTED_TO_OFFICE_DATA\");";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            if (!reader.IsDBNull(1)) {
                columns.Add(reader.GetString(1));
            }
        }

        return columns.Contains("c_appt_type_code")
            ? $"{tableAlias}.c_appt_type_code"
            : $"{tableAlias}.c_appt_code";
    }
}
