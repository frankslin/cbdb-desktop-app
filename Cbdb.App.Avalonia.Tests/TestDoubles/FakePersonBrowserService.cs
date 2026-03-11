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
        1,
        0,
        0,
        1,
        1,
        1,
        1,
        1,
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

    private static readonly IReadOnlyList<PersonPostingItem> Postings = new[] {
        new PersonPostingItem(
            2305,
            "fixture",
            "2026-03-01",
            "fixture",
            "2026-03-02",
            new[] {
                new PersonPostingOfficeItem(
                    8001,
                    1,
                    "轉運司判官",
                    "Zhuan Yun Si Pan Guan",
                    "差遣 / Appointment",
                    "就任 / Assumed Office",
                    "地方官 / Local Office",
                    1085,
                    "元豐",
                    8,
                    "Exact",
                    3,
                    false,
                    12,
                    "甲子",
                    1086,
                    "元祐",
                    1,
                    "Exact",
                    4,
                    false,
                    15,
                    "乙丑",
                    "宋 / Song",
                    "宋史",
                    "88-89",
                    "Sample posting note",
                    new[] {
                        new PersonPostingAddressItem(9001, "開封", "Kaifeng"),
                        new PersonPostingAddressItem(9002, "常州", "Changzhou")
                    }
                )
            }
        )
    };

    private static readonly IReadOnlyList<PersonEntryItem> Entries = new[] {
        new PersonEntryItem(
            1,
            "進士 / Presented Scholar",
            "第一甲",
            1057,
            "嘉祐",
            2,
            "宋 / Song",
            "Exact",
            21,
            "父 / Father",
            "蘇洵",
            "Su Xun",
            "薦舉 / Recommendation",
            "歐陽修",
            "Ouyang Xiu",
            "國子監",
            "Directorate of Education",
            "開封",
            "Kaifeng",
            "官戶 / Official household",
            "宋史",
            "40-41",
            "Sample entry note",
            "Sample posting note"
        )
    };

    private static readonly IReadOnlyList<PersonEventItem> Events = new[] {
        new PersonEventItem(
            1,
            "赴任 / Departure for Office",
            "主角 / Principal",
            1061,
            "嘉祐",
            6,
            3,
            false,
            12,
            "辛卯",
            "Exact",
            "鳳翔",
            "Fengxiang",
            "宋史",
            "60-61",
            "Left for office in Fengxiang.",
            "Sample event note"
        )
    };

    private static readonly IReadOnlyList<PersonKinshipItem> Kinships = new[] {
        new PersonKinshipItem(3, "兄 / Elder Brother", "蘇轍", "Su Zhe", 0, 0, 0, 1, "宋史", "20", "Sample kinship note")
    };

    private static readonly IReadOnlyList<PersonStatusItem> Statuses = new[] {
        new PersonStatusItem(1, "士大夫 / Literati Official", 1060, "嘉祐", 5, "Exact", 1100, "元符", 3, "Exact", "宋史", "55", "Sample status note")
    };

    private static readonly IReadOnlyList<PersonPossessionItem> Possessions = new[] {
        new PersonPossessionItem(501, 1, "田產 / Landholding", "受賜 / Granted", "120", "畝 / mu", 1085, "元豐", 8, "Exact", "常州", "Changzhou", "宋史", "78", "Sample possession note")
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

    public Task<IReadOnlyList<PersonPostingItem>> GetPostingsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Postings : Array.Empty<PersonPostingItem>());
    }

    public Task<IReadOnlyList<PersonEntryItem>> GetEntriesAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Entries : Array.Empty<PersonEntryItem>());
    }

    public Task<IReadOnlyList<PersonEventItem>> GetEventsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Events : Array.Empty<PersonEventItem>());
    }

    public Task<IReadOnlyList<PersonKinshipItem>> GetKinshipsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Kinships : Array.Empty<PersonKinshipItem>());
    }

    public Task<IReadOnlyList<PersonStatusItem>> GetStatusesAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Statuses : Array.Empty<PersonStatusItem>());
    }

    public Task<IReadOnlyList<PersonPossessionItem>> GetPossessionsAsync(string sqlitePath, int personId, CancellationToken cancellationToken = default) {
        return Task.FromResult(personId == Detail.PersonId ? Possessions : Array.Empty<PersonPossessionItem>());
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
