namespace Cbdb.App.Core;

public sealed record PersonListItem(
    int PersonId,
    string? Name,
    string? NameChn,
    int? IndexYear,
    string? Dynasty,
    string? DynastyChn
);
