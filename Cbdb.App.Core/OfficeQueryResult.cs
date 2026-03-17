namespace Cbdb.App.Core;

public sealed record OfficeQueryResult(
    IReadOnlyList<OfficeQueryRecord> Records,
    IReadOnlyList<OfficeQueryPerson> People
);
