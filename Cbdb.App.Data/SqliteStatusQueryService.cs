using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqliteStatusQueryService : IStatusQueryService {
    public async Task<IReadOnlyList<StatusCodeOption>> GetStatusCodesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<StatusCodeOption>();
        }

        var options = new List<StatusCodeOption>();

        await using var connection = await OpenReadOnlyConnectionAsync(sqlitePath, cancellationToken);
        await using var command = connection.CreateCommand();
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
            options.Add(new StatusCodeOption(
                Code: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                DescriptionChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                UsageCount: reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
            ));
        }

        return options;
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
    COALESCE(d.c_dynasty_chn, d.c_dynasty) AS dynasty_label,
    COALESCE(ac.c_name_chn, ac.c_name) AS index_address,
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
    COALESCE(src.c_title_chn, src.c_title) AS source_label,
    sd.c_pages,
    sd.c_notes
FROM STATUS_DATA sd
JOIN matched_people mp ON mp.c_personid = sd.c_personid
JOIN BIOG_MAIN b ON b.c_personid = sd.c_personid
LEFT JOIN STATUS_CODES sc ON sc.c_status_code = sd.c_status_code
LEFT JOIN DYNASTIES d ON d.c_dy = b.c_dy
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = b.c_index_addr_id
LEFT JOIN NIAN_HAO nh1 ON nh1.c_nianhao_id = sd.c_fy_nh_code
LEFT JOIN NIAN_HAO nh2 ON nh2.c_nianhao_id = sd.c_ly_nh_code
LEFT JOIN YEAR_RANGE_CODES yr1 ON yr1.c_range_code = sd.c_fy_range
LEFT JOIN YEAR_RANGE_CODES yr2 ON yr2.c_range_code = sd.c_ly_range
LEFT JOIN TEXT_CODES src ON src.c_textid = sd.c_source
WHERE 1 = 1
""";

        for (var i = 0; i < request.StatusCodes.Count; i++) {
            sql += $"\n  AND sd.c_status_code = $statusCode{i}";
            command.Parameters.AddWithValue($"$statusCode{i}", request.StatusCodes[i]);
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
                Dynasty: reader.IsDBNull(4) ? null : reader.GetString(4),
                IndexAddress: reader.IsDBNull(5) ? null : reader.GetString(5),
                Sequence: reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                Status: reader.IsDBNull(7) ? null : reader.GetString(7),
                StatusCode: reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                FirstYear: reader.IsDBNull(9) ? null : reader.GetInt32(9),
                FirstNianhao: reader.IsDBNull(10) ? null : reader.GetString(10),
                FirstNianhaoYear: reader.IsDBNull(11) ? null : reader.GetInt32(11),
                FirstRange: reader.IsDBNull(12) ? null : reader.GetString(12),
                LastYear: reader.IsDBNull(13) ? null : reader.GetInt32(13),
                LastNianhao: reader.IsDBNull(14) ? null : reader.GetString(14),
                LastNianhaoYear: reader.IsDBNull(15) ? null : reader.GetInt32(15),
                LastRange: reader.IsDBNull(16) ? null : reader.GetString(16),
                Source: reader.IsDBNull(17) ? null : reader.GetString(17),
                Pages: reader.IsDBNull(18) ? null : reader.GetString(18),
                Notes: reader.IsDBNull(19) ? null : reader.GetString(19)
            ));
        }

        var people = records
            .GroupBy(
                record => new {
                    record.PersonId,
                    record.NameChn,
                    record.Name,
                    record.IndexYear,
                    record.Dynasty,
                    record.IndexAddress
                }
            )
            .Select(group => new StatusQueryPerson(
                PersonId: group.Key.PersonId,
                NameChn: group.Key.NameChn,
                Name: group.Key.Name,
                IndexYear: group.Key.IndexYear,
                Dynasty: group.Key.Dynasty,
                IndexAddress: group.Key.IndexAddress,
                StatusCount: group.Count()
            ))
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
