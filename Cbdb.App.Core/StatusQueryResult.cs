namespace Cbdb.App.Core;

public sealed record StatusQueryResult(
    IReadOnlyList<StatusQueryRecord> Records,
    IReadOnlyList<StatusQueryPerson> People
);
