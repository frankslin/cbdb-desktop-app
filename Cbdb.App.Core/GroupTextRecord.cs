namespace Cbdb.App.Core;

public sealed record GroupTextRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? TextId,
    string? Title,
    string? Role,
    int? Year,
    string? Source,
    string? Pages,
    string? Notes
);
