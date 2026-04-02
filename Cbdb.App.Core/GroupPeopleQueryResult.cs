namespace Cbdb.App.Core;

public sealed record GroupPeopleQueryResult(
    IReadOnlyList<GroupStatusRecord> StatusRecords,
    IReadOnlyList<GroupOfficeRecord> OfficeRecords,
    IReadOnlyList<GroupEntryRecord> EntryRecords,
    IReadOnlyList<GroupTextRecord> TextRecords,
    IReadOnlyList<GroupAddressRecord> AddressRecords
);
