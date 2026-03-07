using System.Data;
using System.IO;
using System.Windows;
using Cbdb.App.Core;
using Microsoft.Data.Sqlite;

namespace Cbdb.App.Desktop.Modules;

public partial class QueryModuleWindow : Window {
    private readonly QueryModuleKind _module;
    private readonly ILocalizationService _localization;
    private readonly string _sqlitePath;

    public QueryModuleWindow(QueryModuleKind module, ILocalizationService localization, string sqlitePath) {
        _module = module;
        _localization = localization;
        _sqlitePath = NormalizeSqlitePath(sqlitePath);

        InitializeComponent();

        _localization.LanguageChanged += (_, _) => ApplyLocalization();

        BtnRunQuery.Click += BtnRunQuery_Click;

        SetupModule();
        ApplyLocalization();
        LoadPlaceholderData();
    }

    private void SetupModule() {
        BtnSelectPrimary.Tag = "qm.select_filter";
        BtnImportList.Tag = "qm.import_list";
        TabMain.Tag = "qm.tab_main";
        TabSecondary.Tag = "qm.tab_secondary";
        TabPeople.Tag = "qm.tab_people";
        TabAggregate.Tag = "qm.tab_aggregate";
        AdvancedPanel.Visibility = Visibility.Collapsed;
        TabSecondary.Visibility = Visibility.Collapsed;
        TabPeople.Visibility = Visibility.Visible;
        TabAggregate.Visibility = Visibility.Collapsed;

        switch (_module) {
            case QueryModuleKind.Entry:
                BtnSelectPrimary.Tag = "qm.select_entry";
                TabMain.Tag = "qm.tab_entry";
                TabPeople.Tag = "qm.tab_people";
                break;
            case QueryModuleKind.Associations:
                BtnSelectPrimary.Tag = "qm.select_association";
                TabMain.Tag = "qm.tab_associations";
                TabPeople.Tag = "qm.tab_people";
                break;
            case QueryModuleKind.Office:
                BtnSelectPrimary.Tag = "qm.select_office";
                TabMain.Tag = "qm.tab_office_postings";
                TabPeople.Tag = "qm.tab_people_office";
                break;
            case QueryModuleKind.Kinship:
                BtnSelectPrimary.Tag = "qm.select_person";
                BtnImportList.Tag = "qm.import_people";
                TabMain.Tag = "qm.tab_kinship_network";
                TabSecondary.Visibility = Visibility.Visible;
                TabSecondary.Tag = "qm.tab_ego_kinship";
                TabPeople.Visibility = Visibility.Collapsed;
                AdvancedPanel.Visibility = Visibility.Visible;
                ChkIncludeKinship.Visibility = Visibility.Collapsed;
                break;
            case QueryModuleKind.Networks:
                BtnSelectPrimary.Tag = "qm.select_person";
                BtnImportList.Tag = "qm.import_people";
                TabMain.Tag = "qm.tab_relation_types";
                TabSecondary.Visibility = Visibility.Visible;
                TabSecondary.Tag = "qm.tab_network_relations";
                TabPeople.Tag = "qm.tab_people_network";
                TabAggregate.Visibility = Visibility.Visible;
                TabAggregate.Tag = "qm.tab_aggregate_relations";
                AdvancedPanel.Visibility = Visibility.Visible;
                break;
            case QueryModuleKind.AssociationPairs:
                BtnSelectPrimary.Tag = "qm.select_first_person";
                BtnImportList.Tag = "qm.import_people";
                TabMain.Tag = "qm.tab_associations";
                TabPeople.Tag = "qm.tab_people";
                AdvancedPanel.Visibility = Visibility.Visible;
                break;
            case QueryModuleKind.Place:
                BtnSelectPrimary.Tag = "qm.select_place";
                BtnImportList.Tag = "qm.import_places";
                TabMain.Tag = "qm.tab_place_relations";
                TabSecondary.Visibility = Visibility.Visible;
                TabSecondary.Tag = "qm.tab_aggregate_people_places";
                TabPeople.Tag = "qm.tab_people";
                break;
            case QueryModuleKind.Status:
                BtnSelectPrimary.Tag = "qm.select_status";
                TabMain.Tag = "qm.tab_status";
                TabPeople.Tag = "qm.tab_people";
                break;
            case QueryModuleKind.Texts:
                BtnSelectPrimary.Tag = "qm.select_text_category";
                TabMain.Tag = "qm.tab_texts_roles";
                TabPeople.Tag = "qm.tab_people";
                break;
        }
    }

    private void ApplyLocalization() {
        Title = T(GetWindowTitleKey());

        BtnSelectPrimary.Content = T(BtnSelectPrimary.Tag?.ToString() ?? "qm.select_filter");
        BtnImportList.Content = T(BtnImportList.Tag?.ToString() ?? "qm.import_list");
        BtnSaveList.Content = T("qm.save_list");

        LblType.Text = T("qm.type");
        LblYears.Text = T("qm.index_years");
        LblDynasties.Text = T("qm.dynasties");
        LblDateMode.Text = T("qm.date_mode");

        LblFrom.Text = T("qm.from");
        LblTo.Text = T("qm.to");
        LblFromDyn.Text = T("qm.from");
        LblToDyn.Text = T("qm.to");

        RdoNoDates.Content = T("qm.no_dates");
        RdoUseIndexYears.Content = T("qm.use_index_years");
        RdoUseDynasties.Content = T("qm.use_dynasties");
        RdoNoDates.IsChecked = true;

        ChkIncludeSubunits.Content = T("qm.include_subunits");
        ChkUseXy.Content = T("qm.use_xy");

        BtnSelectPlace.Content = T("qm.select_place");
        BtnImportPlaces.Content = T("qm.import_places");
        BtnAllPlaces.Content = T("qm.all_places");


        ChkIncludeKinship.Content = T("qm.include_kinship_relations");
        LblMaxNodeDist.Text = T("qm.max_node_distance");
        LblMaxLoop.Text = T("qm.max_loop");
        LblMaxAncestor.Text = T("qm.max_ancestor");

        TabMain.Header = T(TabMain.Tag?.ToString() ?? "qm.tab_main");
        TabSecondary.Header = T(TabSecondary.Tag?.ToString() ?? "qm.tab_secondary");
        TabPeople.Header = T(TabPeople.Tag?.ToString() ?? "qm.tab_people");
        TabAggregate.Header = T(TabAggregate.Tag?.ToString() ?? "qm.tab_aggregate");

        BtnRunQuery.Content = T("qm.run_query");
        BtnSavePersonIds.Content = T("qm.save_person_ids");
        BtnSaveGIS.Content = T("qm.save_gis");
        LblEncoding.Text = T("qm.encoding");
        BtnHelp.Content = T("qm.help");
        BtnClose.Content = T("button.exit");

        TxtPrimaryA.ToolTip = T("qm.prototype_hint");
        TxtTypeA.ToolTip = T("qm.prototype_hint");
        TxtPlaceA.ToolTip = T("qm.prototype_hint");
    }

    private async void BtnRunQuery_Click(object sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(_sqlitePath) || !File.Exists(_sqlitePath)) {
            MessageBox.Show(this, $"SQLite file not found: {_sqlitePath}", "CBDB", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try {
            BtnRunQuery.IsEnabled = false;
            var request = BuildRequest();

            var main = await Task.Run(() => ExecuteQuery(request.MainSql, request));
            GridMain.ItemsSource = main.DefaultView;

            if (!string.IsNullOrWhiteSpace(request.SecondarySql) && TabSecondary.Visibility == Visibility.Visible) {
                var secondary = await Task.Run(() => ExecuteQuery(request.SecondarySql!, request));
                GridSecondary.ItemsSource = secondary.DefaultView;
            }

            if (!string.IsNullOrWhiteSpace(request.PeopleSql) && TabPeople.Visibility == Visibility.Visible) {
                var people = await Task.Run(() => ExecuteQuery(request.PeopleSql!, request));
                GridPeople.ItemsSource = people.DefaultView;
            }

            if (!string.IsNullOrWhiteSpace(request.AggregateSql) && TabAggregate.Visibility == Visibility.Visible) {
                var aggregate = await Task.Run(() => ExecuteQuery(request.AggregateSql!, request));
                GridAggregate.ItemsSource = aggregate.DefaultView;
            }
        } catch (Exception ex) {
            MessageBox.Show(this, ex.Message, "CBDB Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
        } finally {
            BtnRunQuery.IsEnabled = true;
        }
    }

    private QueryRequest BuildRequest() {
        var keyword = NormalizeSqliteText(TxtPrimaryA.Text);
        var keywordB = NormalizeSqliteText(TxtPrimaryB.Text);

        var useYear = RdoUseIndexYears.IsChecked == true;
        var yearFrom = ParseIntOrDefault(TxtYearFrom.Text, -200);
        var yearTo = ParseIntOrDefault(TxtYearTo.Text, 1911);

        var useDynasty = RdoUseDynasties.IsChecked == true;
        var dynastyFrom = ParseIntOrDefault(TxtDynFrom.Text, 0);
        var dynastyTo = ParseIntOrDefault(TxtDynTo.Text, 9999);

        var request = new QueryRequest {
            Keyword = keyword,
            KeywordB = keywordB,
            UseYear = useYear,
            YearFrom = Math.Min(yearFrom, yearTo),
            YearTo = Math.Max(yearFrom, yearTo),
            UseDynasty = useDynasty,
            DynastyFrom = Math.Min(dynastyFrom, dynastyTo),
            DynastyTo = Math.Max(dynastyFrom, dynastyTo)
        };

        var personFilter = BuildPersonFilter("b");

        switch (_module) {
            case QueryModuleKind.Entry:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       e.c_entry_code AS EntryCode, e.c_firstyear AS FirstYear, e.c_lastyear AS LastYear
FROM ENTRY_DATA e
JOIN BIOG_MAIN b ON b.c_personid = e.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = "SELECT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;

            case QueryModuleKind.Office:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       p.c_office_id AS OfficeId, p.c_firstyear AS FirstYear, p.c_lastyear AS LastYear
FROM POSTED_TO_OFFICE_DATA p
JOIN BIOG_MAIN b ON b.c_personid = p.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = "SELECT DISTINCT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;

            case QueryModuleKind.Kinship:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       k.c_kin_name AS KinName, k.c_kin_code AS KinCode, k.c_kin_id AS KinId
FROM KIN_DATA k
JOIN BIOG_MAIN b ON b.c_personid = k.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.SecondarySql = @"
SELECT c_kin_code AS KinCode, COUNT(*) AS RelationCount
FROM KIN_DATA
GROUP BY c_kin_code
ORDER BY RelationCount DESC
LIMIT 200;";
                break;

            case QueryModuleKind.Associations:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       a.c_assoc_desc AS AssocDesc, a.c_assoc_code AS AssocCode, a.c_assoc_personid AS AssocPersonId
FROM ASSOC_DATA a
JOIN BIOG_MAIN b ON b.c_personid = a.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = "SELECT DISTINCT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;

            case QueryModuleKind.Networks:
                request.MainSql = $@"
SELECT b.c_personid AS PersonAId, b.c_name AS PersonA, b2.c_personid AS PersonBId, b2.c_name AS PersonB,
       a.c_assoc_desc AS Relation, a.c_assoc_code AS RelationType
FROM ASSOC_DATA a
JOIN BIOG_MAIN b ON b.c_personid = a.c_personid
LEFT JOIN BIOG_MAIN b2 ON b2.c_personid = a.c_assoc_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 3000;";
                request.SecondarySql = @"
SELECT c_assoc_code AS RelationType, COUNT(*) AS LinkCount
FROM ASSOC_DATA
GROUP BY c_assoc_code
ORDER BY LinkCount DESC
LIMIT 200;";
                request.PeopleSql = $@"
SELECT DISTINCT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear
FROM ASSOC_DATA a
JOIN BIOG_MAIN b ON b.c_personid = a.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.AggregateSql = @"
SELECT c_assoc_code AS RelationType, COUNT(DISTINCT c_personid) AS PersonCount
FROM ASSOC_DATA
GROUP BY c_assoc_code
ORDER BY PersonCount DESC
LIMIT 200;";
                break;

            case QueryModuleKind.AssociationPairs:
                request.MainSql = $@"
SELECT b.c_personid AS PersonAId, b.c_name AS PersonA, b2.c_personid AS PersonBId, b2.c_name AS PersonB,
       a.c_assoc_desc AS AssocDesc, a.c_assoc_code AS AssocCode
FROM ASSOC_DATA a
JOIN BIOG_MAIN b ON b.c_personid = a.c_personid
LEFT JOIN BIOG_MAIN b2 ON b2.c_personid = a.c_assoc_personid
WHERE {personFilter}
  AND ($kw2 IS NULL OR b2.c_name LIKE $kw2 OR b2.c_name_chn LIKE $kw2)
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = $@"
SELECT DISTINCT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear
FROM ASSOC_DATA a
JOIN BIOG_MAIN b ON b.c_personid = a.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                break;

            case QueryModuleKind.Place:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       ba.c_addr_id AS AddressId, ba.c_addr_type AS AddressType, ba.c_firstyear AS FirstYear, ba.c_lastyear AS LastYear
FROM BIOG_ADDR_DATA ba
JOIN BIOG_MAIN b ON b.c_personid = ba.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.SecondarySql = @"
SELECT c_addr_type AS AddressType, COUNT(*) AS RowCount
FROM BIOG_ADDR_DATA
GROUP BY c_addr_type
ORDER BY RowCount DESC
LIMIT 200;";
                request.PeopleSql = "SELECT DISTINCT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;

            case QueryModuleKind.Status:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       s.c_status_code AS StatusCode, s.c_firstyear AS FirstYear, s.c_lastyear AS LastYear
FROM STATUS_DATA s
JOIN BIOG_MAIN b ON b.c_personid = s.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = "SELECT DISTINCT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;

            case QueryModuleKind.Texts:
                request.MainSql = $@"
SELECT b.c_personid, b.c_name AS Name, b.c_name_chn AS NameChn, b.c_index_year AS IndexYear,
       t.c_textid AS TextId, t.c_role_id AS RoleId, t.c_year AS TextYear
FROM BIOG_TEXT_DATA t
JOIN BIOG_MAIN b ON b.c_personid = t.c_personid
WHERE {personFilter}
ORDER BY b.c_personid
LIMIT 2000;";
                request.PeopleSql = "SELECT DISTINCT c_personid, Name, NameChn, IndexYear FROM (" + request.MainSql.Trim().TrimEnd(';') + ");";
                break;
        }

        return request;
    }

    private string BuildPersonFilter(string alias) {
        return $@"
($kw IS NULL
    OR {alias}.c_name LIKE $kw
    OR {alias}.c_name_chn LIKE $kw
    OR EXISTS (
        SELECT 1 FROM ALTNAME_DATA an
        WHERE an.c_personid = {alias}.c_personid
          AND (an.c_alt_name LIKE $kw OR an.c_alt_name_chn LIKE $kw)
    )
)
AND ($useYear = 0 OR ({alias}.c_index_year BETWEEN $yearFrom AND $yearTo))
AND ($useDynasty = 0 OR ({alias}.c_dy BETWEEN $dyFrom AND $dyTo))";
    }

    private DataTable ExecuteQuery(string sql, QueryRequest request) {
        var table = new DataTable();

        var builder = new SqliteConnectionStringBuilder {
            DataSource = _sqlitePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$kw", request.Keyword is null ? DBNull.Value : $"%{request.Keyword}%");
        command.Parameters.AddWithValue("$kw2", request.KeywordB is null ? DBNull.Value : $"%{request.KeywordB}%");
        command.Parameters.AddWithValue("$useYear", request.UseYear ? 1 : 0);
        command.Parameters.AddWithValue("$yearFrom", request.YearFrom);
        command.Parameters.AddWithValue("$yearTo", request.YearTo);
        command.Parameters.AddWithValue("$useDynasty", request.UseDynasty ? 1 : 0);
        command.Parameters.AddWithValue("$dyFrom", request.DynastyFrom);
        command.Parameters.AddWithValue("$dyTo", request.DynastyTo);

        using var reader = command.ExecuteReader();
        table.Load(reader);
        return table;
    }

    private void LoadPlaceholderData() {
        var columns = GetColumns();
        var table = new DataTable();
        foreach (var column in columns) {
            table.Columns.Add(column);
        }

        if (columns.Count > 0) {
            var row = table.NewRow();
            row[0] = string.Empty;
            table.Rows.Add(row);
        }

        GridMain.ItemsSource = table.DefaultView;
        GridSecondary.ItemsSource = table.DefaultView;
        GridPeople.ItemsSource = table.DefaultView;
        GridAggregate.ItemsSource = table.DefaultView;
    }

    private List<string> GetColumns() {
        return _module switch {
            QueryModuleKind.Entry => new List<string> { "Name", "NameChn", "Index Year", "Entry Type", "Entry Code", "Index Place" },
            QueryModuleKind.Associations => new List<string> { "Name", "NameChn", "Index Year", "Associate", "Assoc Type", "Assoc. Index" },
            QueryModuleKind.Office => new List<string> { "Name", "NameChn", "Index Year", "Office", "Address Type", "Place" },
            QueryModuleKind.Kinship => new List<string> { "Name", "NameChn", "Sex", "Index Year", "Kin Name", "Kin Index" },
            QueryModuleKind.Networks => new List<string> { "Person A", "Person B", "Relation", "Type", "First Year", "Last Year" },
            QueryModuleKind.AssociationPairs => new List<string> { "Name", "Linked to", "Relation Type", "Kin/Non", "Link" },
            QueryModuleKind.Place => new List<string> { "Name", "NameChn", "Index Year", "Place Name", "Address Type", "Category" },
            QueryModuleKind.Status => new List<string> { "Name", "NameChn", "Index Year", "Status", "Status Code", "First Year" },
            QueryModuleKind.Texts => new List<string> { "Name", "NameChn", "Index Year", "Role", "Title", "Text Year", "Category" },
            _ => new List<string> { "Name", "Value" }
        };
    }

    private string GetWindowTitleKey() {
        return _module switch {
            QueryModuleKind.Entry => "module.entry",
            QueryModuleKind.Office => "module.office",
            QueryModuleKind.Kinship => "module.kinship",
            QueryModuleKind.Associations => "module.associations",
            QueryModuleKind.Networks => "module.networks",
            QueryModuleKind.AssociationPairs => "module.association_pairs",
            QueryModuleKind.Place => "module.place",
            QueryModuleKind.Status => "module.status",
            QueryModuleKind.Texts => "module.texts",
            _ => "window.title"
        };
    }

    private static string NormalizeSqlitePath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        return path.Trim().Trim('"');
    }

    private static string? NormalizeSqliteText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim().Replace("\"", string.Empty);
    }

    private static int ParseIntOrDefault(string? value, int fallback) {
        return int.TryParse(value?.Trim(), out var parsed) ? parsed : fallback;
    }

    private string T(string key) => _localization.Get(key);

    private void BtnClose_Click(object sender, RoutedEventArgs e) {
        Close();
    }

    private sealed class QueryRequest {
        public string MainSql { get; set; } = string.Empty;
        public string? SecondarySql { get; set; }
        public string? PeopleSql { get; set; }
        public string? AggregateSql { get; set; }

        public string? Keyword { get; set; }
        public string? KeywordB { get; set; }

        public bool UseYear { get; set; }
        public int YearFrom { get; set; }
        public int YearTo { get; set; }

        public bool UseDynasty { get; set; }
        public int DynastyFrom { get; set; }
        public int DynastyTo { get; set; }
    }
}


