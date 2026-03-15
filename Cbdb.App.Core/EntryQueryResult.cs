namespace Cbdb.App.Core;

public sealed record EntryQueryResult(
    IReadOnlyList<EntryQueryRecord> Records,
    IReadOnlyList<EntryQueryPerson> People
);
