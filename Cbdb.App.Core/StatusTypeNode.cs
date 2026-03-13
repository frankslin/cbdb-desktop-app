namespace Cbdb.App.Core;

public sealed record StatusTypeNode(
    string Code,
    string? Description,
    string? DescriptionChn,
    IReadOnlyList<StatusTypeNode> Children,
    IReadOnlyList<string> StatusCodes
) {
    public bool IsRoot => string.Equals(Code, StatusPickerData.RootCode, StringComparison.Ordinal);
}
