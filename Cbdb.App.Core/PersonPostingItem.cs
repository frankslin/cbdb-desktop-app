namespace Cbdb.App.Core;

public sealed record PersonPostingItem(
    int PostingId,
    IReadOnlyList<PersonPostingOfficeItem> Offices
);
