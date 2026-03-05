namespace Cbdb.App.Core;

public interface IDatabaseHealthService {
    Task<DatabaseHealthResult> CheckAsync(string sqlitePath, CancellationToken cancellationToken = default);
}
