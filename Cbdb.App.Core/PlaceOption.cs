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
    double? XCoord,
    double? YCoord
) {
    public string DisplayLabel => string.IsNullOrWhiteSpace(NameChn)
        ? string.IsNullOrWhiteSpace(Name)
            ? $"#{AddressId}"
            : $"#{AddressId} {Name}"
        : string.IsNullOrWhiteSpace(Name)
            ? $"#{AddressId} {NameChn}"
            : $"#{AddressId} {NameChn} / {Name}";

    public string DetailLabel {
        get {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(AdminType)) {
                parts.Add(AdminType);
            }

            if (FirstYear.HasValue || LastYear.HasValue) {
                parts.Add($"{FirstYear?.ToString() ?? "?"}-{LastYear?.ToString() ?? "?"}");
            }

            if (!string.IsNullOrWhiteSpace(BelongsToNameChn) || !string.IsNullOrWhiteSpace(BelongsToName)) {
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
