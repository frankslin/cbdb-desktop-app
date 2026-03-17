namespace Cbdb.App.Core;

public sealed record EntryQueryRequest(
    string? PersonKeyword,
    IReadOnlyList<string> EntryCodes,
    IReadOnlyList<int> PlaceIds,
    bool IncludeSubordinateUnits,
    bool UseIndexYearRange,
    int IndexYearFrom,
    int IndexYearTo,
    bool UseEntryYearRange,
    int EntryYearFrom,
    int EntryYearTo,
    bool UseDynastyRange,
    DynastyOption? DynastyFrom,
    DynastyOption? DynastyTo,
    int Limit = 5000
);
