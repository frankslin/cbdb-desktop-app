using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqlitePersonBrowserService : IPersonBrowserService {
    public async Task<IReadOnlyList<PersonListItem>> SearchAsync(
        string sqlitePath,
        string? keyword,
        int limit = 200,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonListItem>();
        }

        limit = Math.Clamp(limit, 1, 1000);
        offset = Math.Max(0, offset);

        var list = new List<PersonListItem>(limit);

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var normalized = NormalizeSqliteText(keyword);
        var hasKeyword = !string.IsNullOrWhiteSpace(normalized);

        await using var command = connection.CreateCommand();        command.CommandText = hasKeyword
            ? @"
WITH matched_ids AS (
    SELECT b.c_personid
    FROM BIOG_MAIN b
    WHERE
           b.c_name LIKE $kw
        OR b.c_name_chn LIKE $kw
        OR b.c_name_rm LIKE $kw
        OR b.c_name_proper LIKE $kw
        OR b.c_surname LIKE $kw
        OR b.c_surname_chn LIKE $kw
        OR b.c_mingzi LIKE $kw
        OR b.c_mingzi_chn LIKE $kw
    UNION
    SELECT a.c_personid
    FROM ALTNAME_DATA a
    WHERE a.c_alt_name LIKE $kw OR a.c_alt_name_chn LIKE $kw
)
SELECT
    b.c_personid,
    b.c_name,
    b.c_name_chn,
    b.c_index_year,
    d.c_dynasty,
    d.c_dynasty_chn
FROM matched_ids m
JOIN BIOG_MAIN b ON b.c_personid = m.c_personid
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
ORDER BY b.c_personid
LIMIT $limit OFFSET $offset;"
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
ORDER BY b.c_personid
LIMIT $limit OFFSET $offset;";

        command.CommandTimeout = 12;

        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);

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

    public async Task<IReadOnlyList<PersonRelatedItem>> GetRelatedItemsAsync(
        string sqlitePath,
        int personId,
        PersonRelatedCategory category,
        int limit = 200,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonRelatedItem>();
        }

        limit = Math.Clamp(limit, 1, 1000);

        var (tableName, preferredColumns) = GetRelatedTableConfig(category);
        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var columns = await GetTableColumnsAsync(connection, tableName, cancellationToken);
        if (!columns.Contains("c_personid", StringComparer.OrdinalIgnoreCase)) {
            return Array.Empty<PersonRelatedItem>();
        }

        var picked = preferredColumns
            .Where(c => columns.Contains(c, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        if (picked.Count == 0) {
            picked = columns
                .Where(c => !string.Equals(c, "c_personid", StringComparison.OrdinalIgnoreCase))
                .Where(c => !c.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList();
        }

        if (picked.Count == 0) {
            picked = new List<string> { "rowid" };
        }

        var orderColumn = PickOrderColumn(columns);
        var selectedColumnsSql = string.Join(", ", picked.Select(QuoteIdentifier));

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT {selectedColumnsSql}
FROM {QuoteIdentifier(tableName)}
WHERE c_personid = $personId
ORDER BY {QuoteIdentifier(orderColumn)}
LIMIT $limit;";
        command.Parameters.AddWithValue("$personId", personId);
        command.Parameters.AddWithValue("$limit", limit);

        var result = new List<PersonRelatedItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            var values = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++) {
                if (reader.IsDBNull(i)) {
                    continue;
                }

                var text = Convert.ToString(reader.GetValue(i));
                if (!string.IsNullOrWhiteSpace(text)) {
                    values.Add(text.Trim());
                }
            }

            if (values.Count == 0) {
                continue;
            }

            var primary = values[0];
            var secondary = values.Count > 1 ? values[1] : null;
            var note = values.Count > 2 ? string.Join(" | ", values.Skip(2)) : null;

            result.Add(new PersonRelatedItem(primary, secondary, note));
        }

        return result;
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string sql, int personId, CancellationToken cancellationToken) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$personId", personId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static (string tableName, IReadOnlyList<string> preferredColumns) GetRelatedTableConfig(PersonRelatedCategory category) {
        return category switch {
            PersonRelatedCategory.Addresses => ("BIOG_ADDR_DATA", new[] { "c_addr_id", "c_firstyear", "c_lastyear", "c_notes" }),
            PersonRelatedCategory.AltNames => ("ALTNAME_DATA", new[] { "c_alt_name", "c_alt_name_chn", "c_alt_name_chnx", "c_notes" }),
            PersonRelatedCategory.Writings => ("BIOG_TEXT_DATA", new[] { "c_textid", "c_role_id", "c_year", "c_notes" }),
            PersonRelatedCategory.Postings => ("POSTED_TO_OFFICE_DATA", new[] { "c_office_id", "c_firstyear", "c_lastyear", "c_notes" }),
            PersonRelatedCategory.Entries => ("ENTRY_DATA", new[] { "c_entry_code", "c_firstyear", "c_lastyear", "c_notes" }),
            PersonRelatedCategory.Events => ("EVENT_DATA", new[] { "c_event_code", "c_firstyear", "c_lastyear", "c_notes" }),
            PersonRelatedCategory.Status => ("STATUS_DATA", new[] { "c_status_code", "c_firstyear", "c_lastyear", "c_notes" }),
            PersonRelatedCategory.Kinship => ("KIN_DATA", new[] { "c_kin_name", "c_kin_code", "c_kin_id", "c_notes" }),
            PersonRelatedCategory.Associations => ("ASSOC_DATA", new[] { "c_assoc_desc", "c_assoc_code", "c_assoc_personid", "c_notes" }),
            _ => ("ALTNAME_DATA", new[] { "c_alt_name", "c_alt_name_chn" })
        };
    }

    private static async Task<List<string>> GetTableColumnsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken) {
        var columns = new List<string>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            if (!reader.IsDBNull(1)) {
                columns.Add(reader.GetString(1));
            }
        }

        return columns;
    }

    private static string PickOrderColumn(IReadOnlyList<string> columns) {
        var priorities = new[] {
            "c_sort",
            "c_sequence",
            "c_index_year",
            "c_year",
            "c_firstyear",
            "c_lastyear",
            "rowid"
        };

        foreach (var name in priorities) {
            if (columns.Contains(name, StringComparer.OrdinalIgnoreCase) || name == "rowid") {
                return name;
            }
        }

        return "rowid";
    }

    private static string QuoteIdentifier(string identifier) {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private static string? NormalizeSqliteText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim().Replace("\"", string.Empty);
    }
}