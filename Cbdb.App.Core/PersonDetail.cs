namespace Cbdb.App.Core;

public sealed record PersonDetail(
    int PersonId,
    string? Name,
    string? NameChn,
    int? IndexYear,
    string? Dynasty,
    string? DynastyChn,
    int? BirthYear,
    int? DeathYear,
    string? Gender,
    string? IndexAddress,
    string? IndexAddressChn,
    int AltNameCount,
    int KinCount,
    int AssocCount,
    int OfficeCount,
    int EntryCount,
    int StatusCount,
    int TextCount
);
