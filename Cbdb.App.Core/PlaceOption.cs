namespace Cbdb.App.Core;

public sealed record PlaceOption(
    int AddressId,
    string? Name,
    string? NameChn,
    string? AdminType,
    int? FirstYear,
    int? LastYear,
    int? BelongsToId,
    string? BelongsToName,
    string? BelongsToNameChn,
    string? BelongsToSummary,
    double? XCoord,
    double? YCoord
) {
    public string DisplayLabel {
        get {
            var primary = string.IsNullOrWhiteSpace(NameChn) ? Name : NameChn;
            var secondary = string.IsNullOrWhiteSpace(NameChn) || string.IsNullOrWhiteSpace(Name)
                ? null
                : Name;

            return string.IsNullOrWhiteSpace(primary)
                ? $"({AddressId})"
                : string.IsNullOrWhiteSpace(secondary)
                    ? $"{primary} ({AddressId})"
                    : $"{primary} / {secondary} ({AddressId})";
        }
    }

    public string DetailLabel {
        get {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(AdminType)) {
                parts.Add(AdminType);
            }

            if (FirstYear.HasValue || LastYear.HasValue) {
                parts.Add($"{FirstYear?.ToString() ?? "?"}-{LastYear?.ToString() ?? "?"}");
            }

            if (!string.IsNullOrWhiteSpace(BelongsToSummary)) {
                parts.Add($"> {BelongsToSummary}");
            } else if (!string.IsNullOrWhiteSpace(BelongsToNameChn) || !string.IsNullOrWhiteSpace(BelongsToName)) {
                var belongsTo = string.IsNullOrWhiteSpace(BelongsToNameChn)
                    ? BelongsToName
                    : string.IsNullOrWhiteSpace(BelongsToName)
                        ? BelongsToNameChn
                        : $"{BelongsToNameChn} / {BelongsToName}";

                if (BelongsToId.HasValue) {
                    belongsTo = $"{belongsTo} ({BelongsToId.Value})";
                }

                parts.Add($"> {belongsTo}");
            }

            if (XCoord.HasValue && YCoord.HasValue) {
                parts.Add($"XY {XCoord.Value:0.###},{YCoord.Value:0.###}");
            }

            return string.Join(" | ", parts);
        }
    }
}
