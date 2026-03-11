namespace Cbdb.App.Core;

public sealed record PersonPossessionItem(
    int RecordId,
    int? Sequence,
    string? Possession,
    string? PossessionAction,
    string? Quantity,
    string? Measure,
    int? Year,
    string? Nianhao,
    int? NianhaoYear,
    string? Range,
    string? AddressNameChn,
    string? AddressName,
    string? Source,
    string? Pages,
    string? Notes
);
