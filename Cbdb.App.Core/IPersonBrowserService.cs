namespace Cbdb.App.Core;

public interface IPersonBrowserService {
    Task<IReadOnlyList<PersonListItem>> SearchAsync(
        string sqlitePath,
        string? keyword,
        int limit = 200,
        int offset = 0,
        CancellationToken cancellationToken = default
    );

    Task<PersonDetail?> GetDetailAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonRelatedItem>> GetRelatedItemsAsync(
        string sqlitePath,
        int personId,
        PersonRelatedCategory category,
        int limit = 200,
        CancellationToken cancellationToken = default
    );
}
