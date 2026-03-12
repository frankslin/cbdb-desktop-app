namespace Cbdb.App.Core;

public sealed record StatusQueryPerson(
    int PersonId,
    string? NameChn,
    string? Name,
    int? IndexYear,
    string? Dynasty,
    string? IndexAddress,
    int StatusCount
);
