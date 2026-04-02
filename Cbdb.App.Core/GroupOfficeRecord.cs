namespace Cbdb.App.Core;

public sealed record GroupOfficeRecord(
    int PersonId,
    string? NameChn,
    string? Name,
    int? PostingId,
    int? OfficeId,
    string? Office,
    string? AppointmentType,
    string? AssumeOffice,
    string? OfficeAddress,
    int? FirstYear,
    int? LastYear,
    string? Source,
    string? Pages,
    string? Notes
);
