namespace Cbdb.App.Core;

public sealed record PersonEntryItem(
    int Sequence,
    string? EntryMethod,
    string? ExamRank,
    int? Year,
    string? Nianhao,
    int? NianhaoYear,
    string? Dynasty,
    string? Range,
    int? Age,
    string? Kinship,
    string? KinNameChn,
    string? KinName,
    string? Association,
    string? AssociateNameChn,
    string? AssociateName,
    string? InstitutionNameChn,
    string? InstitutionName,
    string? EntryAddressChn,
    string? EntryAddress,
    string? ParentalStatus,
    string? Source,
    string? Pages,
    string? Notes,
    string? PostingNotes
);
