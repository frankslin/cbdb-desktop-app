namespace Cbdb.App.Core;

public sealed record StatusQueryRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? IndexYear,
    string? Dynasty,
    string? IndexAddress,
    int Sequence,
    string? Status,
    string StatusCode,
    int? FirstYear,
    string? FirstNianhao,
    int? FirstNianhaoYear,
    string? FirstRange,
    int? LastYear,
    string? LastNianhao,
    int? LastNianhaoYear,
    string? LastRange,
    string? Source,
    string? Pages,
    string? Notes
);
