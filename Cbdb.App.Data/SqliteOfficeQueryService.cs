using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteOfficeQueryService : IOfficeQueryService {
    public async Task<OfficePickerData> GetOfficePickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new OfficePickerData(
                new OfficeTypeNode(OfficePickerData.RootCode, null, null, Array.Empty<OfficeTypeNode>(), Array.Empty<string>()),
                Array.Empty<OfficeCodeOption>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var options = new List<OfficeCodeOption>();
        var optionByCode = new Dictionary<string, OfficeCodeOption>(StringComparer.OrdinalIgnoreCase);
        var typeRows = new List<(string Code, string? Description, string? DescriptionChn, string? ParentId)>();
        var relationByTypeCode = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var primaryTypeByOfficeCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await OpenReadOnlyConnectionAsync(sqlitePath, cancellationToken);
        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(oc.c_office_id AS TEXT) AS c_office_id,
    oc.c_office_trans,
    oc.c_office_chn,
    d.c_dynasty,
    d.c_dynasty_chn,
    COUNT(pto.c_personid) AS usage_count
FROM OFFICE_CODES oc
LEFT JOIN DYNASTIES d ON d.c_dy = oc.c_dy
LEFT JOIN POSTED_TO_OFFICE_DATA pto ON pto.c_office_id = oc.c_office_id
GROUP BY oc.c_office_id, oc.c_office_trans, oc.c_office_chn, d.c_dynasty, d.c_dynasty_chn
ORDER BY COALESCE(oc.c_office_chn, oc.c_office_trans, CAST(oc.c_office_id AS TEXT)), oc.c_office_id;
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var option = new OfficeCodeOption(
                    Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                    DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                    Dynasty: reader.IsDBNull(3) ? null : reader.GetString(3),
                    DynastyChn: reader.IsDBNull(4) ? null : reader.GetString(4),
                    UsageCount: reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                );
                options.Add(option);
                optionByCode[option.Code] = option;
            }
        }

        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(c_office_type_node_id AS TEXT) AS c_office_type_node_id,
    c_office_type_desc,
    c_office_type_desc_chn,
    CAST(c_parent_id AS TEXT) AS c_parent_id
FROM OFFICE_TYPE_TREE
ORDER BY LENGTH(CAST(c_office_type_node_id AS TEXT)), CAST(c_office_type_node_id AS TEXT);
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                typeRows.Add((
                    Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                    DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                    ParentId: reader.IsDBNull(3) ? null : reader.GetString(3)
                ));
            }
        }

        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(c_office_tree_id AS TEXT) AS c_office_tree_id,
    CAST(c_office_id AS TEXT) AS c_office_id
FROM OFFICE_CODE_TYPE_REL
ORDER BY CAST(c_office_tree_id AS TEXT), c_office_id;
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var typeCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var officeCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(officeCode)) {
                    continue;
                }

                if (!relationByTypeCode.TryGetValue(typeCode, out var officeCodes)) {
                    officeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    relationByTypeCode[typeCode] = officeCodes;
                }

                officeCodes.Add(officeCode);
                if (!primaryTypeByOfficeCode.ContainsKey(officeCode)) {
                    primaryTypeByOfficeCode[officeCode] = typeCode;
                }
            }
        }

        var optionOrder = options
            .Select((option, index) => new { option.Code, index })
            .ToDictionary(item => item.Code, item => item.index, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string> OrderOfficeCodes(IEnumerable<string> officeCodes) =>
            officeCodes
                .Where(optionByCode.ContainsKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => optionOrder.TryGetValue(code, out var index) ? index : int.MaxValue)
                .ToList();

        OfficeTypeNode BuildNode(string typeCode) {
            var row = typeRows.First(entry => string.Equals(entry.Code, typeCode, StringComparison.OrdinalIgnoreCase));
            var children = typeRows
                .Where(entry => string.Equals(NormalizeParent(entry.ParentId), typeCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
                .Select(entry => BuildNode(entry.Code))
                .ToList();

            var aggregatedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (relationByTypeCode.TryGetValue(typeCode, out var directCodes)) {
                aggregatedCodes.UnionWith(directCodes);
            }

            foreach (var child in children) {
                aggregatedCodes.UnionWith(child.OfficeCodes);
            }

            return new OfficeTypeNode(
                row.Code,
                row.Description,
                row.DescriptionChn,
                children,
                OrderOfficeCodes(aggregatedCodes)
            );
        }

        var explicitRoot = typeRows.FirstOrDefault(entry => entry.Code == "0");
        var rootChildren = typeRows
            .Where(entry => string.Equals(NormalizeParent(entry.ParentId), explicitRoot.Code ?? "0", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
            .Select(entry => BuildNode(entry.Code))
            .ToList();

        if (rootChildren.Count == 0) {
            rootChildren = typeRows
                .Where(entry => string.IsNullOrWhiteSpace(NormalizeParent(entry.ParentId)))
                .OrderBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
                .Select(entry => BuildNode(entry.Code))
                .ToList();
        }

        var root = new OfficeTypeNode(
            OfficePickerData.RootCode,
            explicitRoot.Description,
            explicitRoot.DescriptionChn,
            rootChildren,
            options.Select(option => option.Code).ToList()
        );

        return new OfficePickerData(root, options, primaryTypeByOfficeCode);
    }

    public async Task<OfficeQueryResult> QueryAsync(
        string sqlitePath,
        OfficeQueryRequest request,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new OfficeQueryResult(Array.Empty<OfficeQueryRecord>(), Array.Empty<OfficeQueryPerson>());
        }

        var records = new List<OfficeQueryRecord>();
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
    COALESCE(person_dy.c_dynasty_chn, person_dy.c_dynasty) AS person_dynasty,
    b.c_index_addr_id,
    COALESCE(index_addr.c_name_chn, index_addr.c_name) AS index_address,
    COALESCE(iat.c_addr_desc_chn, iat.c_addr_desc) AS index_address_type,
    pto.c_posting_id,
    COALESCE(pto.c_sequence, 0),
    CAST(pto.c_office_id AS TEXT) AS office_code,
    COALESCE(oc.c_office_chn, oc.c_office_trans, oc.c_office_pinyin) AS office_label,
    COALESCE(appt.c_appt_desc_chn, appt.c_appt_desc) AS appointment_type,
    COALESCE(assume_office.c_assume_office_desc_chn, assume_office.c_assume_office_desc) AS assume_office,
    COALESCE(cat.c_category_desc_chn, cat.c_category_desc) AS category_label,
    pto.c_firstyear,
    COALESCE(fy_nh.c_nianhao_chn, fy_nh.c_nianhao_pin) AS first_nianhao,
    pto.c_fy_nh_year,
    COALESCE(fy_range.c_range_chn, fy_range.c_range) AS first_range,
    pto.c_fy_month,
    pto.c_fy_intercalary,
    pto.c_fy_day,
    COALESCE(fy_gz.c_ganzhi_chn, fy_gz.c_ganzhi_py) AS first_ganzhi,
    pto.c_lastyear,
    COALESCE(ly_nh.c_nianhao_chn, ly_nh.c_nianhao_pin) AS last_nianhao,
    pto.c_ly_nh_year,
    COALESCE(ly_range.c_range_chn, ly_range.c_range) AS last_range,
    pto.c_ly_month,
    pto.c_ly_intercalary,
    pto.c_ly_day,
    COALESCE(ly_gz.c_ganzhi_chn, ly_gz.c_ganzhi_py) AS last_ganzhi,
    COALESCE(sinc.c_inst_name_hz, sinc.c_inst_name_py) AS institution_label,
    pta.c_addr_id,
    COALESCE(office_addr.c_name_chn, office_addr.c_name) AS office_address,
    office_addr.x_coord,
    office_addr.y_coord,
    xy.xy_count,
    COALESCE(src.c_title_chn, src.c_title) AS source_label,
    pto.c_pages,
    pto.c_notes
FROM POSTED_TO_OFFICE_DATA pto
JOIN matched_people mp ON mp.c_personid = pto.c_personid
JOIN BIOG_MAIN b ON b.c_personid = pto.c_personid
LEFT JOIN POSTED_TO_ADDR_DATA pta
    ON pta.c_posting_id = pto.c_posting_id
   AND pta.c_office_id = pto.c_office_id
   AND pta.c_personid = pto.c_personid
LEFT JOIN OFFICE_CODES oc ON oc.c_office_id = pto.c_office_id
LEFT JOIN APPOINTMENT_CODES appt ON appt.c_appt_code = pto.c_appt_type_code
LEFT JOIN ASSUME_OFFICE_CODES assume_office ON assume_office.c_assume_office_code = pto.c_assume_office_code
LEFT JOIN OFFICE_CATEGORIES cat ON cat.c_office_category_id = pto.c_office_category_id
LEFT JOIN INDEXYEAR_TYPE_CODES iy ON iy.c_index_year_type_code = b.c_index_year_type_code
LEFT JOIN ADDR_CODES index_addr ON index_addr.c_addr_id = b.c_index_addr_id
LEFT JOIN BIOG_ADDR_CODES iat ON iat.c_addr_type = b.c_index_addr_type_code
LEFT JOIN DYNASTIES person_dy ON person_dy.c_dy = b.c_dy
LEFT JOIN NIAN_HAO fy_nh ON fy_nh.c_nianhao_id = pto.c_fy_nh_code
LEFT JOIN YEAR_RANGE_CODES fy_range ON fy_range.c_range_code = pto.c_fy_range
LEFT JOIN GANZHI_CODES fy_gz ON fy_gz.c_ganzhi_code = pto.c_fy_day_gz
LEFT JOIN NIAN_HAO ly_nh ON ly_nh.c_nianhao_id = pto.c_ly_nh_code
LEFT JOIN YEAR_RANGE_CODES ly_range ON ly_range.c_range_code = pto.c_ly_range
LEFT JOIN GANZHI_CODES ly_gz ON ly_gz.c_ganzhi_code = pto.c_ly_day_gz
LEFT JOIN SOCIAL_INSTITUTION_NAME_CODES sinc ON sinc.c_inst_name_code = pto.c_inst_name_code
LEFT JOIN ADDR_CODES office_addr ON office_addr.c_addr_id = pta.c_addr_id
LEFT JOIN (
    SELECT
        c_addr_id,
        COUNT(DISTINCT c_personid) AS xy_count
    FROM POSTED_TO_ADDR_DATA
    WHERE c_addr_id IS NOT NULL
    GROUP BY c_addr_id
) xy ON xy.c_addr_id = pta.c_addr_id
LEFT JOIN TEXT_CODES src ON src.c_textid = pto.c_source
WHERE 1 = 1
""";

        if (request.OfficeCodes.Count > 0) {
            sql += "\n  AND pto.c_office_id IN (" + string.Join(", ", request.OfficeCodes.Select((_, index) => $"$officeCode{index}")) + ")";
        }

        for (var i = 0; i < request.OfficeCodes.Count; i++) {
            command.Parameters.AddWithValue($"$officeCode{i}", request.OfficeCodes[i]);
        }

        if (request.PlaceIds.Count > 0) {
            sql += request.IncludeSubordinateUnits
                ? "\n  AND (pta.c_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ") OR EXISTS (SELECT 1 FROM ZZZ_BELONGS_TO bt WHERE bt.c_addr_id = pta.c_addr_id AND bt.c_belongs_to IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")))"
                : "\n  AND pta.c_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")";

            for (var i = 0; i < request.PlaceIds.Count; i++) {
                command.Parameters.AddWithValue($"$placeId{i}", request.PlaceIds[i]);
            }
        }

        sql += """

ORDER BY office_label, pto.c_firstyear, b.c_personid, pto.c_posting_id, pto.c_sequence, pta.c_addr_id
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
            records.Add(new OfficeQueryRecord(
                PersonId: reader.GetInt32(0),
                NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IndexYearType: reader.IsDBNull(4) ? null : reader.GetString(4),
                Sex: reader.IsDBNull(5) ? null : reader.GetString(5),
                Dynasty: reader.IsDBNull(6) ? null : reader.GetString(6),
                IndexAddressId: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                IndexAddress: reader.IsDBNull(8) ? null : reader.GetString(8),
                IndexAddressType: reader.IsDBNull(9) ? null : reader.GetString(9),
                PostingId: reader.GetInt32(10),
                Sequence: reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                OfficeCode: reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                Office: reader.IsDBNull(13) ? null : reader.GetString(13),
                AppointmentType: reader.IsDBNull(14) ? null : reader.GetString(14),
                AssumeOffice: reader.IsDBNull(15) ? null : reader.GetString(15),
                Category: reader.IsDBNull(16) ? null : reader.GetString(16),
                FirstYear: reader.IsDBNull(17) ? null : reader.GetInt32(17),
                FirstNianhao: reader.IsDBNull(18) ? null : reader.GetString(18),
                FirstNianhaoYear: reader.IsDBNull(19) ? null : reader.GetInt32(19),
                FirstRange: reader.IsDBNull(20) ? null : reader.GetString(20),
                FirstMonth: reader.IsDBNull(21) ? null : reader.GetInt32(21),
                FirstIntercalary: reader.IsDBNull(22) ? null : reader.GetInt32(22) == 1,
                FirstDay: reader.IsDBNull(23) ? null : reader.GetInt32(23),
                FirstGanzhi: reader.IsDBNull(24) ? null : reader.GetString(24),
                LastYear: reader.IsDBNull(25) ? null : reader.GetInt32(25),
                LastNianhao: reader.IsDBNull(26) ? null : reader.GetString(26),
                LastNianhaoYear: reader.IsDBNull(27) ? null : reader.GetInt32(27),
                LastRange: reader.IsDBNull(28) ? null : reader.GetString(28),
                LastMonth: reader.IsDBNull(29) ? null : reader.GetInt32(29),
                LastIntercalary: reader.IsDBNull(30) ? null : reader.GetInt32(30) == 1,
                LastDay: reader.IsDBNull(31) ? null : reader.GetInt32(31),
                LastGanzhi: reader.IsDBNull(32) ? null : reader.GetString(32),
                Institution: reader.IsDBNull(33) ? null : reader.GetString(33),
                OfficeAddressId: reader.IsDBNull(34) ? null : reader.GetInt32(34),
                OfficeAddress: reader.IsDBNull(35) ? null : reader.GetString(35),
                OfficeXCoord: reader.IsDBNull(36) ? null : reader.GetDouble(36),
                OfficeYCoord: reader.IsDBNull(37) ? null : reader.GetDouble(37),
                OfficeXyCount: reader.IsDBNull(38) ? 0 : reader.GetInt32(38),
                Source: reader.IsDBNull(39) ? null : reader.GetString(39),
                Pages: reader.IsDBNull(40) ? null : reader.GetString(40),
                Notes: reader.IsDBNull(41) ? null : reader.GetString(41)
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
                record.IndexAddress,
                record.IndexAddressType
            })
            .Select(group => {
                var officeAddressId = group.Select(record => record.OfficeAddressId).FirstOrDefault(id => id.HasValue);
                var xyCount = officeAddressId.HasValue
                    ? records
                        .Where(record => record.OfficeAddressId == officeAddressId)
                        .Select(record => record.PersonId)
                        .Distinct()
                        .Count()
                    : 0;

                return new OfficeQueryPerson(
                    PersonId: group.Key.PersonId,
                    NameChn: group.Key.NameChn,
                    Name: group.Key.Name,
                    IndexYear: group.Key.IndexYear,
                    IndexYearType: group.Key.IndexYearType,
                    Sex: group.Key.Sex,
                    Dynasty: group.Key.Dynasty,
                    IndexAddressId: group.Key.IndexAddressId,
                    IndexAddress: group.Key.IndexAddress,
                    IndexAddressType: group.Key.IndexAddressType,
                    OfficeAddressId: officeAddressId,
                    OfficeAddress: group.Select(record => record.OfficeAddress).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
                    OfficeXCoord: group.Select(record => record.OfficeXCoord).FirstOrDefault(value => value.HasValue),
                    OfficeYCoord: group.Select(record => record.OfficeYCoord).FirstOrDefault(value => value.HasValue),
                    XyCount: xyCount,
                    PostingCount: group.Select(record => (record.PostingId, record.OfficeCode)).Distinct().Count()
                );
            })
            .OrderBy(person => person.PersonId)
            .ToList();

        return new OfficeQueryResult(records, people);
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
