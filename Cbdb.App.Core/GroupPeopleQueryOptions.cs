namespace Cbdb.App.Core;

public sealed record GroupPeopleQueryOptions(
    bool IncludeStatus,
    bool IncludeOffice,
    bool IncludeEntry,
    bool IncludeTexts,
    bool IncludeAddresses,
    GroupPeopleAddressMode AddressMode
);
