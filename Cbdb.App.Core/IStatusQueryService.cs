namespace Cbdb.App.Core;

public interface IStatusQueryService {
    Task<IReadOnlyList<StatusCodeOption>> GetStatusCodesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );

    Task<StatusPickerData> GetStatusPickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );

    Task<StatusQueryResult> QueryAsync(
        string sqlitePath,
        StatusQueryRequest request,
        CancellationToken cancellationToken = default
    );
}
