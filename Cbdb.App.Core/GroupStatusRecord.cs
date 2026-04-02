namespace Cbdb.App.Core;

public sealed record GroupStatusRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? Sequence,
    string? Status,
    int? FirstYear,
    int? LastYear,
    string? Source,
    string? Pages,
    string? Notes
);
