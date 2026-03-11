namespace Cbdb.App.Core;

public sealed record PersonStatusItem(
    int Sequence,
    string? Status,
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
