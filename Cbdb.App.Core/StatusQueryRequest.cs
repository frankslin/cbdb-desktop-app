namespace Cbdb.App.Core;

public sealed record StatusQueryRequest(
    string? PersonKeyword,
    IReadOnlyList<string> StatusCodes,
    bool UseIndexYearRange,
    int IndexYearFrom,
    int IndexYearTo,
    bool UseDynastyRange,
    DynastyOption? DynastyFrom,
    DynastyOption? DynastyTo,
    int Limit = 5000
);
