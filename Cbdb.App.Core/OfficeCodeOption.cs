namespace Cbdb.App.Core;

public sealed record OfficeCodeOption(
    string Code,
    string? Description,
    string? DescriptionChn,
    string? Dynasty,
    string? DynastyChn,
    int UsageCount
) {
    public string DisplayLabel {
        get {
            var name = string.IsNullOrWhiteSpace(DescriptionChn)
                ? Description ?? Code
                : string.IsNullOrWhiteSpace(Description)
                    ? DescriptionChn
                    : $"{DescriptionChn} / {Description}";

            var dynasty = string.IsNullOrWhiteSpace(DynastyChn)
                ? Dynasty
                : string.IsNullOrWhiteSpace(Dynasty)
                    ? DynastyChn
                    : $"{DynastyChn} / {Dynasty}";

            return string.IsNullOrWhiteSpace(dynasty)
                ? $"{name} ({Code})"
                : $"{name} [{dynasty}] ({Code})";
        }
    }
}
