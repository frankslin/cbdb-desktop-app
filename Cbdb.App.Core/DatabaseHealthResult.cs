namespace Cbdb.App.Core;

public sealed record DatabaseHealthResult(
    bool Success,
    string Message,
    int? PersonCount,
    int? AltNameCount,
    int? KinCount,
    int? AssocCount
);
