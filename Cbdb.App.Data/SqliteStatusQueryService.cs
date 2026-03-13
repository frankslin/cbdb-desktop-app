using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteStatusQueryService : IStatusQueryService {
    public async Task<IReadOnlyList<StatusCodeOption>> GetStatusCodesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        var pickerData = await GetStatusPickerDataAsync(sqlitePath, cancellationToken);
        return pickerData.AllStatusCodes;
    }

    public async Task<StatusPickerData> GetStatusPickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new StatusPickerData(
                new StatusTypeNode(StatusPickerData.RootCode, null, null, Array.Empty<StatusTypeNode>(), Array.Empty<string>()),
                Array.Empty<StatusCodeOption>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var options = new List<StatusCodeOption>();
        var optionByCode = new Dictionary<string, StatusCodeOption>(StringComparer.OrdinalIgnoreCase);
        var typeRows = new List<(string Code, string? Description, string? DescriptionChn)>();
        var relationByTypeCode = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var primaryTypeByStatusCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await OpenReadOnlyConnectionAsync(sqlitePath, cancellationToken);
        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(sc.c_status_code AS TEXT) AS c_status_code,
    sc.c_status_desc,
    sc.c_status_desc_chn,
    COUNT(sd.c_personid) AS usage_count
FROM STATUS_CODES sc
LEFT JOIN STATUS_DATA sd ON sd.c_status_code = sc.c_status_code
GROUP BY sc.c_status_code, sc.c_status_desc, sc.c_status_desc_chn
ORDER BY COALESCE(sc.c_status_desc_chn, sc.c_status_desc, CAST(sc.c_status_code AS TEXT)), sc.c_status_code;
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var option = new StatusCodeOption(
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
    CAST(c_status_type_code AS TEXT) AS c_status_type_code,
    c_status_type_desc,
    c_status_type_chn
FROM STATUS_TYPES
ORDER BY LENGTH(CAST(c_status_type_code AS TEXT)), CAST(c_status_type_code AS TEXT);
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                typeRows.Add((
                    Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                    DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2)
                ));
            }
        }

        await using (var command = connection.CreateCommand()) {
            command.CommandText = """
SELECT
    CAST(c_status_type_code AS TEXT) AS c_status_type_code,
    CAST(c_status_code AS TEXT) AS c_status_code
FROM STATUS_CODE_TYPE_REL
ORDER BY LENGTH(CAST(c_status_type_code AS TEXT)) DESC, CAST(c_status_type_code AS TEXT), CAST(c_status_code AS TEXT);
""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) {
                var typeCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var statusCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(statusCode)) {
                    continue;
                }

                if (!relationByTypeCode.TryGetValue(typeCode, out var statusCodes)) {
                    statusCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    relationByTypeCode[typeCode] = statusCodes;
                }

                statusCodes.Add(statusCode);

                if (!primaryTypeByStatusCode.ContainsKey(statusCode)) {
                    primaryTypeByStatusCode[statusCode] = typeCode;
                }
            }
        }

        IReadOnlyList<string> OrderStatusCodes(IEnumerable<string> statusCodes) =>
            statusCodes
                .Where(optionByCode.ContainsKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => options.FindIndex(option => string.Equals(option.Code, code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        var childNodesByTopCode = typeRows
            .Where(row => row.Code.Length > 2)
            .GroupBy(row => row.Code[..2], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<StatusTypeNode>)group
                    .OrderBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(row => new StatusTypeNode(
                        row.Code,
                        row.Description,
                        row.DescriptionChn,
                        Array.Empty<StatusTypeNode>(),
                        OrderStatusCodes(relationByTypeCode.TryGetValue(row.Code, out var childCodes) ? childCodes : Array.Empty<string>())
                    ))
                    .ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        var topNodes = typeRows
            .Where(row => row.Code.Length == 2)
            .OrderBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
            .Select(row => {
                childNodesByTopCode.TryGetValue(row.Code, out var children);
                children ??= Array.Empty<StatusTypeNode>();

                var aggregatedCodes = relationByTypeCode
                    .Where(entry => entry.Key.StartsWith(row.Code, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(entry => entry.Value);

                return new StatusTypeNode(
                    row.Code,
                    row.Description,
                    row.DescriptionChn,
                    children,
                    OrderStatusCodes(aggregatedCodes)
                );
            })
            .ToList();

        var root = new StatusTypeNode(
            StatusPickerData.RootCode,
            null,
            null,
            topNodes,
            options.Select(option => option.Code).ToList()
        );

        return new StatusPickerData(root, options, primaryTypeByStatusCode);
    }

    public async Task<StatusQueryResult> QueryAsync(
        string sqlitePath,
        StatusQueryRequest request,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return new StatusQueryResult(Array.Empty<StatusQueryRecord>(), Array.Empty<StatusQueryPerson>());
        }

        var records = new List<StatusQueryRecord>();

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
    COALESCE(ac.c_name_chn, ac.c_name) AS index_address,
    ac.x_coord,
    ac.y_coord,
    sd.c_sequence,
    COALESCE(sc.c_status_desc_chn, sc.c_status_desc) AS status_label,
    CAST(sd.c_status_code AS TEXT) AS status_code,
    sd.c_firstyear,
    COALESCE(nh1.c_nianhao_chn, nh1.c_nianhao_pin) AS first_nianhao,
    sd.c_fy_nh_year,
    COALESCE(yr1.c_range_chn, yr1.c_range) AS first_range,
    sd.c_lastyear,
    COALESCE(nh2.c_nianhao_chn, nh2.c_nianhao_pin) AS last_nianhao,
    sd.c_ly_nh_year,
    COALESCE(yr2.c_range_chn, yr2.c_range) AS last_range,
    sd.c_supplement,
    COALESCE(src.c_title_chn, src.c_title) AS source_label,
    sd.c_pages,
    sd.c_notes
FROM STATUS_DATA sd
JOIN matched_people mp ON mp.c_personid = sd.c_personid
JOIN BIOG_MAIN b ON b.c_personid = sd.c_personid
LEFT JOIN STATUS_CODES sc ON sc.c_status_code = sd.c_status_code
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
LEFT JOIN INDEXYEAR_TYPE_CODES iy ON iy.c_index_year_type_code = b.c_index_year_type_code
LEFT JOIN NIAN_HAO nh1 ON nh1.c_nianhao_id = sd.c_fy_nh_code
LEFT JOIN NIAN_HAO nh2 ON nh2.c_nianhao_id = sd.c_ly_nh_code
LEFT JOIN YEAR_RANGE_CODES yr1 ON yr1.c_range_code = sd.c_fy_range
LEFT JOIN YEAR_RANGE_CODES yr2 ON yr2.c_range_code = sd.c_ly_range
LEFT JOIN TEXT_CODES src ON src.c_textid = sd.c_source
WHERE 1 = 1
""";

        if (request.StatusCodes.Count > 0) {
            sql += "\n  AND sd.c_status_code IN (" + string.Join(", ", request.StatusCodes.Select((_, index) => $"$statusCode{index}")) + ")";
        }

        for (var i = 0; i < request.StatusCodes.Count; i++) {
            command.Parameters.AddWithValue($"$statusCode{i}", request.StatusCodes[i]);
        }

        if (request.PlaceIds.Count > 0) {
            sql += request.IncludeSubordinateUnits
                ? "\n  AND (b.c_index_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ") OR EXISTS (SELECT 1 FROM ZZZ_BELONGS_TO bt WHERE bt.c_addr_id = b.c_index_addr_id AND bt.c_belongs_to IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")))"
                : "\n  AND b.c_index_addr_id IN (" + string.Join(", ", request.PlaceIds.Select((_, index) => $"$placeId{index}")) + ")";

            for (var i = 0; i < request.PlaceIds.Count; i++) {
                command.Parameters.AddWithValue($"$placeId{i}", request.PlaceIds[i]);
            }
        }

        sql += """

ORDER BY status_label, b.c_personid, sd.c_sequence
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
        command.Parameters.AddWithValue(
            "$dynastyFromStart",
            request.DynastyFrom?.StartYear is int fromStart ? fromStart : DBNull.Value
        );
        command.Parameters.AddWithValue(
            "$dynastyToEnd",
            request.DynastyTo?.EndYear is int toEnd ? toEnd : DBNull.Value
        );
        command.Parameters.AddWithValue("$limit", Math.Clamp(request.Limit, 1, 10000));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            records.Add(new StatusQueryRecord(
                PersonId: reader.GetInt32(0),
                NameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                Name: reader.IsDBNull(2) ? null : reader.GetString(2),
                IndexYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IndexYearType: reader.IsDBNull(4) ? null : reader.GetString(4),
                Sex: reader.IsDBNull(5) ? null : reader.GetString(5),
                Dynasty: reader.IsDBNull(6) ? null : reader.GetString(6),
                IndexAddressId: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                IndexAddress: reader.IsDBNull(8) ? null : reader.GetString(8),
                XCoord: reader.IsDBNull(9) ? null : reader.GetDouble(9),
                YCoord: reader.IsDBNull(10) ? null : reader.GetDouble(10),
                Sequence: reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                Status: reader.IsDBNull(12) ? null : reader.GetString(12),
                StatusCode: reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                FirstYear: reader.IsDBNull(14) ? null : reader.GetInt32(14),
                FirstNianhao: reader.IsDBNull(15) ? null : reader.GetString(15),
                FirstNianhaoYear: reader.IsDBNull(16) ? null : reader.GetInt32(16),
                FirstRange: reader.IsDBNull(17) ? null : reader.GetString(17),
                LastYear: reader.IsDBNull(18) ? null : reader.GetInt32(18),
                LastNianhao: reader.IsDBNull(19) ? null : reader.GetString(19),
                LastNianhaoYear: reader.IsDBNull(20) ? null : reader.GetInt32(20),
                LastRange: reader.IsDBNull(21) ? null : reader.GetString(21),
                Supplement: reader.IsDBNull(22) ? null : reader.GetString(22),
                Source: reader.IsDBNull(23) ? null : reader.GetString(23),
                Pages: reader.IsDBNull(24) ? null : reader.GetString(24),
                Notes: reader.IsDBNull(25) ? null : reader.GetString(25)
            ));
        }

        var people = records
            .GroupBy(
                record => new {
                    record.PersonId,
                    record.NameChn,
                    record.Name,
                    record.IndexYear,
                    record.IndexYearType,
                    record.Sex,
                    record.Dynasty,
                    record.IndexAddressId,
                    record.IndexAddress
                }
            )
            .Select(group => {
                var xyCount = group.Key.IndexAddressId.HasValue
                    ? records
                        .Where(record => record.IndexAddressId == group.Key.IndexAddressId)
                        .Select(record => record.PersonId)
                        .Distinct()
                        .Count()
                    : 0;

                return new StatusQueryPerson(
                PersonId: group.Key.PersonId,
                NameChn: group.Key.NameChn,
                Name: group.Key.Name,
                IndexYear: group.Key.IndexYear,
                IndexYearType: group.Key.IndexYearType,
                Sex: group.Key.Sex,
                Dynasty: group.Key.Dynasty,
                IndexAddressId: group.Key.IndexAddressId,
                IndexAddress: group.Key.IndexAddress,
                XCoord: group.Select(record => record.XCoord).FirstOrDefault(value => value.HasValue),
                YCoord: group.Select(record => record.YCoord).FirstOrDefault(value => value.HasValue),
                XyCount: xyCount,
                StatusCount: group.Count()
                );
            })
            .OrderBy(person => person.PersonId)
            .ToList();

        return new StatusQueryResult(records, people);
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
