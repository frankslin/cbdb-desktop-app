using System.Data;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Tests.TestDoubles;

internal sealed class FakePersonBrowserService : IPersonBrowserService {
    private static readonly IReadOnlyList<PersonListItem> People = new[] {
        new PersonListItem(1, "蘇軾", "Su Shi", 1057, "眉州"),
        new PersonListItem(2, "歐陽修", "Ouyang Xiu", 1007, "吉州")
    };

    private static readonly PersonDetail Detail = new(
        1,
        "蘇",
        "軾",
        "Su",
        "Shi",
        "Su",
        "Shi",
        "Su",
        "Shi",
        "Su Shi",
        "蘇軾",
        1057,
        "Birth year",
        "1 / 蘇洵 / Su Xun",
        "Song",
        "宋",
        1037,
        1101,
        "M",
        "Meizhou",
        "眉州",
        "Native place",
        1,
        1,
        0,
        0,
        0,
        0,
        0,
        0,
        1,
        0,
        1,
        1,
        new[] {
            new PersonFieldValue("c_name_proper", "Su Shi"),
            new PersonFieldValue("c_name_rm", "Su Shih"),
            new PersonFieldValue("c_birthyear", "1037"),
            new PersonFieldValue("c_by_nh_code", "qingli|慶曆"),
            new PersonFieldValue("c_by_nh_year", "7"),
            new PersonFieldValue("c_by_month", "12"),
            new PersonFieldValue("c_by_intercalary", "0"),
            new PersonFieldValue("c_by_day", "19"),
            new PersonFieldValue("c_by_day_gz", "renzi|壬子"),
            new PersonFieldValue("c_by_range", "exact|Exact"),
            new PersonFieldValue("c_deathyear", "1101"),
            new PersonFieldValue("c_dy_nh_code", "jianzhongjingguo|建中靖國"),
            new PersonFieldValue("c_dy_nh_year", "1"),
            new PersonFieldValue("c_dy_month", "7"),
            new PersonFieldValue("c_dy_intercalary", "0"),
            new PersonFieldValue("c_dy_day", "28"),
            new PersonFieldValue("c_dy_day_gz", "guiyou|癸酉"),
            new PersonFieldValue("c_dy_range", "exact|Exact"),
            new PersonFieldValue("c_death_age", "64"),
            new PersonFieldValue("c_death_age_range", "exact|Exact"),
            new PersonFieldValue("c_fl_earliest_year", "1057"),
            new PersonFieldValue("c_fl_ey_nh_code", "jiayou|嘉祐"),
            new PersonFieldValue("c_fl_ey_nh_year", "2"),
            new PersonFieldValue("c_fl_ey_notes", "Earliest active record"),
            new PersonFieldValue("c_fl_latest_year", "1101"),
            new PersonFieldValue("c_fl_ly_nh_code", "jianzhongjingguo|建中靖國"),
            new PersonFieldValue("c_fl_ly_nh_year", "1"),
            new PersonFieldValue("c_fl_ly_notes", "Latest active record"),
            new PersonFieldValue("c_choronym_code", "shu|蜀人"),
            new PersonFieldValue("c_household_status_code", "commoner|Commoner"),
            new PersonFieldValue("c_ethnicity_code", "han|Han"),
            new PersonFieldValue("c_tribe", "眉山蘇氏"),
            new PersonFieldValue("c_notes", "Test note for headless UI."),
            new PersonFieldValue("c_created_by", "fixture"),
            new PersonFieldValue("c_created_date", "2026-03-01"),
            new PersonFieldValue("c_modified_by", "fixture"),
            new PersonFieldValue("c_modified_date", "2026-03-02")
        }
    );

    private static readonly IReadOnlyList<PersonAddressItem> Addresses = new[] {
        new PersonAddressItem(
            1,
            true,
            "Native place",
            "眉州",
            "Meizhou",
            1037,
            "慶曆",
            7,
            12,
            false,
            19,
            "壬子",
            "Exact",
            1101,
            "建中靖國",
            1,
            7,
            false,
            28,
            "癸酉",
            "Exact",
            "宋史",
            "12-14",
            "Sample address note"
        )
    };

    private static readonly IReadOnlyList<PersonAltNameItem> AltNames = new[] {
        new PersonAltNameItem(1, "子瞻", "Zizhan", "Courtesy name", "宋史", "18", "Sample alt name note")
    };

    private static readonly IReadOnlyList<PersonWritingItem> Writings = new[] {
        new PersonWritingItem(101, "東坡集", "Collected Works of Dongpo", "Author", 1090, "元祐", 5, "Exact", "宋史", "25", "Sample writing note")
    };

    private static readonly IReadOnlyList<PersonSourceItem> Sources = new[] {
        new PersonSourceItem("宋史", "History of Song", "12-14", "Sample source note", true, false, "https://example.invalid/source")
    };

    private static readonly IReadOnlyList<PersonInstitutionItem> Institutions = new[] {
        new PersonInstitutionItem("翰林院", "Hanlin Academy", "Scholar", 1070, "熙寧", 3, "Exact", 1071, "熙寧", 4, "Exact", "開封", "Kaifeng", "Capital", "宋史", "32", "Sample institution note", 114.3, 34.8)
    };

    public Task<IReadOnlyList<PersonListItem>> SearchAsync(string sqlitePath, string? keyword, int limit = 200, int offset = 0, CancellationToken cancellationToken = default) {
        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? People
            : People.Where(item =>
                    (item.NameChn?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (item.NameRm?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();

        return Task.FromResult<IReadOnlyList<PersonListItem>>(filtered.Skip(offset).Take(limit).ToArray());
    }

    public Task<PersonDetail?> GetDetailAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult<PersonDetail?>(personId == Detail.PersonId ? Detail : null);
    }

    public Task<IReadOnlyList<PersonAddressItem>> GetAddressesAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Addresses : Array.Empty<PersonAddressItem>());
    }

    public Task<IReadOnlyList<PersonAltNameItem>> GetAltNamesAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? AltNames : Array.Empty<PersonAltNameItem>());
    }

    public Task<IReadOnlyList<PersonWritingItem>> GetWritingsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Writings : Array.Empty<PersonWritingItem>());
    }

    public Task<IReadOnlyList<PersonSourceItem>> GetSourcesAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Sources : Array.Empty<PersonSourceItem>());
    }

    public Task<IReadOnlyList<PersonInstitutionItem>> GetInstitutionsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Institutions : Array.Empty<PersonInstitutionItem>());
    }

    public Task<DataTable> GetRelatedItemsAsync(string sqlitePath, int personId, PersonRelatedCategory category, int limit = 200, CancellationToken cancellationToken = default) {
        return Task.FromResult(new DataTable());
    }
}
