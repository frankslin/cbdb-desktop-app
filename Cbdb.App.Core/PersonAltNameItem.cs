namespace Cbdb.App.Core;

public sealed record PersonAltNameItem(
    int Sequence,
    string? AltNameChn,
    string? AltName,
    string? NameType,
    string? Source,
    string? Pages,
    string? Notes
);
