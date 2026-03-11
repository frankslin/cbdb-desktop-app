namespace Cbdb.App.Core;

public sealed record PersonKinshipItem(
    int KinPersonId,
    string? Kinship,
    string? KinNameChn,
    string? KinName,
    int? UpStep,
    int? DownStep,
    int? MarriageStep,
    int? CollateralStep,
    string? Source,
    string? Pages,
    string? Notes
);
