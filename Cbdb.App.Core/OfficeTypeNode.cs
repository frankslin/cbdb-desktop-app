namespace Cbdb.App.Core;

public sealed record OfficeTypeNode(
    string Code,
    string? Description,
    string? DescriptionChn,
    IReadOnlyList<OfficeTypeNode> Children,
    IReadOnlyList<string> OfficeCodes
) {
    public bool IsRoot => string.Equals(Code, OfficePickerData.RootCode, StringComparison.Ordinal);
}
