namespace Cbdb.App.Core;

public interface IOfficeQueryService {
    Task<OfficePickerData> GetOfficePickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );

    Task<OfficeQueryResult> QueryAsync(
        string sqlitePath,
        OfficeQueryRequest request,
        CancellationToken cancellationToken = default
    );
}
