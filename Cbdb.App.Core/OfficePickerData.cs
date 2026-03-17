namespace Cbdb.App.Core;

public sealed record OfficePickerData(
    OfficeTypeNode Root,
    IReadOnlyList<OfficeCodeOption> AllOfficeCodes,
    IReadOnlyDictionary<string, string> OfficeCodeToTypeCode
) {
    public const string RootCode = "Root";
}
