namespace Cbdb.App.Core;

public interface IGroupPeopleService {
    Task<GroupPeopleQueryResult> QueryAsync(
        string sqlitePath,
        IReadOnlyList<int> personIds,
        GroupPeopleQueryOptions options,
        CancellationToken cancellationToken = default
    );
}
