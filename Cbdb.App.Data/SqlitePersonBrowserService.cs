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
        "c_index_year",
        "c_index_year_type_code",
        "c_index_year_source_id",
        "c_dy",
        "c_index_addr_id",
        "c_index_addr_type_code",
        "c_notes",
        "c_self_bio"
    };

    private static readonly string[] PreferredDisplayColumns = [
        "c_name_chn", "c_name", "c_alt_name_chn", "c_alt_name", "c_title_chn", "c_title",
        "c_desc_chn", "c_desc", "c_dynasty_chn", "c_dynasty", "c_inst_name_chn", "c_inst_name",
        "c_addr_desc_chn", "c_addr_desc", "c_addr_chn", "c_addr", "c_event_chn", "c_event",
        "c_role_desc_chn", "c_role_desc", "c_text_title_chn", "c_text_title", "c_nianhao_chn", "c_nianhao_pin",
        "c_ganzhi_chn", "c_ganzhi_py", "c_choronym_chn", "c_choronym_desc",
        "c_household_status_desc_chn", "c_household_status_desc", "c_range_chn", "c_range", "c_approx_chn", "c_approx",
        "name_chn", "name", "title_chn", "title", "label_chn", "label"
    ];

    private static readonly ConcurrentDictionary<string, string?> LookupCache = new();
    private static readonly Dictionary<string, ForeignKeyInfo> ManualLookupColumns = new(StringComparer.OrdinalIgnoreCase) {
        ["c_by_nh_code"] = new("c_by_nh_code", "NIAN_HAO", "c_nianhao_id"),
        ["c_dy_nh_code"] = new("c_dy_nh_code", "NIAN_HAO", "c_nianhao_id"),
        ["c_fl_ey_nh_code"] = new("c_fl_ey_nh_code", "NIAN_HAO", "c_nianhao_id"),
        ["c_fl_ly_nh_code"] = new("c_fl_ly_nh_code", "NIAN_HAO", "c_nianhao_id"),
        ["c_by_day_gz"] = new("c_by_day_gz", "GANZHI_CODES", "c_ganzhi_code"),
        ["c_dy_day_gz"] = new("c_dy_day_gz", "GANZHI_CODES", "c_ganzhi_code"),
        ["c_by_range"] = new("c_by_range", "YEAR_RANGE_CODES", "c_range_code"),
        ["c_dy_range"] = new("c_dy_range", "YEAR_RANGE_CODES", "c_range_code"),
        ["c_death_age_range"] = new("c_death_age_range", "YEAR_RANGE_CODES", "c_range_code"),
        ["c_choronym_code"] = new("c_choronym_code", "CHORONYM_CODES", "c_choronym_code"),
        ["c_household_status_code"] = new("c_household_status_code", "HOUSEHOLD_STATUS_CODES", "c_household_status_code"),
        ["c_ethnicity_code"] = new("c_ethnicity_code", "ETHNICITY_TRIBE_CODES", "c_ethnicity_code")
    };

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
    CASE
        WHEN b.c_index_year_source_id IS NULL THEN NULL
        ELSE CAST(b.c_index_year_source_id AS TEXT)
             || CASE
                    WHEN src.c_name_chn IS NOT NULL AND TRIM(src.c_name_chn) <> '' THEN ' / ' || TRIM(src.c_name_chn)
                    ELSE ''
                END
             || CASE
                    WHEN src.c_name IS NOT NULL AND TRIM(src.c_name) <> '' THEN ' / ' || TRIM(src.c_name)
                    ELSE ''
                END
    END,
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
                " / {0}"
            );
            indexYearSource = reader.IsDBNull(15) ? null : reader.GetString(15);
            dynasty = reader.IsDBNull(16) ? null : reader.GetString(16);
            dynastyChn = reader.IsDBNull(17) ? null : reader.GetString(17);
            birthYear = reader.IsDBNull(18) ? null : reader.GetInt32(18);
            deathYear = reader.IsDBNull(19) ? null : reader.GetInt32(19);
            gender = reader.IsDBNull(20)
                ? "Unknown"
                : (reader.GetInt32(20) == 1 ? "F" : "M");
            indexAddress = reader.IsDBNull(21) ? null : reader.GetString(21);
            indexAddressChn = reader.IsDBNull(22) ? null : reader.GetString(22);
            indexAddressType = reader.IsDBNull(24) ? (reader.IsDBNull(23) ? null : Convert.ToString(reader.GetValue(23))) : reader.GetString(24);
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

    public async Task<IReadOnlyList<PersonAddressItem>> GetAddressesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonAddressItem>();
        }

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        var items = new List<PersonAddressItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    bad.c_sequence,
    bad.c_natal,
    bac.c_addr_desc,
    bac.c_addr_desc_chn,
    ac.c_name,
    ac.c_name_chn,
    bad.c_firstyear,
    fy_nh.c_nianhao_pin,
    fy_nh.c_nianhao_chn,
    bad.c_fy_nh_year,
    bad.c_fy_month,
    bad.c_fy_intercalary,
    bad.c_fy_day,
    fy_gz.c_ganzhi_py,
    fy_gz.c_ganzhi_chn,
    fy_range.c_range,
    fy_range.c_range_chn,
    bad.c_lastyear,
    ly_nh.c_nianhao_pin,
    ly_nh.c_nianhao_chn,
    bad.c_ly_nh_year,
    bad.c_ly_month,
    bad.c_ly_intercalary,
    bad.c_ly_day,
    ly_gz.c_ganzhi_py,
    ly_gz.c_ganzhi_chn,
    ly_range.c_range,
    ly_range.c_range_chn,
    src.c_title,
    src.c_title_chn,
    bad.c_pages,
    bad.c_notes
FROM BIOG_ADDR_DATA bad
LEFT JOIN BIOG_ADDR_CODES bac ON bac.c_addr_type = bad.c_addr_type
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = bad.c_addr_id
LEFT JOIN NIAN_HAO fy_nh ON fy_nh.c_nianhao_id = bad.c_fy_nh_code
LEFT JOIN NIAN_HAO ly_nh ON ly_nh.c_nianhao_id = bad.c_ly_nh_code
LEFT JOIN GANZHI_CODES fy_gz ON fy_gz.c_ganzhi_code = bad.c_fy_day_gz
LEFT JOIN GANZHI_CODES ly_gz ON ly_gz.c_ganzhi_code = bad.c_ly_day_gz
LEFT JOIN YEAR_RANGE_CODES fy_range ON fy_range.c_range_code = bad.c_fy_range
LEFT JOIN YEAR_RANGE_CODES ly_range ON ly_range.c_range_code = bad.c_ly_range
LEFT JOIN TEXT_CODES src ON src.c_textid = bad.c_source
WHERE bad.c_personid = $personId
ORDER BY bad.c_sequence, bad.c_addr_type, bad.c_addr_id;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonAddressItem(
                Sequence: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Natal: reader.IsDBNull(1) ? null : reader.GetInt32(1) == 1,
                AddressType: JoinDisplay(
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                ),
                AddressNameChn: reader.IsDBNull(5) ? null : reader.GetString(5),
                AddressName: reader.IsDBNull(4) ? null : reader.GetString(4),
                FirstYear: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                FirstNianhao: JoinDisplay(
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(7) ? null : reader.GetString(7)
                ),
                FirstNianhaoYear: reader.IsDBNull(9) ? null : reader.GetInt32(9),
                FirstMonth: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                FirstIntercalary: reader.IsDBNull(11) ? null : reader.GetInt32(11) == 1,
                FirstDay: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                FirstGanzhi: JoinDisplay(
                    reader.IsDBNull(14) ? null : reader.GetString(14),
                    reader.IsDBNull(13) ? null : reader.GetString(13)
                ),
                FirstRange: JoinDisplay(
                    reader.IsDBNull(16) ? null : reader.GetString(16),
                    reader.IsDBNull(15) ? null : reader.GetString(15)
                ),
                LastYear: reader.IsDBNull(17) ? null : reader.GetInt32(17),
                LastNianhao: JoinDisplay(
                    reader.IsDBNull(19) ? null : reader.GetString(19),
                    reader.IsDBNull(18) ? null : reader.GetString(18)
                ),
                LastNianhaoYear: reader.IsDBNull(20) ? null : reader.GetInt32(20),
                LastMonth: reader.IsDBNull(21) ? null : reader.GetInt32(21),
                LastIntercalary: reader.IsDBNull(22) ? null : reader.GetInt32(22) == 1,
                LastDay: reader.IsDBNull(23) ? null : reader.GetInt32(23),
                LastGanzhi: JoinDisplay(
                    reader.IsDBNull(25) ? null : reader.GetString(25),
                    reader.IsDBNull(24) ? null : reader.GetString(24)
                ),
                LastRange: JoinDisplay(
                    reader.IsDBNull(27) ? null : reader.GetString(27),
                    reader.IsDBNull(26) ? null : reader.GetString(26)
                ),
                Source: JoinDisplay(
                    reader.IsDBNull(29) ? null : reader.GetString(29),
                    reader.IsDBNull(28) ? null : reader.GetString(28)
                ),
                Pages: reader.IsDBNull(30) ? null : reader.GetString(30),
                Notes: reader.IsDBNull(31) ? null : reader.GetString(31)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonAltNameItem>> GetAltNamesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonAltNameItem>();
        }

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        var items = new List<PersonAltNameItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    a.c_sequence,
    a.c_alt_name_chn,
    a.c_alt_name,
    anc.c_name_type_desc_chn,
    anc.c_name_type_desc,
    src.c_title_chn,
    src.c_title,
    a.c_pages,
    a.c_notes
FROM ALTNAME_DATA a
LEFT JOIN ALTNAME_CODES anc ON anc.c_name_type_code = a.c_alt_name_type_code
LEFT JOIN TEXT_CODES src ON src.c_textid = a.c_source
WHERE a.c_personid = $personId
ORDER BY a.c_sequence, a.c_alt_name_type_code, a.c_alt_name_chn;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonAltNameItem(
                Sequence: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                AltNameChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                AltName: reader.IsDBNull(2) ? null : reader.GetString(2),
                NameType: JoinDisplay(
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4)
                ),
                Source: JoinDisplay(
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)
                ),
                Pages: reader.IsDBNull(7) ? null : reader.GetString(7),
                Notes: reader.IsDBNull(8) ? null : reader.GetString(8)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonWritingItem>> GetWritingsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonWritingItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonWritingItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    btd.c_textid,
    tc.c_title_chn,
    tc.c_title,
    trc.c_role_desc_chn,
    trc.c_role_desc,
    btd.c_year,
    nh.c_nianhao_chn,
    nh.c_nianhao_pin,
    btd.c_nh_year,
    yrc.c_range_chn,
    yrc.c_range,
    src.c_title_chn,
    src.c_title,
    btd.c_pages,
    btd.c_notes
FROM BIOG_TEXT_DATA btd
LEFT JOIN TEXT_CODES tc ON tc.c_textid = btd.c_textid
LEFT JOIN TEXT_ROLE_CODES trc ON trc.c_role_id = btd.c_role_id
LEFT JOIN NIAN_HAO nh ON nh.c_nianhao_id = btd.c_nh_code
LEFT JOIN YEAR_RANGE_CODES yrc ON yrc.c_range_code = btd.c_range_code
LEFT JOIN TEXT_CODES src ON src.c_textid = btd.c_source
WHERE btd.c_personid = $personId
ORDER BY btd.c_year, btd.c_textid, btd.c_role_id;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonWritingItem(
                TextId: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                TitleChn: reader.IsDBNull(1) ? null : reader.GetString(1),
                Title: reader.IsDBNull(2) ? null : reader.GetString(2),
                Role: JoinDisplay(reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4)),
                Year: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Nianhao: JoinDisplay(reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7)),
                NianhaoYear: reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Range: JoinDisplay(reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10)),
                Source: JoinDisplay(reader.IsDBNull(11) ? null : reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12)),
                Pages: reader.IsDBNull(13) ? null : reader.GetString(13),
                Notes: reader.IsDBNull(14) ? null : reader.GetString(14)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonPostingItem>> GetPostingsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonPostingItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var postings = new List<PersonPostingItem>();
        var postingMap = new Dictionary<int, List<PostingOfficeAccumulator>>();
        var postingOrder = new List<int>();
        var officeMap = new Dictionary<(int PostingId, int OfficeId), PostingOfficeAccumulator>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    pd.c_posting_id,
    pto.c_office_id,
    pto.c_sequence,
    oc.c_office_chn,
    oc.c_office_pinyin,
    appt.c_appt_desc_chn,
    appt.c_appt_desc,
    assume_office.c_assume_office_desc_chn,
    assume_office.c_assume_office_desc,
    cat.c_category_desc_chn,
    cat.c_category_desc,
    pto.c_firstyear,
    fy_nh.c_nianhao_chn,
    fy_nh.c_nianhao_pin,
    pto.c_fy_nh_year,
    fy_range.c_range_chn,
    fy_range.c_range,
    pto.c_fy_month,
    pto.c_fy_intercalary,
    pto.c_fy_day,
    fy_gz.c_ganzhi_chn,
    fy_gz.c_ganzhi_py,
    pto.c_lastyear,
    ly_nh.c_nianhao_chn,
    ly_nh.c_nianhao_pin,
    pto.c_ly_nh_year,
    ly_range.c_range_chn,
    ly_range.c_range,
    pto.c_ly_month,
    pto.c_ly_intercalary,
    pto.c_ly_day,
    ly_gz.c_ganzhi_chn,
    ly_gz.c_ganzhi_py,
    dy.c_dynasty_chn,
    dy.c_dynasty,
    src.c_title_chn,
    src.c_title,
    pto.c_pages,
    pto.c_notes,
    pto.c_created_by,
    pto.c_created_date,
    pto.c_modified_by,
    pto.c_modified_date,
    office_addr.c_addr_id,
    office_addr.c_name_chn,
    office_addr.c_name,
    pta.c_created_by,
    pta.c_created_date,
    pta.c_modified_by,
    pta.c_modified_date
FROM POSTING_DATA pd
LEFT JOIN POSTED_TO_OFFICE_DATA pto
    ON pto.c_posting_id = pd.c_posting_id
   AND pto.c_personid = pd.c_personid
LEFT JOIN POSTED_TO_ADDR_DATA pta
    ON pta.c_posting_id = pto.c_posting_id
   AND pta.c_office_id = pto.c_office_id
   AND pta.c_personid = pto.c_personid
LEFT JOIN OFFICE_CODES oc ON oc.c_office_id = pto.c_office_id
LEFT JOIN APPOINTMENT_CODES appt ON appt.c_appt_code = pto.c_appt_type_code
LEFT JOIN ASSUME_OFFICE_CODES assume_office ON assume_office.c_assume_office_code = pto.c_assume_office_code
LEFT JOIN OFFICE_CATEGORIES cat ON cat.c_office_category_id = pto.c_office_category_id
LEFT JOIN NIAN_HAO fy_nh ON fy_nh.c_nianhao_id = pto.c_fy_nh_code
LEFT JOIN YEAR_RANGE_CODES fy_range ON fy_range.c_range_code = pto.c_fy_range
LEFT JOIN GANZHI_CODES fy_gz ON fy_gz.c_ganzhi_code = pto.c_fy_day_gz
LEFT JOIN NIAN_HAO ly_nh ON ly_nh.c_nianhao_id = pto.c_ly_nh_code
LEFT JOIN YEAR_RANGE_CODES ly_range ON ly_range.c_range_code = pto.c_ly_range
LEFT JOIN GANZHI_CODES ly_gz ON ly_gz.c_ganzhi_code = pto.c_ly_day_gz
LEFT JOIN DYNASTIES dy ON dy.c_dy = pto.c_dy
LEFT JOIN TEXT_CODES src ON src.c_textid = pto.c_source
LEFT JOIN ADDR_CODES office_addr ON office_addr.c_addr_id = pta.c_addr_id
WHERE pd.c_personid = $personId
ORDER BY pd.c_posting_id, pto.c_sequence, pto.c_office_id, office_addr.c_addr_id;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            var postingId = reader.GetInt32(0);
            if (!postingMap.ContainsKey(postingId)) {
                postingMap[postingId] = new List<PostingOfficeAccumulator>();
                postingOrder.Add(postingId);
            }

            if (reader.IsDBNull(1)) {
                continue;
            }

            var officeId = reader.GetInt32(1);
            var officeKey = (PostingId: postingId, OfficeId: officeId);
            if (!officeMap.TryGetValue(officeKey, out var office)) {
                office = new PostingOfficeAccumulator(
                    officeId,
                    reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    JoinDisplay(reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)),
                    JoinDisplay(reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)),
                    JoinDisplay(reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10)),
                    reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    JoinDisplay(reader.IsDBNull(12) ? null : reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetString(13)),
                    reader.IsDBNull(14) ? null : reader.GetInt32(14),
                    JoinDisplay(reader.IsDBNull(15) ? null : reader.GetString(15), reader.IsDBNull(16) ? null : reader.GetString(16)),
                    reader.IsDBNull(17) ? null : reader.GetInt32(17),
                    reader.IsDBNull(18) ? null : reader.GetInt32(18) != 0,
                    reader.IsDBNull(19) ? null : reader.GetInt32(19),
                    JoinDisplay(reader.IsDBNull(20) ? null : reader.GetString(20), reader.IsDBNull(21) ? null : reader.GetString(21)),
                    reader.IsDBNull(22) ? null : reader.GetInt32(22),
                    JoinDisplay(reader.IsDBNull(23) ? null : reader.GetString(23), reader.IsDBNull(24) ? null : reader.GetString(24)),
                    reader.IsDBNull(25) ? null : reader.GetInt32(25),
                    JoinDisplay(reader.IsDBNull(26) ? null : reader.GetString(26), reader.IsDBNull(27) ? null : reader.GetString(27)),
                    reader.IsDBNull(28) ? null : reader.GetInt32(28),
                    reader.IsDBNull(29) ? null : reader.GetInt32(29) != 0,
                    reader.IsDBNull(30) ? null : reader.GetInt32(30),
                    JoinDisplay(reader.IsDBNull(31) ? null : reader.GetString(31), reader.IsDBNull(32) ? null : reader.GetString(32)),
                    JoinDisplay(reader.IsDBNull(33) ? null : reader.GetString(33), reader.IsDBNull(34) ? null : reader.GetString(34)),
                    JoinDisplay(reader.IsDBNull(35) ? null : reader.GetString(35), reader.IsDBNull(36) ? null : reader.GetString(36)),
                    reader.IsDBNull(37) ? null : reader.GetString(37),
                    reader.IsDBNull(38) ? null : reader.GetString(38),
                    reader.IsDBNull(39) ? null : reader.GetString(39),
                    reader.IsDBNull(40) ? null : reader.GetString(40),
                    reader.IsDBNull(41) ? null : reader.GetString(41),
                    reader.IsDBNull(42) ? null : reader.GetString(42)
                );
                officeMap[officeKey] = office;
                postingMap[postingId].Add(office);
            }

            if (!reader.IsDBNull(43)) {
                office.Addresses.Add(new PersonPostingAddressItem(
                    AddressId: reader.GetInt32(43),
                    AddressNameChn: reader.IsDBNull(44) ? null : reader.GetString(44),
                    AddressName: reader.IsDBNull(45) ? null : reader.GetString(45),
                    CreatedBy: reader.IsDBNull(46) ? null : reader.GetString(46),
                    CreatedDate: reader.IsDBNull(47) ? null : reader.GetString(47),
                    ModifiedBy: reader.IsDBNull(48) ? null : reader.GetString(48),
                    ModifiedDate: reader.IsDBNull(49) ? null : reader.GetString(49)
                ));
            }
        }

        foreach (var postingId in postingOrder) {
            postings.Add(new PersonPostingItem(
                PostingId: postingId,
                Offices: postingMap[postingId]
                    .Select(office => new PersonPostingOfficeItem(
                        OfficeId: office.OfficeId,
                        Sequence: office.Sequence,
                        OfficeNameChn: office.OfficeNameChn,
                        OfficeName: office.OfficeName,
                        AppointmentType: office.AppointmentType,
                        AssumeOffice: office.AssumeOffice,
                        Category: office.Category,
                        FirstYear: office.FirstYear,
                        FirstNianhao: office.FirstNianhao,
                        FirstNianhaoYear: office.FirstNianhaoYear,
                        FirstRange: office.FirstRange,
                        FirstMonth: office.FirstMonth,
                        FirstIntercalary: office.FirstIntercalary,
                        FirstDay: office.FirstDay,
                        FirstGanzhi: office.FirstGanzhi,
                        LastYear: office.LastYear,
                        LastNianhao: office.LastNianhao,
                        LastNianhaoYear: office.LastNianhaoYear,
                        LastRange: office.LastRange,
                        LastMonth: office.LastMonth,
                        LastIntercalary: office.LastIntercalary,
                        LastDay: office.LastDay,
                        LastGanzhi: office.LastGanzhi,
                        Dynasty: office.Dynasty,
                        Source: office.Source,
                        Pages: office.Pages,
                        Notes: office.Notes,
                        CreatedBy: office.CreatedBy,
                        CreatedDate: office.CreatedDate,
                        ModifiedBy: office.ModifiedBy,
                        ModifiedDate: office.ModifiedDate,
                        Addresses: office.Addresses
                    ))
                    .ToArray()
            ));
        }

        return postings;
    }

    private sealed class PostingOfficeAccumulator(
        int officeId,
        int sequence,
        string? officeNameChn,
        string? officeName,
        string? appointmentType,
        string? assumeOffice,
        string? category,
        int? firstYear,
        string? firstNianhao,
        int? firstNianhaoYear,
        string? firstRange,
        int? firstMonth,
        bool? firstIntercalary,
        int? firstDay,
        string? firstGanzhi,
        int? lastYear,
        string? lastNianhao,
        int? lastNianhaoYear,
        string? lastRange,
        int? lastMonth,
        bool? lastIntercalary,
        int? lastDay,
        string? lastGanzhi,
        string? dynasty,
        string? source,
        string? pages,
        string? notes,
        string? createdBy,
        string? createdDate,
        string? modifiedBy,
        string? modifiedDate
    ) {
        public int OfficeId { get; } = officeId;
        public int Sequence { get; } = sequence;
        public string? OfficeNameChn { get; } = officeNameChn;
        public string? OfficeName { get; } = officeName;
        public string? AppointmentType { get; } = appointmentType;
        public string? AssumeOffice { get; } = assumeOffice;
        public string? Category { get; } = category;
        public int? FirstYear { get; } = firstYear;
        public string? FirstNianhao { get; } = firstNianhao;
        public int? FirstNianhaoYear { get; } = firstNianhaoYear;
        public string? FirstRange { get; } = firstRange;
        public int? FirstMonth { get; } = firstMonth;
        public bool? FirstIntercalary { get; } = firstIntercalary;
        public int? FirstDay { get; } = firstDay;
        public string? FirstGanzhi { get; } = firstGanzhi;
        public int? LastYear { get; } = lastYear;
        public string? LastNianhao { get; } = lastNianhao;
        public int? LastNianhaoYear { get; } = lastNianhaoYear;
        public string? LastRange { get; } = lastRange;
        public int? LastMonth { get; } = lastMonth;
        public bool? LastIntercalary { get; } = lastIntercalary;
        public int? LastDay { get; } = lastDay;
        public string? LastGanzhi { get; } = lastGanzhi;
        public string? Dynasty { get; } = dynasty;
        public string? Source { get; } = source;
        public string? Pages { get; } = pages;
        public string? Notes { get; } = notes;
        public string? CreatedBy { get; } = createdBy;
        public string? CreatedDate { get; } = createdDate;
        public string? ModifiedBy { get; } = modifiedBy;
        public string? ModifiedDate { get; } = modifiedDate;
        public List<PersonPostingAddressItem> Addresses { get; } = new();
    }

    public async Task<IReadOnlyList<PersonEntryItem>> GetEntriesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonEntryItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonEntryItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ed.c_sequence,
    ec.c_entry_desc_chn,
    ec.c_entry_desc,
    ed.c_exam_rank,
    ed.c_year,
    nh.c_nianhao_chn,
    nh.c_nianhao_pin,
    ed.c_entry_nh_year,
    yr.c_range_chn,
    yr.c_range,
    ed.c_age,
    kc.c_kinrel_chn,
    kc.c_kinrel,
    kin.c_name_chn,
    kin.c_name,
    ac.c_assoc_desc_chn,
    ac.c_assoc_desc,
    assoc.c_name_chn,
    assoc.c_name,
    sinc.c_inst_name_hz,
    sinc.c_inst_name_py,
    addr.c_name_chn,
    addr.c_name,
    psc.c_parental_status_desc_chn,
    psc.c_parental_status_desc,
    src.c_title_chn,
    src.c_title,
    ed.c_pages,
    ed.c_notes,
    ed.c_posting_notes
FROM ENTRY_DATA ed
LEFT JOIN ENTRY_CODES ec ON ec.c_entry_code = ed.c_entry_code
LEFT JOIN NIAN_HAO nh ON nh.c_nianhao_id = ed.c_entry_nh_id
LEFT JOIN YEAR_RANGE_CODES yr ON yr.c_range_code = ed.c_entry_range
LEFT JOIN KINSHIP_CODES kc ON kc.c_kincode = ed.c_kin_code
LEFT JOIN BIOG_MAIN kin ON kin.c_personid = ed.c_kin_id
LEFT JOIN ASSOC_CODES ac ON ac.c_assoc_code = ed.c_assoc_code
LEFT JOIN BIOG_MAIN assoc ON assoc.c_personid = ed.c_assoc_id
LEFT JOIN SOCIAL_INSTITUTION_NAME_CODES sinc ON sinc.c_inst_name_code = ed.c_inst_name_code
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = ed.c_entry_addr_id
LEFT JOIN PARENTAL_STATUS_CODES psc ON psc.c_parental_status_code = ed.c_parental_status_code
LEFT JOIN TEXT_CODES src ON src.c_textid = ed.c_source
WHERE ed.c_personid = $personId
ORDER BY ed.c_year, ed.c_sequence, ed.c_entry_code;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonEntryItem(
                Sequence: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                EntryMethod: JoinDisplay(reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)),
                ExamRank: reader.IsDBNull(3) ? null : reader.GetString(3),
                Year: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Nianhao: JoinDisplay(reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)),
                NianhaoYear: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Dynasty: null,
                Range: JoinDisplay(reader.IsDBNull(8) ? null : reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9)),
                Age: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                Kinship: JoinDisplay(reader.IsDBNull(11) ? null : reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12)),
                KinNameChn: reader.IsDBNull(13) ? null : reader.GetString(13),
                KinName: reader.IsDBNull(14) ? null : reader.GetString(14),
                Association: JoinDisplay(reader.IsDBNull(15) ? null : reader.GetString(15), reader.IsDBNull(16) ? null : reader.GetString(16)),
                AssociateNameChn: reader.IsDBNull(17) ? null : reader.GetString(17),
                AssociateName: reader.IsDBNull(18) ? null : reader.GetString(18),
                InstitutionNameChn: reader.IsDBNull(19) ? null : reader.GetString(19),
                InstitutionName: reader.IsDBNull(20) ? null : reader.GetString(20),
                EntryAddressChn: reader.IsDBNull(21) ? null : reader.GetString(21),
                EntryAddress: reader.IsDBNull(22) ? null : reader.GetString(22),
                ParentalStatus: JoinDisplay(reader.IsDBNull(23) ? null : reader.GetString(23), reader.IsDBNull(24) ? null : reader.GetString(24)),
                Source: JoinDisplay(reader.IsDBNull(25) ? null : reader.GetString(25), reader.IsDBNull(26) ? null : reader.GetString(26)),
                Pages: reader.IsDBNull(27) ? null : reader.GetString(27),
                Notes: reader.IsDBNull(28) ? null : reader.GetString(28),
                PostingNotes: reader.IsDBNull(29) ? null : reader.GetString(29)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonStatusItem>> GetStatusesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonStatusItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonStatusItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    sd.c_sequence,
    sc.c_status_desc_chn,
    sc.c_status_desc,
    sd.c_firstyear,
    fy_nh.c_nianhao_chn,
    fy_nh.c_nianhao_pin,
    sd.c_fy_nh_year,
    fy_range.c_range_chn,
    fy_range.c_range,
    sd.c_lastyear,
    ly_nh.c_nianhao_chn,
    ly_nh.c_nianhao_pin,
    sd.c_ly_nh_year,
    ly_range.c_range_chn,
    ly_range.c_range,
    src.c_title_chn,
    src.c_title,
    sd.c_pages,
    sd.c_notes
FROM STATUS_DATA sd
LEFT JOIN STATUS_CODES sc ON sc.c_status_code = sd.c_status_code
LEFT JOIN NIAN_HAO fy_nh ON fy_nh.c_nianhao_id = sd.c_fy_nh_code
LEFT JOIN YEAR_RANGE_CODES fy_range ON fy_range.c_range_code = sd.c_fy_range
LEFT JOIN NIAN_HAO ly_nh ON ly_nh.c_nianhao_id = sd.c_ly_nh_code
LEFT JOIN YEAR_RANGE_CODES ly_range ON ly_range.c_range_code = sd.c_ly_range
LEFT JOIN TEXT_CODES src ON src.c_textid = sd.c_source
WHERE sd.c_personid = $personId
ORDER BY sd.c_sequence, sd.c_status_code;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonStatusItem(
                Sequence: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Status: JoinDisplay(reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)),
                FirstYear: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                FirstNianhao: JoinDisplay(reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)),
                FirstNianhaoYear: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                FirstRange: JoinDisplay(reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)),
                LastYear: reader.IsDBNull(9) ? null : reader.GetInt32(9),
                LastNianhao: JoinDisplay(reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11)),
                LastNianhaoYear: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                LastRange: JoinDisplay(reader.IsDBNull(13) ? null : reader.GetString(13), reader.IsDBNull(14) ? null : reader.GetString(14)),
                Source: JoinDisplay(reader.IsDBNull(15) ? null : reader.GetString(15), reader.IsDBNull(16) ? null : reader.GetString(16)),
                Pages: reader.IsDBNull(17) ? null : reader.GetString(17),
                Notes: reader.IsDBNull(18) ? null : reader.GetString(18)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonPossessionItem>> GetPossessionsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonPossessionItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonPossessionItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    pd.c_possession_record_id,
    pd.c_sequence,
    pd.c_possession_desc_chn,
    pd.c_possession_desc,
    pac.c_possession_act_desc_chn,
    pac.c_possession_act_desc,
    pd.c_quantity,
    mc.c_measure_desc_chn,
    mc.c_measure_desc,
    pd.c_possession_yr,
    nh.c_nianhao_chn,
    nh.c_nianhao_pin,
    pd.c_possession_nh_yr,
    yr.c_range_chn,
    yr.c_range,
    addr.c_name_chn,
    addr.c_name,
    src.c_title_chn,
    src.c_title,
    pd.c_pages,
    pd.c_notes
FROM POSSESSION_DATA pd
LEFT JOIN POSSESSION_ACT_CODES pac ON pac.c_possession_act_code = pd.c_possession_act_code
LEFT JOIN MEASURE_CODES mc ON mc.c_measure_code = pd.c_measure_code
LEFT JOIN NIAN_HAO nh ON nh.c_nianhao_id = pd.c_possession_nh_code
LEFT JOIN YEAR_RANGE_CODES yr ON yr.c_range_code = pd.c_possession_yr_range
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = pd.c_addr_id
LEFT JOIN TEXT_CODES src ON src.c_textid = pd.c_source
WHERE pd.c_personid = $personId
ORDER BY pd.c_sequence, pd.c_possession_record_id;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonPossessionItem(
                RecordId: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Sequence: reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Possession: JoinDisplay(reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)),
                PossessionAction: JoinDisplay(reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)),
                Quantity: reader.IsDBNull(6) ? null : reader.GetString(6),
                Measure: JoinDisplay(reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)),
                Year: reader.IsDBNull(9) ? null : reader.GetInt32(9),
                Nianhao: JoinDisplay(reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11)),
                NianhaoYear: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                Range: JoinDisplay(reader.IsDBNull(13) ? null : reader.GetString(13), reader.IsDBNull(14) ? null : reader.GetString(14)),
                AddressNameChn: reader.IsDBNull(15) ? null : reader.GetString(15),
                AddressName: reader.IsDBNull(16) ? null : reader.GetString(16),
                Source: JoinDisplay(reader.IsDBNull(17) ? null : reader.GetString(17), reader.IsDBNull(18) ? null : reader.GetString(18)),
                Pages: reader.IsDBNull(19) ? null : reader.GetString(19),
                Notes: reader.IsDBNull(20) ? null : reader.GetString(20)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonEventItem>> GetEventsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonEventItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonEventItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ed.c_sequence,
    ec.c_event_name_chn,
    ec.c_event_name,
    ed.c_role,
    ed.c_year,
    nh.c_nianhao_chn,
    nh.c_nianhao_pin,
    ed.c_nh_year,
    ed.c_month,
    ed.c_intercalary,
    ed.c_day,
    gz.c_ganzhi_chn,
    gz.c_ganzhi_py,
    yr.c_range_chn,
    yr.c_range,
    addr.c_name_chn,
    addr.c_name,
    src.c_title_chn,
    src.c_title,
    ed.c_pages,
    ed.c_event,
    ed.c_notes
FROM EVENTS_DATA ed
LEFT JOIN EVENT_CODES ec ON ec.c_event_code = ed.c_event_code
LEFT JOIN NIAN_HAO nh ON nh.c_nianhao_id = ed.c_nh_code
LEFT JOIN GANZHI_CODES gz ON gz.c_ganzhi_code = ed.c_day_ganzhi
LEFT JOIN YEAR_RANGE_CODES yr ON yr.c_range_code = ed.c_yr_range
LEFT JOIN ADDR_CODES addr ON addr.c_addr_id = ed.c_addr_id
LEFT JOIN TEXT_CODES src ON src.c_textid = ed.c_source
WHERE ed.c_personid = $personId
ORDER BY ed.c_year, ed.c_sequence, ed.c_event_code;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonEventItem(
                Sequence: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                EventName: JoinDisplay(reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)),
                Role: reader.IsDBNull(3) ? null : reader.GetString(3),
                Year: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Nianhao: JoinDisplay(reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)),
                NianhaoYear: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Month: reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Intercalary: reader.IsDBNull(9) ? null : reader.GetInt32(9) == 1,
                Day: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                Ganzhi: JoinDisplay(reader.IsDBNull(11) ? null : reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12)),
                Range: JoinDisplay(reader.IsDBNull(13) ? null : reader.GetString(13), reader.IsDBNull(14) ? null : reader.GetString(14)),
                AddressNameChn: reader.IsDBNull(15) ? null : reader.GetString(15),
                AddressName: reader.IsDBNull(16) ? null : reader.GetString(16),
                Source: JoinDisplay(reader.IsDBNull(17) ? null : reader.GetString(17), reader.IsDBNull(18) ? null : reader.GetString(18)),
                Pages: reader.IsDBNull(19) ? null : reader.GetString(19),
                EventText: reader.IsDBNull(20) ? null : reader.GetString(20),
                Notes: reader.IsDBNull(21) ? null : reader.GetString(21)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonKinshipItem>> GetKinshipsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonKinshipItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonKinshipItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    kd.c_kin_id,
    kc.c_kinrel_chn,
    kc.c_kinrel,
    kin.c_name_chn,
    kin.c_name,
    kc.c_upstep,
    kc.c_dwnstep,
    kc.c_marstep,
    kc.c_colstep,
    src.c_title_chn,
    src.c_title,
    kd.c_pages,
    kd.c_notes
FROM KIN_DATA kd
LEFT JOIN KINSHIP_CODES kc ON kc.c_kincode = kd.c_kin_code
LEFT JOIN BIOG_MAIN kin ON kin.c_personid = kd.c_kin_id
LEFT JOIN TEXT_CODES src ON src.c_textid = kd.c_source
WHERE kd.c_personid = $personId
ORDER BY kd.c_kin_id, kd.c_kin_code;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonKinshipItem(
                KinPersonId: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Kinship: JoinDisplay(reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)),
                KinNameChn: reader.IsDBNull(3) ? null : reader.GetString(3),
                KinName: reader.IsDBNull(4) ? null : reader.GetString(4),
                UpStep: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                DownStep: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                MarriageStep: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                CollateralStep: reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Source: JoinDisplay(reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10)),
                Pages: reader.IsDBNull(11) ? null : reader.GetString(11),
                Notes: reader.IsDBNull(12) ? null : reader.GetString(12)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonSourceItem>> GetSourcesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonSourceItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonSourceItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    tc.c_title_chn,
    tc.c_title,
    bsd.c_pages,
    bsd.c_notes,
    bsd.c_main_source,
    bsd.c_self_bio,
    CASE
        WHEN tc.c_url_api IS NULL AND tc.c_url_api_coda IS NULL THEN NULL
        ELSE COALESCE(tc.c_url_api, '') || COALESCE(bsd.c_pages, '') || COALESCE(tc.c_url_api_coda, '')
    END AS c_hyperlink
FROM BIOG_SOURCE_DATA bsd
LEFT JOIN TEXT_CODES tc ON tc.c_textid = bsd.c_textid
WHERE bsd.c_personid = $personId
ORDER BY bsd.c_textid, bsd.c_pages;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonSourceItem(
                TitleChn: reader.IsDBNull(0) ? null : reader.GetString(0),
                Title: reader.IsDBNull(1) ? null : reader.GetString(1),
                Pages: reader.IsDBNull(2) ? null : reader.GetString(2),
                Notes: reader.IsDBNull(3) ? null : reader.GetString(3),
                MainSource: reader.IsDBNull(4) ? null : reader.GetInt32(4) == 1,
                SelfBio: reader.IsDBNull(5) ? null : reader.GetInt32(5) == 1,
                Hyperlink: reader.IsDBNull(6) ? null : reader.GetString(6)
            ));
        }

        return items;
    }

    public async Task<IReadOnlyList<PersonInstitutionItem>> GetInstitutionsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PersonInstitutionItem>();
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = SqliteOpenMode.ReadOnly };
        var items = new List<PersonInstitutionItem>();

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    sinc.c_inst_name_hz,
    sinc.c_inst_name_py,
    bic.c_bi_role_chn,
    bic.c_bi_role_desc,
    bid.c_bi_begin_year,
    by_nh.c_nianhao_chn,
    by_nh.c_nianhao_pin,
    bid.c_bi_by_nh_year,
    by_range.c_range_chn,
    by_range.c_range,
    bid.c_bi_end_year,
    ey_nh.c_nianhao_chn,
    ey_nh.c_nianhao_pin,
    bid.c_bi_ey_nh_year,
    ey_range.c_range_chn,
    ey_range.c_range,
    ac.c_name_chn,
    ac.c_name,
    siat.c_inst_addr_type_chn,
    siat.c_inst_addr_type_desc,
    src.c_title_chn,
    src.c_title,
    bid.c_pages,
    bid.c_notes,
    sia.inst_xcoord,
    sia.inst_ycoord
FROM BIOG_INST_DATA bid
LEFT JOIN SOCIAL_INSTITUTION_NAME_CODES sinc ON sinc.c_inst_name_code = bid.c_inst_name_code
LEFT JOIN BIOG_INST_CODES bic ON bic.c_bi_role_code = bid.c_bi_role_code
LEFT JOIN NIAN_HAO by_nh ON by_nh.c_nianhao_id = bid.c_bi_by_nh_code
LEFT JOIN YEAR_RANGE_CODES by_range ON by_range.c_range_code = bid.c_bi_by_range
LEFT JOIN NIAN_HAO ey_nh ON ey_nh.c_nianhao_id = bid.c_bi_ey_nh_code
LEFT JOIN YEAR_RANGE_CODES ey_range ON ey_range.c_range_code = bid.c_bi_ey_range
LEFT JOIN SOCIAL_INSTITUTION_ADDR sia
    ON sia.c_inst_code = bid.c_inst_code
   AND sia.c_inst_name_code = bid.c_inst_name_code
LEFT JOIN ADDR_CODES ac ON ac.c_addr_id = sia.c_inst_addr_id
LEFT JOIN SOCIAL_INSTITUTION_ADDR_TYPES siat ON siat.c_inst_addr_type_code = sia.c_inst_addr_type_code
LEFT JOIN TEXT_CODES src ON src.c_textid = bid.c_source
WHERE bid.c_personid = $personId
ORDER BY bid.c_bi_begin_year, bid.c_inst_name_code, bid.c_inst_code;";
        command.Parameters.AddWithValue("$personId", personId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            items.Add(new PersonInstitutionItem(
                InstitutionNameChn: reader.IsDBNull(0) ? null : reader.GetString(0),
                InstitutionName: reader.IsDBNull(1) ? null : reader.GetString(1),
                Role: JoinDisplay(reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)),
                BeginYear: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                BeginNianhao: JoinDisplay(reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)),
                BeginNianhaoYear: reader.IsDBNull(7) ? null : reader.GetInt32(7),
                BeginRange: JoinDisplay(reader.IsDBNull(8) ? null : reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9)),
                EndYear: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                EndNianhao: JoinDisplay(reader.IsDBNull(11) ? null : reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12)),
                EndNianhaoYear: reader.IsDBNull(13) ? null : reader.GetInt32(13),
                EndRange: JoinDisplay(reader.IsDBNull(14) ? null : reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetString(15)),
                PlaceNameChn: reader.IsDBNull(16) ? null : reader.GetString(16),
                PlaceName: reader.IsDBNull(17) ? null : reader.GetString(17),
                PlaceType: JoinDisplay(reader.IsDBNull(18) ? null : reader.GetString(18), reader.IsDBNull(19) ? null : reader.GetString(19)),
                Source: JoinDisplay(reader.IsDBNull(20) ? null : reader.GetString(20), reader.IsDBNull(21) ? null : reader.GetString(21)),
                Pages: reader.IsDBNull(22) ? null : reader.GetString(22),
                Notes: reader.IsDBNull(23) ? null : reader.GetString(23),
                XCoord: reader.IsDBNull(24) ? null : reader.GetDouble(24),
                YCoord: reader.IsDBNull(25) ? null : reader.GetDouble(25)
            ));
        }

        return items;
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
            ManualLookupColumns.TryGetValue(columnName, out foreignKey);
        }
        if (foreignKey is null) {
            return rawValue;
        }

        var resolved = await ResolveForeignKeyTextAsync(connection, foreignKey, rawValue, cancellationToken);
        if (string.IsNullOrWhiteSpace(resolved)) {
            return rawValue;
        }
        return ShouldDisplayLookupOnly(columnName) ? resolved : $"{rawValue} | {resolved}";
    }

    private static bool ShouldDisplayLookupOnly(string columnName) {
        return ManualLookupColumns.ContainsKey(columnName)
            || columnName.EndsWith("_code", StringComparison.OrdinalIgnoreCase)
            || columnName.EndsWith("_range", StringComparison.OrdinalIgnoreCase);
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
