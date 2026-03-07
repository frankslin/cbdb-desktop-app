namespace Cbdb.App.Core;

public sealed record PersonListItem(
    int PersonId,
    string? NameChn,
    string? NameRm,
    int? IndexYear,
    string? IndexAddress
);
