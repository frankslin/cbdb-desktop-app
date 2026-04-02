namespace Cbdb.App.Core;

public sealed record GroupAddressRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? AddressId,
    string? Address,
    string? AddressType,
    int? FirstYear,
    int? LastYear,
    string? Source,
    string? Notes,
    bool IsIndexAddress
);
