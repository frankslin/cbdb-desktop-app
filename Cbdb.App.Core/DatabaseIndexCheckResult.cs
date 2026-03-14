namespace Cbdb.App.Core;

public sealed record DatabaseIndexCheckResult(
    IReadOnlyList<string> MissingIndexNames
) {
    public bool HasAllIndexes => MissingIndexNames.Count == 0;
}
