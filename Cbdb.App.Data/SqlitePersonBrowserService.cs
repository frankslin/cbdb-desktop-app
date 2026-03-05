using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqlitePersonBrowserService : IPersonBrowserService {
    public async Task<IReadOnlyList<PersonListItem>> SearchAsync(string sqlitePath, string? keyword, int limit = 200, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonListItem>();
        }

        limit = Math.Clamp(limit, 1, 1000);
        var list = new List<PersonListItem>(limit);

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var normalized = keyword?.Trim();
        var hasKeyword = !string.IsNullOrWhiteSpace(normalized);

        await using var command = connection.CreateCommand();
        command.CommandText = hasKeyword
            ? @"
SELECT
    b.c_personid,
    b.c_name,
    b.c_name_chn,
    b.c_index_year,
    d.c_dynasty,
    d.c_dynasty_chn
FROM BIOG_MAIN b
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
WHERE (
       b.c_name LIKE $kw
    OR b.c_name_chn LIKE $kw
    OR EXISTS (
        SELECT 1
        FROM ALTNAME_DATA a
        WHERE a.c_personid = b.c_personid
          AND (a.c_alt_name LIKE $kw OR a.c_alt_name_chn LIKE $kw)
    )
)
ORDER BY b.c_name, b.c_personid
LIMIT $limit;"
            : @"
SELECT
    b.c_personid,
    b.c_name,
    b.c_name_chn,
    b.c_index_year,
    d.c_dynasty,
    d.c_dynasty_chn
FROM BIOG_MAIN b
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
ORDER BY b.c_name, b.c_personid
LIMIT $limit;";

        command.Parameters.AddWithValue("$limit", limit);
        if (hasKeyword) {
            command.Parameters.AddWithValue("$kw", $"%{normalized}%");
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            list.Add(new PersonListItem(
                PersonId: reader.GetInt32(0),
                Name: reader.IsDBNull(1) ? null : reader.GetString(1),
                NameChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Dynasty: reader.IsDBNull(4) ? null : reader.GetString(4),
                DynastyChn: reader.IsDBNull(5) ? null : reader.GetString(5)
            ));
        }

        return list;
    }

    public async Task<PersonDetail?> GetDetailAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return null;
        }

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    b.c_personid,
    b.c_name,
    b.c_name_chn,
    b.c_index_year,
    d.c_dynasty,
    d.c_dynasty_chn,
    b.c_birthyear,
    b.c_deathyear,
    b.c_female,
    ac.c_name,
    ac.c_name_chn
FROM BIOG_MAIN b
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
WHERE b.c_personid = $personId
LIMIT 1;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            return null;
        }

        var gender = reader.IsDBNull(8)
            ? "Unknown"
            : (reader.GetInt32(8) == -1 ? "F" : "M");

        return new PersonDetail(
            PersonId: reader.GetInt32(0),
            Name: reader.IsDBNull(1) ? null : reader.GetString(1),
            NameChn: reader.IsDBNull(2) ? null : reader.GetString(2),
            IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
            Dynasty: reader.IsDBNull(4) ? null : reader.GetString(4),
            DynastyChn: reader.IsDBNull(5) ? null : reader.GetString(5),
            BirthYear: reader.IsDBNull(6) ? null : reader.GetInt32(6),
            DeathYear: reader.IsDBNull(7) ? null : reader.GetInt32(7),
            Gender: gender,
            IndexAddress: reader.IsDBNull(9) ? null : reader.GetString(9),
            IndexAddressChn: reader.IsDBNull(10) ? null : reader.GetString(10),
            AltNameCount: await CountAsync(connection, "SELECT COUNT(*) FROM ALTNAME_DATA WHERE c_personid = $personId", personId, cancellationToken),
            KinCount: await CountAsync(connection, "SELECT COUNT(*) FROM KIN_DATA WHERE c_personid = $personId", personId, cancellationToken),
            AssocCount: await CountAsync(connection, "SELECT COUNT(*) FROM ASSOC_DATA WHERE c_personid = $personId", personId, cancellationToken),
            OfficeCount: await CountAsync(connection, "SELECT COUNT(*) FROM POSTED_TO_OFFICE_DATA WHERE c_personid = $personId", personId, cancellationToken),
            EntryCount: await CountAsync(connection, "SELECT COUNT(*) FROM ENTRY_DATA WHERE c_personid = $personId", personId, cancellationToken),
            StatusCount: await CountAsync(connection, "SELECT COUNT(*) FROM STATUS_DATA WHERE c_personid = $personId", personId, cancellationToken),
            TextCount: await CountAsync(connection, "SELECT COUNT(*) FROM BIOG_TEXT_DATA WHERE c_personid = $personId", personId, cancellationToken)
        );
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string sql, int personId, CancellationToken cancellationToken) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$personId", personId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }
}
