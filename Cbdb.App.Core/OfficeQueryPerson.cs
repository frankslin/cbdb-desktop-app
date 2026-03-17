namespace Cbdb.App.Core;

public sealed record OfficeQueryPerson(
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
    int? OfficeAddressId,
    string? OfficeAddress,
    double? OfficeXCoord,
    double? OfficeYCoord,
    int XyCount,
    int PostingCount
);
