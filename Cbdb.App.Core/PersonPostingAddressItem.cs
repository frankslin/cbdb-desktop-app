namespace Cbdb.App.Core;

public sealed record PersonPostingAddressItem(
    int AddressId,
    string? AddressNameChn,
    string? AddressName,
    string? CreatedBy,
    string? CreatedDate,
    string? ModifiedBy,
    string? ModifiedDate
);
