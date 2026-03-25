namespace Cbdb.App.Core;

public sealed record OfficeQueryRequest(
    string? PersonKeyword,
    IReadOnlyList<string> OfficeCodes,
    IReadOnlyList<int> PersonPlaceIds,
    bool IncludeSubordinatePersonUnits,
    IReadOnlyList<int> OfficePlaceIds,
    bool IncludeSubordinateOfficeUnits,
    bool UseIndexYearRange,
    int IndexYearFrom,
    int IndexYearTo,
    bool UseOfficeYearRange,
    int OfficeYearFrom,
    int OfficeYearTo,
    IReadOnlyList<int> DynastyIds,
    int Limit = 5000
);
