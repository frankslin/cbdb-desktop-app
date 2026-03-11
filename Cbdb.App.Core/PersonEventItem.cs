namespace Cbdb.App.Core;

public sealed record PersonEventItem(
    int Sequence,
    string? EventName,
    string? Role,
    int? Year,
    string? Nianhao,
    int? NianhaoYear,
    int? Month,
    bool? Intercalary,
    int? Day,
    string? Ganzhi,
    string? Range,
    string? AddressNameChn,
    string? AddressName,
    string? Source,
    string? Pages,
    string? EventText,
    string? Notes
);
