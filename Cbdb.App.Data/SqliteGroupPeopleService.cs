using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteGroupPeopleService : IGroupPeopleService {
    public async Task<GroupPeopleQueryResult> QueryAsync(
        string sqlitePath,
        IReadOnlyList<int> personIds,
        GroupPeopleQueryOptions options,
        CancellationToken cancellationToken = default
    ) {
        var normalizedIds = NormalizePersonIds(personIds);
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath) || normalizedIds.Count == 0) {
            return new GroupPeopleQueryResult(
                Array.Empty<GroupStatusRecord>(),
                Array.Empty<GroupOfficeRecord>(),
                Array.Empty<GroupEntryRecord>(),
                Array.Empty<GroupTextRecord>(),
                Array.Empty<GroupAddressRecord>()
            );
        }

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var statusRecords = options.IncludeStatus
            ? await LoadStatusRecordsAsync(connection, normalizedIds, cancellationToken)
            : Array.Empty<GroupStatusRecord>();
        var officeRecords = options.IncludeOffice
            ? await LoadOfficeRecordsAsync(connection, normalizedIds, cancellationToken)
            : Array.Empty<GroupOfficeRecord>();
        var entryRecords = options.IncludeEntry
            ? await LoadEntryRecordsAsync(connection, normalizedIds, cancellationToken)
            : Array.Empty<GroupEntryRecord>();
        var textRecords = options.IncludeTexts
            ? await LoadTextRecordsAsync(connection, normalizedIds, cancellationToken)
            : Array.Empty<GroupTextRecord>();
        var addressRecords = options.IncludeAddresses
            ? await LoadAddressRecordsAsync(connection, normalizedIds, options.AddressMode, cancellationToken)
            : Array.Empty<GroupAddressRecord>();

        return new GroupPeopleQueryResult(statusRecords, officeRecords, entryRecords, textRecords, addressRecords);
    }

    private static async Task<IReadOnlyList<GroupStatusRecord>> LoadStatusRecordsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> personIds,
        CancellationToken cancellationToken
    ) {
        var records = new List<GroupStatusRecord>();
        foreach (var chunk in Chunk(personIds, 900)) {
            await using var command = connection.CreateCommand();
            var inClause = AddIdParameters(command, chunk);
            command.CommandText = $@"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    sd.c_sequence,
    COALESCE(sc.c_status_desc_chn, sc.c_status_desc),
    sd.c_firstyear,
    sd.c_lastyear,
    COALESCE(src.c_title_chn, src.c_title),
    sd.c_pages,
    sd.c_notes
FROM STATUS_DATA sd
JOIN BIOG_MAIN b ON b.c_personid = sd.c_personid
LEFT JOIN STATUS_CODES sc ON sc.c_status_code = sd.c_status_code
LEFT JOIN TEXT_CODES src ON src.c_textid = sd.c_source
WHERE sd.c_personid IN ({inClause})
ORDER BY sd.c_personid, sd.c_sequence, sd.c_status_code;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                records.Add(new GroupStatusRecord(
                    PersonId: reader.GetInt32(0),
                    NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                    Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                    Sequence: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Status: reader.IsDBNull(4) ? null : reader.GetString(4),
                    FirstYear: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    LastYear: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    Source: reader.IsDBNull(7) ? null : reader.GetString(7),
                    Pages: reader.IsDBNull(8) ? null : reader.GetString(8),
                    Notes: reader.IsDBNull(9) ? null : reader.GetString(9)
                ));
            }
        }

        return records;
    }

    private static async Task<IReadOnlyList<GroupOfficeRecord>> LoadOfficeRecordsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> personIds,
        CancellationToken cancellationToken
    ) {
        var records = new List<GroupOfficeRecord>();
        foreach (var chunk in Chunk(personIds, 900)) {
            await using var command = connection.CreateCommand();
            var inClause = AddIdParameters(command, chunk);
            command.CommandText = $@"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    pto.c_posting_id,
    pto.c_office_id,
    COALESCE(oc.c_office_chn, oc.c_office_pinyin, oc.c_office_trans),
    COALESCE(appt.c_appt_desc_chn, appt.c_appt_desc),
    COALESCE(assume_office.c_assume_office_desc_chn, assume_office.c_assume_office_desc),
    COALESCE(addr.c_name_chn, addr.c_name),
    pto.c_firstyear,
    pto.c_lastyear,
    COALESCE(src.c_title_chn, src.c_title),
    pto.c_pages,
    pto.c_notes
FROM POSTED_TO_OFFICE_DATA pto
JOIN BIOG_MAIN b ON b.c_personid = pto.c_personid
LEFT JOIN OFFICE_CODES oc ON oc.c_office_id = pto.c_office_id
LEFT JOIN APPOINTMENT_CODES appt ON appt.c_appt_code = pto.c_appt_type_code
LEFT JOIN ASSUME_OFFICE_CODES assume_office ON assume_office.c_assume_office_code = pto.c_assume_office_code
LEFT JOIN POSTED_TO_ADDR_DATA pta
    ON pta.c_personid = pto.c_personid
   AND pta.c_posting_id = pto.c_posting_id
   AND pta.c_office_id = pto.c_office_id
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = pta.c_addr_id
LEFT JOIN TEXT_CODES src ON src.c_textid = pto.c_source
WHERE pto.c_personid IN ({inClause})
ORDER BY pto.c_personid, pto.c_posting_id, pto.c_sequence, pto.c_office_id, pta.c_addr_id;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                records.Add(new GroupOfficeRecord(
                    PersonId: reader.GetInt32(0),
                    NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                    Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                    PostingId: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    OfficeId: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Office: reader.IsDBNull(5) ? null : reader.GetString(5),
                    AppointmentType: reader.IsDBNull(6) ? null : reader.GetString(6),
                    AssumeOffice: reader.IsDBNull(7) ? null : reader.GetString(7),
                    OfficeAddress: reader.IsDBNull(8) ? null : reader.GetString(8),
                    FirstYear: reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    LastYear: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    Source: reader.IsDBNull(11) ? null : reader.GetString(11),
                    Pages: reader.IsDBNull(12) ? null : reader.GetString(12),
                    Notes: reader.IsDBNull(13) ? null : reader.GetString(13)
                ));
            }
        }

        return records;
    }

    private static async Task<IReadOnlyList<GroupEntryRecord>> LoadEntryRecordsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> personIds,
        CancellationToken cancellationToken
    ) {
        var records = new List<GroupEntryRecord>();
        foreach (var chunk in Chunk(personIds, 900)) {
            await using var command = connection.CreateCommand();
            var inClause = AddIdParameters(command, chunk);
            command.CommandText = $@"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    ed.c_sequence,
    COALESCE(ec.c_entry_desc_chn, ec.c_entry_desc),
    ed.c_year,
    COALESCE(addr.c_name_chn, addr.c_name),
    COALESCE(src.c_title_chn, src.c_title),
    ed.c_pages,
    ed.c_notes
FROM ENTRY_DATA ed
JOIN BIOG_MAIN b ON b.c_personid = ed.c_personid
LEFT JOIN ENTRY_CODES ec ON ec.c_entry_code = ed.c_entry_code
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = ed.c_entry_addr_id
LEFT JOIN TEXT_CODES src ON src.c_textid = ed.c_source
WHERE ed.c_personid IN ({inClause})
ORDER BY ed.c_personid, ed.c_year, ed.c_sequence, ed.c_entry_code;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                records.Add(new GroupEntryRecord(
                    PersonId: reader.GetInt32(0),
                    NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                    Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                    Sequence: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    EntryMethod: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Year: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    EntryAddress: reader.IsDBNull(6) ? null : reader.GetString(6),
                    Source: reader.IsDBNull(7) ? null : reader.GetString(7),
                    Pages: reader.IsDBNull(8) ? null : reader.GetString(8),
                    Notes: reader.IsDBNull(9) ? null : reader.GetString(9)
                ));
            }
        }

        return records;
    }

    private static async Task<IReadOnlyList<GroupTextRecord>> LoadTextRecordsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> personIds,
        CancellationToken cancellationToken
    ) {
        var records = new List<GroupTextRecord>();
        foreach (var chunk in Chunk(personIds, 900)) {
            await using var command = connection.CreateCommand();
            var inClause = AddIdParameters(command, chunk);
            command.CommandText = $@"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    btd.c_textid,
    COALESCE(tc.c_title_chn, tc.c_title),
    COALESCE(trc.c_role_desc_chn, trc.c_role_desc),
    btd.c_year,
    COALESCE(src.c_title_chn, src.c_title),
    btd.c_pages,
    btd.c_notes
FROM BIOG_TEXT_DATA btd
JOIN BIOG_MAIN b ON b.c_personid = btd.c_personid
LEFT JOIN TEXT_CODES tc ON tc.c_textid = btd.c_textid
LEFT JOIN TEXT_ROLE_CODES trc ON trc.c_role_id = btd.c_role_id
LEFT JOIN TEXT_CODES src ON src.c_textid = btd.c_source
WHERE btd.c_personid IN ({inClause})
ORDER BY btd.c_personid, btd.c_year, btd.c_textid, btd.c_role_id;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                records.Add(new GroupTextRecord(
                    PersonId: reader.GetInt32(0),
                    NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                    Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                    TextId: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Title: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Role: reader.IsDBNull(5) ? null : reader.GetString(5),
                    Year: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    Source: reader.IsDBNull(7) ? null : reader.GetString(7),
                    Pages: reader.IsDBNull(8) ? null : reader.GetString(8),
                    Notes: reader.IsDBNull(9) ? null : reader.GetString(9)
                ));
            }
        }

        return records;
    }

    private static async Task<IReadOnlyList<GroupAddressRecord>> LoadAddressRecordsAsync(
        SqliteConnection connection,
        IReadOnlyList<int> personIds,
        GroupPeopleAddressMode addressMode,
        CancellationToken cancellationToken
    ) {
        var records = new List<GroupAddressRecord>();
        foreach (var chunk in Chunk(personIds, 900)) {
            await using var command = connection.CreateCommand();
            var inClause = AddIdParameters(command, chunk);
            command.CommandText = addressMode == GroupPeopleAddressMode.IndexAddresses
                ? $@"
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    b.c_index_addr_id,
    COALESCE(addr.c_name_chn, addr.c_name),
    COALESCE(addr_type.c_addr_desc_chn, addr_type.c_addr_desc),
    b.c_index_year,
    NULL,
    NULL,
    NULL,
    1
FROM BIOG_MAIN b
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = b.c_index_addr_id
LEFT JOIN BIOG_ADDR_CODES addr_type ON addr_type.c_addr_type = b.c_index_addr_type_code
WHERE b.c_personid IN ({inClause})
  AND b.c_index_addr_id IS NOT NULL
ORDER BY b.c_personid, b.c_index_addr_id;"
                : $@"
SELECT
    q.c_personid,
    q.c_name_chn,
    q.c_name,
    q.c_addr_id,
    q.c_addr_name,
    q.c_addr_type,
    q.c_firstyear,
    q.c_lastyear,
    q.c_source,
    q.c_notes,
    q.is_index_address
FROM (
    SELECT
        b.c_personid,
        b.c_name_chn,
        b.c_name,
        b.c_index_addr_id AS c_addr_id,
        COALESCE(addr.c_name_chn, addr.c_name) AS c_addr_name,
        COALESCE(addr_type.c_addr_desc_chn, addr_type.c_addr_desc) AS c_addr_type,
        b.c_index_year AS c_firstyear,
        NULL AS c_lastyear,
        NULL AS c_source,
        NULL AS c_notes,
        1 AS is_index_address
    FROM BIOG_MAIN b
    LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = b.c_index_addr_id
    LEFT JOIN BIOG_ADDR_CODES addr_type ON addr_type.c_addr_type = b.c_index_addr_type_code
    WHERE b.c_personid IN ({inClause})
      AND b.c_index_addr_id IS NOT NULL

    UNION ALL

    SELECT
        b.c_personid,
        b.c_name_chn,
        b.c_name,
        bad.c_addr_id,
        COALESCE(addr.c_name_chn, addr.c_name) AS c_addr_name,
        COALESCE(addr_type.c_addr_desc_chn, addr_type.c_addr_desc) AS c_addr_type,
        bad.c_firstyear,
        bad.c_lastyear,
        COALESCE(src.c_title_chn, src.c_title) AS c_source,
        bad.c_notes,
        0 AS is_index_address
    FROM BIOG_ADDR_DATA bad
    JOIN BIOG_MAIN b ON b.c_personid = bad.c_personid
    LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = bad.c_addr_id
    LEFT JOIN BIOG_ADDR_CODES addr_type ON addr_type.c_addr_type = bad.c_addr_type
    LEFT JOIN TEXT_CODES src ON src.c_textid = bad.c_source
    WHERE bad.c_personid IN ({inClause})
      AND bad.c_addr_id IS NOT NULL
) q
ORDER BY q.c_personid, q.is_index_address DESC, q.c_firstyear, q.c_addr_id;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                records.Add(new GroupAddressRecord(
                    PersonId: reader.GetInt32(0),
                    NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                    Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                    AddressId: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Address: reader.IsDBNull(4) ? null : reader.GetString(4),
                    AddressType: reader.IsDBNull(5) ? null : reader.GetString(5),
                    FirstYear: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    LastYear: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    Source: reader.IsDBNull(8) ? null : reader.GetString(8),
                    Notes: reader.IsDBNull(9) ? null : reader.GetString(9),
                    IsIndexAddress: !reader.IsDBNull(10) && reader.GetInt32(10) != 0
                ));
            }
        }

        return records;
    }

    private static string AddIdParameters(SqliteCommand command, IReadOnlyList<int> personIds) {
        var parameterNames = new List<string>(personIds.Count);
        for (var i = 0; i < personIds.Count; i++) {
            var parameterName = $"$id{i}";
            command.Parameters.AddWithValue(parameterName, personIds[i]);
            parameterNames.Add(parameterName);
        }

        return string.Join(", ", parameterNames);
    }

    private static List<int> NormalizePersonIds(IReadOnlyList<int> personIds) {
        var normalized = new List<int>(personIds.Count);
        var seen = new HashSet<int>();
        foreach (var personId in personIds) {
            if (personId > 0 && seen.Add(personId)) {
                normalized.Add(personId);
            }
        }

        return normalized;
    }

    private static IEnumerable<IReadOnlyList<int>> Chunk(IReadOnlyList<int> personIds, int chunkSize) {
        for (var i = 0; i < personIds.Count; i += chunkSize) {
            yield return personIds.Skip(i).Take(chunkSize).ToArray();
        }
    }
}
