namespace Cbdb.App.Core;

public sealed record PersonAddressItem(
    int Sequence,
    bool? Natal,
    string? AddressType,
    string? AddressNameChn,
    string? AddressName,
    int? FirstYear,
    string? FirstNianhao,
    int? FirstNianhaoYear,
    int? FirstMonth,
    bool? FirstIntercalary,
    int? FirstDay,
    string? FirstGanzhi,
    string? FirstRange,
    int? LastYear,
    string? LastNianhao,
    int? LastNianhaoYear,
    int? LastMonth,
    bool? LastIntercalary,
    int? LastDay,
    string? LastGanzhi,
    string? LastRange,
    string? Source,
    string? Pages,
    string? Notes
);
