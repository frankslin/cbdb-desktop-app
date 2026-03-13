namespace Cbdb.App.Core;

public sealed record StatusPickerData(
    StatusTypeNode Root,
    IReadOnlyList<StatusCodeOption> AllStatusCodes,
    IReadOnlyDictionary<string, string> StatusCodeToTypeCode
) {
    public const string RootCode = "Root";
}
