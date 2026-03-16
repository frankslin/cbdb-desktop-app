namespace Cbdb.App.Core;

public sealed record StatusQueryPerson(
    int PersonId,
    string? NameChn,
    string? Name,
    int? IndexYear,
    string? IndexYearType,
    string? Sex,
    string? Dynasty,
    int? IndexAddressId,
    string? IndexAddress,
    string? IndexAddressType,
    double? XCoord,
    double? YCoord,
    int XyCount,
    int StatusCount
);
