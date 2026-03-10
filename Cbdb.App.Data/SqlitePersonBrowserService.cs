using System.Collections.Concurrent;
using System.Data;
using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqlitePersonBrowserService : IPersonBrowserService {
    private static readonly HashSet<string> HiddenBiogMainFields = new(StringComparer.OrdinalIgnoreCase) {
        "c_personid",
        "c_surname_chn",
        "c_mingzi_chn",
        "c_name_chn",
        "c_surname",
        "c_mingzi",
        "c_name",
        "c_surname_proper",
        "c_mingzi_proper",
        "c_name_proper",
        "c_surname_rm",
        "c_mingzi_rm",
        "c_name_rm",
        "c_female",
        "c_birthyear",
        "c_deathyear",
        "c_index_year",
        "c_index_year_type_code",
        "c_index_year_source_id",
        "c_dy",
        "c_index_addr_id",
        "c_index_addr_type_code",
        "c_notes"
    };

    private static readonly string[] PreferredDisplayColumns = [
        "c_name_chn", "c_name", "c_alt_name_chn", "c_alt_name", "c_title_chn", "c_title",
        "c_desc_chn", "c_desc", "c_dynasty_chn", "c_dynasty", "c_inst_name_chn", "c_inst_name",
        "c_addr_desc_chn", "c_addr_desc", "c_addr_chn", "c_addr", "c_event_chn", "c_event",
        "c_role_desc_chn", "c_role_desc", "c_text_title_chn", "c_text_title", "name_chn", "name", "title_chn", "title", "label_chn", "label"
    ];

    private static readonly ConcurrentDictionary<string, string?> LookupCache = new();

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

        await using var command = connection.CreateCommand();
        command.CommandText = hasKeyword
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
        OR b.c_surname_rm LIKE $kw
        OR b.c_mingzi LIKE $kw
        OR b.c_mingzi_chn LIKE $kw
        OR b.c_mingzi_rm LIKE $kw
    UNION
    SELECT a.c_personid
    FROM ALTNAME_DATA a
    WHERE a.c_alt_name LIKE $kw OR a.c_alt_name_chn LIKE $kw
)
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    b.c_index_year,
    COALESCE(ac.c_name_chn, ac.c_name) AS c_index_address
FROM matched_ids m
JOIN BIOG_MAIN b ON b.c_personid = m.c_personid
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
ORDER BY b.c_personid
LIMIT $limit OFFSET $offset;"
            : @"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    b.c_index_year,
    COALESCE(ac.c_name_chn, ac.c_name) AS c_index_address
FROM BIOG_MAIN b
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
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
                NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                NameRm: reader.IsDBNull(2) ? null : reader.GetString(2),
                IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IndexAddress: reader.IsDBNull(4) ? null : reader.GetString(4)
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

        int detailPersonId;
        string? surnameChn;
        string? mingziChn;
        string? surname;
        string? mingzi;
        string? surnameProper;
        string? mingziProper;
        string? surnameRm;
        string? mingziRm;
        string? name;
        string? nameChn;
        int? indexYear;
        string? indexYearType;
        string? indexYearSource;
        string? dynasty;
        string? dynastyChn;
        int? birthYear;
        int? deathYear;
        string? gender;
        string? indexAddress;
        string? indexAddressChn;
        string? indexAddressType;

        await using (var command = connection.CreateCommand()) {
            command.CommandText = @"
SELECT
    b.c_personid,
    b.c_surname_chn,
    b.c_mingzi_chn,
    b.c_surname,
    b.c_mingzi,
    b.c_surname_proper,
    b.c_mingzi_proper,
    b.c_surname_rm,
    b.c_mingzi_rm,
    b.c_name,
    b.c_name_chn,
    b.c_index_year,
    b.c_index_year_type_code,
    iy.c_index_year_type_hz,
    iy.c_index_year_type_desc,
    b.c_index_year_source_id,
    COALESCE(src.c_name_chn, src.c_name),
    d.c_dynasty,
    d.c_dynasty_chn,
    b.c_birthyear,
    b.c_deathyear,
    b.c_female,
    ac.c_name,
    ac.c_name_chn,
    b.c_index_addr_type_code,
    COALESCE(bac.c_addr_desc_chn, bac.c_addr_desc)
FROM BIOG_MAIN b
LEFT JOIN INDEXYEAR_TYPE_CODES iy ON iy.c_index_year_type_code = b.c_index_year_type_code
LEFT JOIN BIOG_MAIN src ON src.c_personid = b.c_index_year_source_id
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
LEFT JOIN BIOG_ADDR_CODES bac ON bac.c_addr_type = b.c_index_addr_type_code
WHERE b.c_personid = $personId
LIMIT 1;";
            command.Parameters.AddWithValue("$personId", personId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) {
                return null;
            }

            detailPersonId = reader.GetInt32(0);
            surnameChn = reader.IsDBNull(1) ? null : reader.GetString(1);
            mingziChn = reader.IsDBNull(2) ? null : reader.GetString(2);
            surname = reader.IsDBNull(3) ? null : reader.GetString(3);
            mingzi = reader.IsDBNull(4) ? null : reader.GetString(4);
            surnameProper = reader.IsDBNull(5) ? null : reader.GetString(5);
            mingziProper = reader.IsDBNull(6) ? null : reader.GetString(6);
            surnameRm = reader.IsDBNull(7) ? null : reader.GetString(7);
            mingziRm = reader.IsDBNull(8) ? null : reader.GetString(8);
            name = reader.IsDBNull(9) ? null : reader.GetString(9);
            nameChn = reader.IsDBNull(10) ? null : reader.GetString(10);
            indexYear = reader.IsDBNull(11) ? null : reader.GetInt32(11);
            indexYearType = JoinDisplay(
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                " ({0})"
            );
            indexYearSource = reader.IsDBNull(16) ? (reader.IsDBNull(15) ? null : Convert.ToString(reader.GetValue(15))) : reader.GetString(16);
            dynasty = reader.IsDBNull(17) ? null : reader.GetString(17);
            dynastyChn = reader.IsDBNull(18) ? null : reader.GetString(18);
            birthYear = reader.IsDBNull(19) ? null : reader.GetInt32(19);
            deathYear = reader.IsDBNull(20) ? null : reader.GetInt32(20);
            gender = reader.IsDBNull(21)
                ? "Unknown"
                : (reader.GetInt32(21) == 1 ? "F" : "M");
            indexAddress = reader.IsDBNull(22) ? null : reader.GetString(22);
            indexAddressChn = reader.IsDBNull(23) ? null : reader.GetString(23);
            indexAddressType = reader.IsDBNull(25) ? (reader.IsDBNull(24) ? null : Convert.ToString(reader.GetValue(24))) : reader.GetString(25);
        }

        var fields = await LoadBiogMainFieldsAsync(connection, personId, cancellationToken);

        return new PersonDetail(
            PersonId: detailPersonId,
            SurnameChn: surnameChn,
            MingziChn: mingziChn,
            Surname: surname,
            Mingzi: mingzi,
            SurnameProper: surnameProper,
            MingziProper: mingziProper,
            SurnameRm: surnameRm,
            MingziRm: mingziRm,
            Name: name,
            NameChn: nameChn,
            IndexYear: indexYear,
            IndexYearType: indexYearType,
            IndexYearSource: indexYearSource,
            Dynasty: dynasty,
            DynastyChn: dynastyChn,
            BirthYear: birthYear,
            DeathYear: deathYear,
            Gender: gender,
            IndexAddress: indexAddress,
            IndexAddressChn: indexAddressChn,
            IndexAddressType: indexAddressType,
            AddressCount: await CountAsync(connection, "SELECT COUNT(*) FROM BIOG_ADDR_DATA WHERE c_personid = $personId", personId, cancellationToken),
            AltNameCount: await CountAsync(connection, "SELECT COUNT(*) FROM ALTNAME_DATA WHERE c_personid = $personId", personId, cancellationToken),
            KinCount: await CountAsync(connection, "SELECT COUNT(*) FROM KIN_DATA WHERE c_personid = $personId", personId, cancellationToken),
            AssocCount: await CountAsync(connection, "SELECT COUNT(*) FROM ASSOC_DATA WHERE c_personid = $personId", personId, cancellationToken),
            OfficeCount: await CountAsync(connection, "SELECT COUNT(*) FROM POSTED_TO_OFFICE_DATA WHERE c_personid = $personId", personId, cancellationToken),
            EntryCount: await CountAsync(connection, "SELECT COUNT(*) FROM ENTRY_DATA WHERE c_personid = $personId", personId, cancellationToken),
            EventCount: await CountAsync(connection, "SELECT COUNT(*) FROM EVENTS_DATA WHERE c_personid = $personId", personId, cancellationToken),
            StatusCount: await CountAsync(connection, "SELECT COUNT(*) FROM STATUS_DATA WHERE c_personid = $personId", personId, cancellationToken),
            TextCount: await CountAsync(connection, "SELECT COUNT(*) FROM BIOG_TEXT_DATA WHERE c_personid = $personId", personId, cancellationToken),
            PossessionCount: await CountAsync(connection, "SELECT COUNT(*) FROM POSSESSION_DATA WHERE c_personid = $personId", personId, cancellationToken),
            SourceCount: await CountAsync(connection, "SELECT COUNT(*) FROM BIOG_SOURCE_DATA WHERE c_personid = $personId", personId, cancellationToken),
            InstitutionCount: await CountAsync(connection, "SELECT COUNT(*) FROM BIOG_INST_DATA WHERE c_personid = $personId", personId, cancellationToken),
            Fields: fields
        );
    }

    public async Task<DataTable> GetRelatedItemsAsync(
        string sqlitePath,
        int personId,
        PersonRelatedCategory category,
        int limit = 200,
        CancellationToken cancellationToken = default
    ) {
        var table = new DataTable();

        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return table;
        }

        limit = Math.Clamp(limit, 1, 1000);

        var tableName = GetRelatedTableName(category);
        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var columns = await GetTableColumnsAsync(connection, tableName, cancellationToken);
        if (columns.Count == 0 || !columns.Contains("c_personid", StringComparer.OrdinalIgnoreCase)) {
            return table;
        }

        var orderColumn = PickOrderColumn(columns);
        var selectedColumnsSql = string.Join(", ", columns.Select(QuoteIdentifier));

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT {selectedColumnsSql}
FROM {QuoteIdentifier(tableName)}
WHERE c_personid = $personId
ORDER BY {QuoteIdentifier(orderColumn)}
LIMIT $limit;";
        command.Parameters.AddWithValue("$personId", personId);
        command.Parameters.AddWithValue("$limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        table.Load(reader);
        return await EnrichTableAsync(connection, tableName, table, cancellationToken);
    }

    private static async Task<IReadOnlyList<PersonFieldValue>> LoadBiogMainFieldsAsync(
        SqliteConnection connection,
        int personId,
        CancellationToken cancellationToken
    ) {
        var fields = new List<PersonFieldValue>();
        var foreignKeys = await GetForeignKeysAsync(connection, "BIOG_MAIN", cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BIOG_MAIN WHERE c_personid = $personId LIMIT 1;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken)) {
            for (var i = 0; i < reader.FieldCount; i++) {
                var fieldName = reader.GetName(i);
                if (HiddenBiogMainFields.Contains(fieldName)) {
                    continue;
                }

                var value = reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i));
                var displayValue = await FormatForeignKeyValueAsync(connection, foreignKeys, fieldName, value, cancellationToken);
                fields.Add(new PersonFieldValue(fieldName, displayValue));
            }
        }

        return fields;
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string sql, int personId, CancellationToken cancellationToken) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$personId", personId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static string GetRelatedTableName(PersonRelatedCategory category) {
        return category switch {
            PersonRelatedCategory.Addresses => "BIOG_ADDR_DATA",
            PersonRelatedCategory.AltNames => "ALTNAME_DATA",
            PersonRelatedCategory.Writings => "BIOG_TEXT_DATA",
            PersonRelatedCategory.Postings => "POSTED_TO_OFFICE_DATA",
            PersonRelatedCategory.Entries => "ENTRY_DATA",
            PersonRelatedCategory.Events => "EVENTS_DATA",
            PersonRelatedCategory.Status => "STATUS_DATA",
            PersonRelatedCategory.Kinship => "KIN_DATA",
            PersonRelatedCategory.Associations => "ASSOC_DATA",
            PersonRelatedCategory.Possessions => "POSSESSION_DATA",
            PersonRelatedCategory.Sources => "BIOG_SOURCE_DATA",
            PersonRelatedCategory.Institutions => "BIOG_INST_DATA",
            _ => "ALTNAME_DATA"
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

    private static async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken
    ) {
        var foreignKeys = new List<ForeignKeyInfo>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list({QuoteIdentifier(tableName)});";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            var fromColumn = reader.IsDBNull(3) ? null : reader.GetString(3);
            var referenceTable = reader.IsDBNull(2) ? null : reader.GetString(2);
            var referenceColumn = reader.IsDBNull(4) ? null : reader.GetString(4);

            if (string.IsNullOrWhiteSpace(fromColumn)
                || string.IsNullOrWhiteSpace(referenceTable)
                || string.IsNullOrWhiteSpace(referenceColumn)) {
                continue;
            }

            foreignKeys.Add(new ForeignKeyInfo(fromColumn, referenceTable, referenceColumn));
        }

        return foreignKeys;
    }

    private static async Task<DataTable> EnrichTableAsync(
        SqliteConnection connection,
        string tableName,
        DataTable source,
        CancellationToken cancellationToken
    ) {
        var foreignKeys = await GetForeignKeysAsync(connection, tableName, cancellationToken);
        if (foreignKeys.Count == 0) {
            return source;
        }

        var displayTable = new DataTable();
        foreach (DataColumn column in source.Columns) {
            displayTable.Columns.Add(column.ColumnName, typeof(string));
        }

        foreach (DataRow row in source.Rows) {
            var newRow = displayTable.NewRow();
            foreach (DataColumn column in source.Columns) {
                var rawValue = row.IsNull(column) ? string.Empty : Convert.ToString(row[column]) ?? string.Empty;
                newRow[column.ColumnName] = await FormatForeignKeyValueAsync(connection, foreignKeys, column.ColumnName, rawValue, cancellationToken);
            }

            displayTable.Rows.Add(newRow);
        }

        return displayTable;
    }

    private static async Task<string> FormatForeignKeyValueAsync(
        SqliteConnection connection,
        IReadOnlyList<ForeignKeyInfo> foreignKeys,
        string columnName,
        string? rawValue,
        CancellationToken cancellationToken
    ) {
        if (string.IsNullOrWhiteSpace(rawValue)) {
            return string.Empty;
        }

        var foreignKey = foreignKeys.FirstOrDefault(fk => string.Equals(fk.FromColumn, columnName, StringComparison.OrdinalIgnoreCase));
        if (foreignKey is null) {
            return rawValue;
        }

        var resolved = await ResolveForeignKeyTextAsync(connection, foreignKey, rawValue, cancellationToken);
        return string.IsNullOrWhiteSpace(resolved) ? rawValue : $"{rawValue} | {resolved}";
    }

    private static async Task<string?> ResolveForeignKeyTextAsync(
        SqliteConnection connection,
        ForeignKeyInfo foreignKey,
        string rawValue,
        CancellationToken cancellationToken
    ) {
        var cacheKey = $"{foreignKey.ReferenceTable}|{foreignKey.ReferenceColumn}|{rawValue}";
        if (LookupCache.TryGetValue(cacheKey, out var cached)) {
            return cached;
        }

        var columns = await GetTableColumnsAsync(connection, foreignKey.ReferenceTable, cancellationToken);
        if (columns.Count == 0) {
            LookupCache[cacheKey] = null;
            return null;
        }

        var displayColumns = PickDisplayColumns(columns);
        if (displayColumns.Count == 0) {
            LookupCache[cacheKey] = null;
            return null;
        }

        var selectSql = string.Join(", ", displayColumns.Select(QuoteIdentifier));

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT {selectSql}
FROM {QuoteIdentifier(foreignKey.ReferenceTable)}
WHERE {QuoteIdentifier(foreignKey.ReferenceColumn)} = $value
LIMIT 1;";
        command.Parameters.AddWithValue("$value", rawValue);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            LookupCache[cacheKey] = null;
            return null;
        }

        var parts = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++) {
            if (reader.IsDBNull(i)) {
                continue;
            }

            var text = Convert.ToString(reader.GetValue(i));
            if (!string.IsNullOrWhiteSpace(text) && !parts.Contains(text, StringComparer.OrdinalIgnoreCase)) {
                parts.Add(text);
            }
        }

        var result = parts.Count == 0 ? null : string.Join(" / ", parts);
        LookupCache[cacheKey] = result;
        return result;
    }

    private static List<string> PickDisplayColumns(IReadOnlyList<string> columns) {
        var selected = new List<string>();

        foreach (var preferred in PreferredDisplayColumns) {
            var match = columns.FirstOrDefault(column => string.Equals(column, preferred, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !selected.Contains(match, StringComparer.OrdinalIgnoreCase)) {
                selected.Add(match);
            }

            if (selected.Count >= 2) {
                return selected;
            }
        }

        foreach (var column in columns) {
            if (column.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                || column.EndsWith("_code", StringComparison.OrdinalIgnoreCase)
                || column.Contains("created_", StringComparison.OrdinalIgnoreCase)
                || column.Contains("modified_", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (!selected.Contains(column, StringComparer.OrdinalIgnoreCase)) {
                selected.Add(column);
            }

            if (selected.Count >= 2) {
                break;
            }
        }

        return selected;
    }

    private static string PickOrderColumn(IReadOnlyList<string> columns) {
        var priorities = new[] {
            "c_sequence",
            "c_sort",
            "c_index_year",
            "c_year",
            "c_firstyear",
            "c_lastyear",
            "rowid"
        };

        foreach (var name in priorities) {
            if (name == "rowid" || columns.Contains(name, StringComparer.OrdinalIgnoreCase)) {
                return name;
            }
        }

        return "rowid";
    }

    private static string QuoteIdentifier(string identifier) {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private static string? JoinDisplay(string? primary, string? secondary, string secondaryPattern = " / {0}") {
        if (string.IsNullOrWhiteSpace(primary)) {
            return secondary;
        }
        if (string.IsNullOrWhiteSpace(secondary) || string.Equals(primary, secondary, StringComparison.OrdinalIgnoreCase)) {
            return primary;
        }
        return primary + string.Format(secondaryPattern, secondary);
    }

    private static string? NormalizeSqliteText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim().Replace("\"", string.Empty);
    }

    private sealed record ForeignKeyInfo(
        string FromColumn,
        string ReferenceTable,
        string ReferenceColumn
    );
}

