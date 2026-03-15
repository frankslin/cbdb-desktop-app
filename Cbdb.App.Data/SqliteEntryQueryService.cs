using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteEntryQueryService : IEntryQueryService {
    public async Task<EntryPickerData> GetEntryPickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new EntryPickerData(
                new EntryTypeNode(EntryPickerData.RootCode, null, null, Array.Empty<EntryTypeNode>(), Array.Empty<string>()),
                Array.Empty<EntryCodeOption>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var options = new List<EntryCodeOption>();
        var optionByCode = new Dictionary<string, EntryCodeOption>(StringComparer.OrdinalIgnoreCase);
        var typeRows = new List<(string Code, string? Description, string? DescriptionChn, string? ParentId, double? SortOrder)>();
        var relationByTypeCode = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var primaryTypeByEntryCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await OpenReadOnlyConnectionAsync(sqlitePath, cancellationToken);
        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(ec.c_entry_code AS TEXT) AS c_entry_code,
    ec.c_entry_desc,
    ec.c_entry_desc_chn,
    COUNT(ed.c_personid) AS usage_count
FROM ENTRY_CODES ec
LEFT JOIN ENTRY_DATA ed ON ed.c_entry_code = ec.c_entry_code
GROUP BY ec.c_entry_code, ec.c_entry_desc, ec.c_entry_desc_chn
ORDER BY COALESCE(ec.c_entry_desc_chn, ec.c_entry_desc, CAST(ec.c_entry_code AS TEXT)), ec.c_entry_code;
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var option = new EntryCodeOption(
                    Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                    DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                    UsageCount: reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                );

                options.Add(option);
                optionByCode[option.Code] = option;
            }
        }

        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(c_entry_type AS TEXT) AS c_entry_type,
    c_entry_type_desc,
    c_entry_type_desc_chn,
    CAST(c_entry_type_parent_id AS TEXT) AS c_entry_type_parent_id,
    c_entry_type_sortorder
FROM ENTRY_TYPES
ORDER BY
    CASE WHEN c_entry_type_parent_id IS NULL OR CAST(c_entry_type_parent_id AS TEXT) IN ('', '0') THEN 0 ELSE 1 END,
    COALESCE(c_entry_type_sortorder, 999999),
    CAST(c_entry_type AS TEXT);
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                typeRows.Add((
                    Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                    DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                    ParentId: reader.IsDBNull(3) ? null : reader.GetString(3),
                    SortOrder: reader.IsDBNull(4) ? null : reader.GetDouble(4)
                ));
            }
        }

        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(c_entry_type AS TEXT) AS c_entry_type,
    CAST(c_entry_code AS TEXT) AS c_entry_code
FROM ENTRY_CODE_TYPE_REL
ORDER BY CAST(c_entry_type AS TEXT), c_entry_code;
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var typeCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var entryCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(entryCode)) {
                    continue;
                }

                if (!relationByTypeCode.TryGetValue(typeCode, out var entryCodes)) {
                    entryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    relationByTypeCode[typeCode] = entryCodes;
                }

                entryCodes.Add(entryCode);
                if (!primaryTypeByEntryCode.ContainsKey(entryCode)) {
                    primaryTypeByEntryCode[entryCode] = typeCode;
                }
            }
        }

        var optionOrder = options
            .Select((option, index) => new { option.Code, index })
            .ToDictionary(item => item.Code, item => item.index, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string> OrderEntryCodes(IEnumerable<string> entryCodes) =>
            entryCodes
                .Where(optionByCode.ContainsKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => optionOrder.TryGetValue(code, out var index) ? index : int.MaxValue)
                .ToList();

        EntryTypeNode BuildNode(string typeCode) {
            var row = typeRows.First(entry => string.Equals(entry.Code, typeCode, StringComparison.OrdinalIgnoreCase));
            var children = typeRows
                .Where(entry => string.Equals(NormalizeParent(entry.ParentId), typeCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.SortOrder ?? double.MaxValue)
                .ThenBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
                .Select(entry => BuildNode(entry.Code))
                .ToList();

            var aggregatedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (relationByTypeCode.TryGetValue(typeCode, out var directCodes)) {
                aggregatedCodes.UnionWith(directCodes);
            }

            foreach (var child in children) {
                aggregatedCodes.UnionWith(child.EntryCodes);
            }

            return new EntryTypeNode(
                row.Code,
                row.Description,
                row.DescriptionChn,
                children,
                OrderEntryCodes(aggregatedCodes)
            );
        }

        var rootChildren = typeRows
            .Where(entry => string.IsNullOrWhiteSpace(NormalizeParent(entry.ParentId)))
            .OrderBy(entry => entry.SortOrder ?? double.MaxValue)
            .ThenBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
            .Select(entry => BuildNode(entry.Code))
            .ToList();

        var root = new EntryTypeNode(
            EntryPickerData.RootCode,
            null,
            null,
            rootChildren,
            options.Select(option => option.Code).ToList()
        );

        return new EntryPickerData(root, options, primaryTypeByEntryCode);
    }

    public async Task<EntryQueryResult> QueryAsync(
        string sqlitePath,
        EntryQueryRequest request,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new EntryQueryResult(Array.Empty<EntryQueryRecord>(), Array.Empty<EntryQueryPerson>());
        }

        var records = new List<EntryQueryRecord>();
        await using var connection = await OpenReadOnlyConnectionAsync(sqlitePath, cancellationToken);
        await using var command = connection.CreateCommand();

        var sql = """
WITH matched_people AS (
    SELECT DISTINCT b.c_personid
    FROM BIOG_MAIN b
    WHERE (
            $personKeyword IS NULL
            OR b.c_name LIKE $personKeyword
            OR b.c_name_chn LIKE $personKeyword
            OR b.c_name_rm LIKE $personKeyword
            OR b.c_name_proper LIKE $personKeyword
            OR b.c_surname LIKE $personKeyword
            OR b.c_surname_chn LIKE $personKeyword
            OR b.c_mingzi LIKE $personKeyword
            OR b.c_mingzi_chn LIKE $personKeyword
         )
       AND ($useIndexYear = 0 OR b.c_index_year BETWEEN $indexYearFrom AND $indexYearTo)
       AND (
            $useDynasty = 0
            OR EXISTS (
                SELECT 1
                FROM DYNASTIES d
                WHERE d.c_dy = b.c_dy
                  AND ($dynastyFromStart IS NULL OR COALESCE(d.c_end, d.c_start) > $dynastyFromStart)
                  AND ($dynastyToEnd IS NULL OR COALESCE(d.c_start, d.c_end) < $dynastyToEnd)
            )
       )
    UNION
    SELECT DISTINCT b.c_personid
    FROM BIOG_MAIN b
    JOIN ALTNAME_DATA an ON an.c_personid = b.c_personid
    WHERE $personKeyword IS NOT NULL
      AND (an.c_alt_name LIKE $personKeyword OR an.c_alt_name_chn LIKE $personKeyword)
      AND ($useIndexYear = 0 OR b.c_index_year BETWEEN $indexYearFrom AND $indexYearTo)
      AND (
            $useDynasty = 0
            OR EXISTS (
                SELECT 1
                FROM DYNASTIES d
                WHERE d.c_dy = b.c_dy
                  AND ($dynastyFromStart IS NULL OR COALESCE(d.c_end, d.c_start) > $dynastyFromStart)
                  AND ($dynastyToEnd IS NULL OR COALESCE(d.c_start, d.c_end) < $dynastyToEnd)
            )
      )
)
SELECT
    b.c_personid,
    b.c_name_chn,
    b.c_name,
    b.c_index_year,
    COALESCE(iy.c_index_year_type_hz, iy.c_index_year_type_desc) AS index_year_type,
    CASE
        WHEN b.c_female = 1 THEN 'F'
        WHEN b.c_female = 0 THEN 'M'
        ELSE NULL
    END AS sex_label,
    COALESCE(d.c_dynasty_chn, d.c_dynasty) AS dynasty_label,
    b.c_index_addr_id,
    COALESCE(index_addr.c_name_chn, index_addr.c_name) AS index_address,
    ed.c_sequence,
    CAST(ed.c_entry_code AS TEXT) AS entry_code,
    COALESCE(ec.c_entry_desc_chn, ec.c_entry_desc) AS entry_label,
    ed.c_year,
    COALESCE(nh.c_nianhao_chn, nh.c_nianhao_pin) AS nianhao_label,
    ed.c_entry_nh_year,
    COALESCE(yr.c_range_chn, yr.c_range) AS range_label,
    ed.c_exam_rank,
    ed.c_age,
    COALESCE(kc.c_kinrel_chn, kc.c_kinrel) AS kinship_label,
    CASE
        WHEN kin.c_personid IS NULL THEN NULL
        ELSE COALESCE(kin.c_name_chn, kin.c_name)
    END AS kin_person_label,
    COALESCE(ac.c_assoc_desc_chn, ac.c_assoc_desc) AS association_label,
    CASE
        WHEN assoc.c_personid IS NULL THEN NULL
        ELSE COALESCE(assoc.c_name_chn, assoc.c_name)
    END AS associate_person_label,
    COALESCE(sinc.c_inst_name_hz, sinc.c_inst_name_py) AS institution_label,
    ed.c_exam_field,
    ed.c_entry_addr_id,
    COALESCE(entry_addr.c_name_chn, entry_addr.c_name) AS entry_address,
    entry_addr.x_coord,
    entry_addr.y_coord,
    xy.xy_count,
    COALESCE(psc.c_parental_status_desc_chn, psc.c_parental_status_desc) AS parental_status_label,
    ed.c_attempt_count,
    COALESCE(src.c_title_chn, src.c_title) AS source_label,
    ed.c_pages,
    ed.c_notes,
    ed.c_posting_notes
FROM ENTRY_DATA ed
JOIN matched_people mp ON mp.c_personid = ed.c_personid
JOIN BIOG_MAIN b ON b.c_personid = ed.c_personid
LEFT JOIN ENTRY_CODES ec ON ec.c_entry_code = ed.c_entry_code
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
LEFT JOIN ADDR_CODES index_addr ON index_addr.c_addr_id = b.c_index_addr_id
LEFT JOIN INDEXYEAR_TYPE_CODES iy ON iy.c_index_year_type_code = b.c_index_year_type_code
LEFT JOIN NIAN_HAO nh ON nh.c_nianhao_id = ed.c_entry_nh_id
LEFT JOIN YEAR_RANGE_CODES yr ON yr.c_range_code = ed.c_entry_range
LEFT JOIN KINSHIP_CODES kc ON kc.c_kincode = ed.c_kin_code
LEFT JOIN BIOG_MAIN kin ON kin.c_personid = ed.c_kin_id
LEFT JOIN ASSOC_CODES ac ON ac.c_assoc_code = ed.c_assoc_code
LEFT JOIN BIOG_MAIN assoc ON assoc.c_personid = ed.c_assoc_id
LEFT JOIN SOCIAL_INSTITUTION_NAME_CODES sinc ON sinc.c_inst_name_code = ed.c_inst_name_code
LEFT JOIN ADDR_CODES entry_addr ON entry_addr.c_addr_id = ed.c_entry_addr_id
LEFT JOIN (
    SELECT
        c_entry_addr_id,
        COUNT(DISTINCT c_personid) AS xy_count
    FROM ENTRY_DATA
    WHERE c_entry_addr_id IS NOT NULL
    GROUP BY c_entry_addr_id
) xy ON xy.c_entry_addr_id = ed.c_entry_addr_id
LEFT JOIN PARENTAL_STATUS_CODES psc ON psc.c_parental_status_code = ed.c_parental_status_code
LEFT JOIN TEXT_CODES src ON src.c_textid = ed.c_source
WHERE 1 = 1
""";

        if (request.EntryCodes.Count > 0) {
            sql += "\n  AND ed.c_entry_code IN (" + string.Join(", ", request.EntryCodes.Select((_, index) => $"$entryCode{index}")) + ")";
        }

        for (var i = 0; i < request.EntryCodes.Count; i++) {
            command.Parameters.AddWithValue($"$entryCode{i}", request.EntryCodes[i]);
        }

        if (request.PlaceIds.Count > 0) {
            sql += request.IncludeSubordinateUnits
                ? "\n  AND (ed.c_entry_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ") OR EXISTS (SELECT 1 FROM ZZZ_BELONGS_TO bt WHERE bt.c_addr_id = ed.c_entry_addr_id AND bt.c_belongs_to IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")))"
                : "\n  AND ed.c_entry_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")";

            for (var i = 0; i < request.PlaceIds.Count; i++) {
                command.Parameters.AddWithValue($"$placeId{i}", request.PlaceIds[i]);
            }
        }

        sql += """

ORDER BY entry_label, ed.c_year, b.c_personid, ed.c_sequence
LIMIT $limit;
""";

        command.CommandText = sql;
        command.Parameters.AddWithValue(
            "$personKeyword",
            string.IsNullOrWhiteSpace(request.PersonKeyword)
                ? DBNull.Value
                : $"%{request.PersonKeyword.Trim().Replace("\"", string.Empty)}%"
        );
        command.Parameters.AddWithValue("$useIndexYear", request.UseIndexYearRange ? 1 : 0);
        command.Parameters.AddWithValue("$indexYearFrom", Math.Min(request.IndexYearFrom, request.IndexYearTo));
        command.Parameters.AddWithValue("$indexYearTo", Math.Max(request.IndexYearFrom, request.IndexYearTo));
        command.Parameters.AddWithValue("$useDynasty", request.UseDynastyRange ? 1 : 0);
        command.Parameters.AddWithValue("$dynastyFromStart", request.DynastyFrom?.StartYear is int fromStart ? fromStart : DBNull.Value);
        command.Parameters.AddWithValue("$dynastyToEnd", request.DynastyTo?.EndYear is int toEnd ? toEnd : DBNull.Value);
        command.Parameters.AddWithValue("$limit", Math.Clamp(request.Limit, 1, 10000));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            records.Add(new EntryQueryRecord(
                PersonId: reader.GetInt32(0),
                NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IndexYearType: reader.IsDBNull(4) ? null : reader.GetString(4),
                Sex: reader.IsDBNull(5) ? null : reader.GetString(5),
                Dynasty: reader.IsDBNull(6) ? null : reader.GetString(6),
                IndexAddressId: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                IndexAddress: reader.IsDBNull(8) ? null : reader.GetString(8),
                Sequence: reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                EntryCode: reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                EntryMethod: reader.IsDBNull(11) ? null : reader.GetString(11),
                EntryYear: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                Nianhao: reader.IsDBNull(13) ? null : reader.GetString(13),
                NianhaoYear: reader.IsDBNull(14) ? null : reader.GetInt32(14),
                Range: reader.IsDBNull(15) ? null : reader.GetString(15),
                ExamRank: reader.IsDBNull(16) ? null : reader.GetString(16),
                Age: reader.IsDBNull(17) ? null : reader.GetInt32(17),
                Kinship: reader.IsDBNull(18) ? null : reader.GetString(18),
                KinPerson: reader.IsDBNull(19) ? null : reader.GetString(19),
                Association: reader.IsDBNull(20) ? null : reader.GetString(20),
                AssociatePerson: reader.IsDBNull(21) ? null : reader.GetString(21),
                Institution: reader.IsDBNull(22) ? null : reader.GetString(22),
                ExamField: reader.IsDBNull(23) ? null : reader.GetString(23),
                EntryAddressId: reader.IsDBNull(24) ? null : reader.GetInt32(24),
                EntryAddress: reader.IsDBNull(25) ? null : reader.GetString(25),
                EntryXCoord: reader.IsDBNull(26) ? null : reader.GetDouble(26),
                EntryYCoord: reader.IsDBNull(27) ? null : reader.GetDouble(27),
                EntryXyCount: reader.IsDBNull(28) ? 0 : reader.GetInt32(28),
                ParentalStatus: reader.IsDBNull(29) ? null : reader.GetString(29),
                AttemptCount: reader.IsDBNull(30) ? null : reader.GetInt32(30),
                Source: reader.IsDBNull(31) ? null : reader.GetString(31),
                Pages: reader.IsDBNull(32) ? null : reader.GetString(32),
                Notes: reader.IsDBNull(33) ? null : reader.GetString(33),
                PostingNotes: reader.IsDBNull(34) ? null : reader.GetString(34)
            ));
        }

        var people = records
            .GroupBy(record => new {
                record.PersonId,
                record.NameChn,
                record.Name,
                record.IndexYear,
                record.IndexYearType,
                record.Sex,
                record.Dynasty,
                record.IndexAddressId,
                record.IndexAddress
            })
            .Select(group => {
                var entryAddressId = group.Select(record => record.EntryAddressId).FirstOrDefault(id => id.HasValue);
                var xyCount = entryAddressId.HasValue
                    ? records
                        .Where(record => record.EntryAddressId == entryAddressId)
                        .Select(record => record.PersonId)
                        .Distinct()
                        .Count()
                    : 0;

                return new EntryQueryPerson(
                    PersonId: group.Key.PersonId,
                    NameChn: group.Key.NameChn,
                    Name: group.Key.Name,
                    IndexYear: group.Key.IndexYear,
                    IndexYearType: group.Key.IndexYearType,
                    Sex: group.Key.Sex,
                    Dynasty: group.Key.Dynasty,
                    IndexAddressId: group.Key.IndexAddressId,
                    IndexAddress: group.Key.IndexAddress,
                    EntryAddressId: entryAddressId,
                    EntryAddress: group.Select(record => record.EntryAddress).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
                    EntryXCoord: group.Select(record => record.EntryXCoord).FirstOrDefault(value => value.HasValue),
                    EntryYCoord: group.Select(record => record.EntryYCoord).FirstOrDefault(value => value.HasValue),
                    XyCount: xyCount,
                    EntryCount: group.Count()
                );
            })
            .OrderBy(person => person.PersonId)
            .ToList();

        return new EntryQueryResult(records, people);
    }

    private static string? NormalizeParent(string? parentId) {
        var normalized = parentId?.Trim();
        return string.IsNullOrWhiteSpace(normalized) || normalized == "0" ? null : normalized;
    }

    private static async Task<SqliteConnection> OpenReadOnlyConnectionAsync(
        string sqlitePath,
        CancellationToken cancellationToken
    ) {
        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
