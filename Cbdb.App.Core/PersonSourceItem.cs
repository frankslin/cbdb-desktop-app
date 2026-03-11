namespace Cbdb.App.Core;

public sealed record PersonSourceItem(
    string? TitleChn,
    string? Title,
    string? Pages,
    string? Notes,
    bool? MainSource,
    bool? SelfBio,
    string? Hyperlink
);
