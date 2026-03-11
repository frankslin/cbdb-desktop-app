namespace Cbdb.App.Core;

public sealed record PersonPostingItem(
    int PostingId,
    string? CreatedBy,
    string? CreatedDate,
    string? ModifiedBy,
    string? ModifiedDate,
    IReadOnlyList<PersonPostingOfficeItem> Offices
);
