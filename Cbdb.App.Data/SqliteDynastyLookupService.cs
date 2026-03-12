using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteDynastyLookupService : IDynastyLookupService {
    public async Task<IReadOnlyList<DynastyOption>> GetDynastiesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<DynastyOption>();
        }

        var options = new List<DynastyOption>();

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    c_dy,
    c_dynasty,
    c_dynasty_chn,
    c_start,
    c_end
FROM DYNASTIES
ORDER BY
    CASE
        WHEN TRIM(COALESCE(c_dynasty_chn, '')) IN ('朝鮮', '朝鲜', '韓國', '韩国')
          OR LOWER(TRIM(COALESCE(c_dynasty, ''))) IN ('choson', 'joseon', 'korea', 'south korea')
        THEN 1
        ELSE 0
    END,
    CASE WHEN c_start IS NULL THEN 1 ELSE 0 END,
    c_start,
    CASE WHEN c_end IS NULL THEN 1 ELSE 0 END,
    c_end,
    c_dy;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            options.Add(new DynastyOption(
                DynastyId: reader.GetInt32(0),
                Name: reader.IsDBNull(1) ? null : reader.GetString(1),
                NameChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                StartYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                EndYear: reader.IsDBNull(4) ? null : reader.GetInt32(4)
            ));
        }

        return options;
    }
}
