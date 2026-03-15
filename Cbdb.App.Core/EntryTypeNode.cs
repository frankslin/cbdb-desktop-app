namespace Cbdb.App.Core;

public sealed record EntryTypeNode(
    string Code,
    string? Description,
    string? DescriptionChn,
    IReadOnlyList<EntryTypeNode> Children,
    IReadOnlyList<string> EntryCodes
) {
    public bool IsRoot => string.Equals(Code, EntryPickerData.RootCode, StringComparison.Ordinal);
}
