namespace Cbdb.App.Core;

public interface IDynastyLookupService {
    Task<IReadOnlyList<DynastyOption>> GetDynastiesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );
}
