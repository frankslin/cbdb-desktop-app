namespace Cbdb.App.Core;

public interface IDatabaseIndexService {
    Task<DatabaseIndexCheckResult> CheckRecommendedIndexesAsync(string sqlitePath, CancellationToken cancellationToken = default);
    Task EnsureRecommendedIndexesAsync(
        string sqlitePath,
        IProgress<DatabaseIndexProgress>? progress = null,
        CancellationToken cancellationToken = default
    );
}
