namespace Cbdb.App.Core;

public sealed record OfficeCodeOption(
    string Code,
    string? Description,
    string? DescriptionChn,
    string? DescriptionAlt,
    string? DescriptionChnAlt,
    string? Dynasty,
    string? DynastyChn,
    int UsageCount
) {
    public string DisplayLabel {
        get {
            return string.IsNullOrWhiteSpace(DescriptionChn)
                ? Description ?? Code
                : string.IsNullOrWhiteSpace(Description)
                    ? DescriptionChn
                    : $"{DescriptionChn} / {Description}";
        }
    }

    public string? DynastyLabel => string.IsNullOrWhiteSpace(DynastyChn)
        ? Dynasty
        : string.IsNullOrWhiteSpace(Dynasty)
            ? DynastyChn
            : $"{DynastyChn} / {Dynasty}";
}
