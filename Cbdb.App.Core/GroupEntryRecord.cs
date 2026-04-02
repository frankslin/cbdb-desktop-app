namespace Cbdb.App.Core;

public sealed record GroupEntryRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? Sequence,
    string? EntryMethod,
    int? Year,
    string? EntryAddress,
    string? Source,
    string? Pages,
    string? Notes
);
