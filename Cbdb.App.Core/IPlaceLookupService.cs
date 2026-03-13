namespace Cbdb.App.Core;

public interface IPlaceLookupService {
    Task<IReadOnlyList<PlaceOption>> GetPlacesAsync(
        string sqlitePath,
        CancellationToken cancellationToken = default
    );
}
