namespace Cbdb.App.Core;

public sealed record DynastyOption(
    int DynastyId,
    string? Name,
    string? NameChn,
    int? StartYear,
    int? EndYear
) {
    public string DisplayLabel {
        get {
            var label = string.IsNullOrWhiteSpace(NameChn)
                ? Name ?? DynastyId.ToString()
                : string.IsNullOrWhiteSpace(Name)
                    ? NameChn
                    : $"{NameChn} / {Name}";

            if (StartYear.HasValue || EndYear.HasValue) {
                return $"{label} ({FormatYear(StartYear)}-{FormatYear(EndYear)})";
            }

            return label;
        }
    }

    private static string FormatYear(int? year) {
        return year.HasValue ? year.Value.ToString() : "?";
    }

    public override string ToString() => DisplayLabel;
}
