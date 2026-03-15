namespace Cbdb.App.Core;

public sealed record EntryPickerData(
    EntryTypeNode Root,
    IReadOnlyList<EntryCodeOption> AllEntryCodes,
    IReadOnlyDictionary<string, string> EntryCodeToTypeCode
) {
    public const string RootCode = "Root";
}
