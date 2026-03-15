namespace Cbdb.App.Core;

public sealed record EntryCodeOption(
    string Code,
    string? Description,
    string? DescriptionChn,
    int UsageCount
) {
    public string DisplayLabel => string.IsNullOrWhiteSpace(DescriptionChn)
        ? string.IsNullOrWhiteSpace(Description)
            ? Code
            : $"{Description} ({Code})"
        : string.IsNullOrWhiteSpace(Description)
            ? $"{DescriptionChn} ({Code})"
            : $"{DescriptionChn} / {Description} ({Code})";
}
