namespace Cbdb.App.Core;

public sealed record OfficeQueryRequest(
    string? PersonKeyword,
    IReadOnlyList<string> OfficeCodes,
    IReadOnlyList<int> PlaceIds,
    bool IncludeSubordinateUnits,
    bool UseIndexYearRange,
    int IndexYearFrom,
    int IndexYearTo,
    bool UseDynastyRange,
    DynastyOption? DynastyFrom,
    DynastyOption? DynastyTo,
    int Limit = 5000
);
