namespace Cbdb.App.Core;

public sealed record PersonWritingItem(
    int TextId,
    string? TitleChn,
    string? Title,
    string? Role,
    int? Year,
    string? Nianhao,
    int? NianhaoYear,
    string? Range,
    string? Source,
    string? Pages,
    string? Notes
);
