namespace Cbdb.App.Core;

public sealed record EntryQueryPerson(
    int PersonId,
    string? NameChn,
    string? Name,
    int? IndexYear,
    string? IndexYearType,
    string? Sex,
    string? Dynasty,
    int? IndexAddressId,
    string? IndexAddress,
    int? EntryAddressId,
    string? EntryAddress,
    double? EntryXCoord,
    double? EntryYCoord,
    int XyCount,
    int EntryCount
);
