namespace Cbdb.App.Core;

public sealed record DatabaseIndexProgress(
    int CompletedSteps,
    int TotalSteps,
    string IndexName
);
