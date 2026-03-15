namespace Cbdb.App.Core;

public interface IEntryQueryService {
    Task<EntryPickerData> GetEntryPickerDataAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );

    Task<EntryQueryResult> QueryAsync(
        string sqlitePath,
        EntryQueryRequest request,
        CancellationToken cancellationToken = default
    );
}
