using System.Data;

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

    Task<IReadOnlyList<PersonAddressItem>> GetAddressesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonAltNameItem>> GetAltNamesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonWritingItem>> GetWritingsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonPostingItem>> GetPostingsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonEntryItem>> GetEntriesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonStatusItem>> GetStatusesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonPossessionItem>> GetPossessionsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonEventItem>> GetEventsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonKinshipItem>> GetKinshipsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonSourceItem>> GetSourcesAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PersonInstitutionItem>> GetInstitutionsAsync(
        string sqlitePath,
        int personId,
        CancellationToken cancellationToken = default
    );

    Task<DataTable> GetRelatedItemsAsync(
        string sqlitePath,
        int personId,
        PersonRelatedCategory category,
        int limit = 200,
        CancellationToken cancellationToken = default
    );
}
