using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Data;

public sealed class SqlitePlaceLookupService : IPlaceLookupService {
    public async Task<IReadOnlyList<PlaceOption>> GetPlacesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    ) {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath)) {
            return Array.Empty<PlaceOption>();
        }

        var rows = new List<PlaceRow>();

        var builder = new SqliteConnectionStringBuilder {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    ac.c_addr_id,
    ac.c_name,
    ac.c_name_chn,
    CAST(ac.c_admin_type AS TEXT) AS c_admin_type,
    abd.c_firstyear,
    abd.c_lastyear,
    abd.c_belongs_to,
    parent.c_name AS belongs_to_name,
    parent.c_name_chn AS belongs_to_name_chn,
    ac.x_coord,
    ac.y_coord
FROM ADDR_CODES ac
LEFT JOIN ADDR_BELONGS_DATA abd ON abd.c_addr_id = ac.c_addr_id
LEFT JOIN ADDR_CODES parent ON parent.c_addr_id = abd.c_belongs_to
ORDER BY COALESCE(ac.c_name_chn, ac.c_name), ac.c_addr_id;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            rows.Add(new PlaceRow(
                AddressId: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Name: reader.IsDBNull(1) ? null : reader.GetString(1),
                NameChn: reader.IsDBNull(2) ? null : reader.GetString(2),
                AdminType: reader.IsDBNull(3) ? null : reader.GetString(3),
                FirstYear: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                LastYear: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                BelongsToId: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                BelongsToName: reader.IsDBNull(7) ? null : reader.GetString(7),
                BelongsToNameChn: reader.IsDBNull(8) ? null : reader.GetString(8),
                XCoord: reader.IsDBNull(9) ? null : reader.GetDouble(9),
                YCoord: reader.IsDBNull(10) ? null : reader.GetDouble(10)
            ));
        }

        return rows
            .GroupBy(row => row.AddressId)
            .Select(group => {
                var first = group.First();
                var belongsToSummary = string.Join("; ", group
                    .Select(BuildBelongsToLabel)
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

                return new PlaceOption(
                    AddressId: first.AddressId,
                    Name: first.Name,
                    NameChn: first.NameChn,
                    AdminType: first.AdminType,
                    FirstYear: group.Where(row => row.FirstYear.HasValue).Select(row => row.FirstYear).Min(),
                    LastYear: group.Where(row => row.LastYear.HasValue).Select(row => row.LastYear).Max(),
                    BelongsToId: first.BelongsToId,
                    BelongsToName: first.BelongsToName,
                    BelongsToNameChn: first.BelongsToNameChn,
                    BelongsToSummary: string.IsNullOrWhiteSpace(belongsToSummary) ? null : belongsToSummary,
                    XCoord: first.XCoord,
                    YCoord: first.YCoord
                );
            })
            .OrderBy(place => place.NameChn ?? place.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(place => place.AddressId)
            .ToList();
    }

    private static string? BuildBelongsToLabel(PlaceRow row) {
        if (row.BelongsToId is null && string.IsNullOrWhiteSpace(row.BelongsToName) && string.IsNullOrWhiteSpace(row.BelongsToNameChn)) {
            return null;
        }

        var belongsTo = string.IsNullOrWhiteSpace(row.BelongsToNameChn)
            ? row.BelongsToName
            : string.IsNullOrWhiteSpace(row.BelongsToName)
                ? row.BelongsToNameChn
                : $"{row.BelongsToNameChn} / {row.BelongsToName}";

        if (row.BelongsToId.HasValue) {
            belongsTo = $"{belongsTo} ({row.BelongsToId.Value})";
        }

        return belongsTo;
    }

    private sealed record PlaceRow(
        int AddressId,
        string? Name,
        string? NameChn,
        string? AdminType,
        int? FirstYear,
        int? LastYear,
        int? BelongsToId,
        string? BelongsToName,
        string? BelongsToNameChn,
        double? XCoord,
        double? YCoord
    );
}
