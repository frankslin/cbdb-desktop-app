using System.Windows;
using System.Windows.Controls;
using Cbdb.App.Core;
using Cbdb.App.Desktop.Localization;

namespace Cbdb.App.Desktop.Browser;

public partial class PersonBrowserWindow {
    private static readonly HashSet<string> StructuredBasicFieldNames = new(StringComparer.OrdinalIgnoreCase) {
        "c_birthyear", "c_by_nh_code", "c_by_nh_year", "c_by_month", "c_by_intercalary", "c_by_day", "c_by_day_gz", "c_by_range",
        "c_deathyear", "c_dy_nh_code", "c_dy_nh_year", "c_dy_month", "c_dy_intercalary", "c_dy_day", "c_dy_day_gz", "c_dy_range",
        "c_death_age", "c_death_age_range",
        "c_fl_earliest_year", "c_fl_ey_nh_code", "c_fl_ey_nh_year", "c_fl_ey_notes",
        "c_fl_latest_year", "c_fl_ly_nh_code", "c_fl_ly_nh_year", "c_fl_ly_notes",
        "c_choronym_code", "c_household_status_code", "c_ethnicity_code", "c_tribe",
        "c_created_by", "c_created_date", "c_modified_by", "c_modified_date",
        "c_self_bio"
    };

    private readonly Dictionary<string, GroupBox> _basicGroups = new();
    private readonly Dictionary<string, TextBlock> _basicLabels = new();
    private readonly Dictionary<string, TextBox> _basicValues = new();
    private readonly Dictionary<string, CheckBox> _basicChecks = new();
    private ItemsControl? _basicFieldsItemsControl;
    private TextBlock? _basicFieldsHeader;

    private void InitializeBasicInfoLayout() {
        TabBirthDeath.Content = BuildBasicInfoContent();
        if (_basicFieldsItemsControl is not null) {
            _basicFieldsItemsControl.ItemsSource = _detailFields;
        }
    }

    private UIElement BuildBasicInfoContent() {
        var root = new StackPanel { Margin = new Thickness(8) };
        root.Children.Add(CreateBirthGroup());
        root.Children.Add(CreateDeathGroup());
        root.Children.Add(CreateFirstLastGroup());
        root.Children.Add(CreateIdentityGroup());
        root.Children.Add(CreateAuditGroup());

        _basicFieldsHeader = new TextBlock {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 10)
        };
        root.Children.Add(_basicFieldsHeader);

        _basicFieldsItemsControl = new ItemsControl();
        _basicFieldsItemsControl.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
        _basicFieldsItemsControl.ItemTemplate = BuildOtherFieldTemplate();
        root.Children.Add(_basicFieldsItemsControl);

        return new ScrollViewer {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = root
        };
    }

    private DataTemplate BuildOtherFieldTemplate() {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(FrameworkElement.WidthProperty, 330.0);
        border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 10));
        border.SetValue(Border.PaddingProperty, new Thickness(8));
        border.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.LightGray);
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.WhiteSmoke);

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        border.AppendChild(stack);

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        label.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.DimGray);
        label.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(PersonFieldValue.FieldName)));
        stack.AppendChild(label);

        var value = new FrameworkElementFactory(typeof(TextBox));
        value.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 6, 0, 0));
        value.SetValue(FrameworkElement.MinHeightProperty, 26.0);
        value.SetValue(TextBox.IsReadOnlyProperty, true);
        value.SetValue(TextBox.TextWrappingProperty, TextWrapping.NoWrap);
        value.SetValue(TextBox.AcceptsReturnProperty, true);
        value.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        value.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        value.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(nameof(PersonFieldValue.Value)));
        stack.AppendChild(value);

        return new DataTemplate { VisualTree = border };
    }

    private GroupBox CreateBirthGroup() {
        var group = CreateGroup("birth_group");
        group.Content = CreateLabeledGrid(new (string labelKey, FrameworkElement value)[] {
            ("birth_gregorian", CreateValueBox("birth_year")),
            ("birth_nianhao", CreateWideValueBox("birth_nianhao")),
            ("birth_nianhao_year", CreateValueBox("birth_nianhao_year")),
            ("birth_month", CreateValueBox("birth_month")),
            ("birth_intercalary", CreateReadOnlyCheckBox("birth_intercalary")),
            ("birth_day", CreateValueBox("birth_day")),
            ("birth_ganzhi", CreateWideValueBox("birth_ganzhi")),
            ("birth_range", CreateWideValueBox("birth_range"))
        }, 3);
        return group;
    }

    private GroupBox CreateDeathGroup() {
        var group = CreateGroup("death_group");
        group.Content = CreateLabeledGrid(new (string labelKey, FrameworkElement value)[] {
            ("death_gregorian", CreateValueBox("death_year")),
            ("death_nianhao", CreateWideValueBox("death_nianhao")),
            ("death_nianhao_year", CreateValueBox("death_nianhao_year")),
            ("death_month", CreateValueBox("death_month")),
            ("death_intercalary", CreateReadOnlyCheckBox("death_intercalary")),
            ("death_day", CreateValueBox("death_day")),
            ("death_ganzhi", CreateWideValueBox("death_ganzhi")),
            ("death_range", CreateWideValueBox("death_range")),
            ("death_age", CreateValueBox("death_age")),
            ("death_age_range", CreateWideValueBox("death_age_range"))
        }, 3);
        return group;
    }

    private GroupBox CreateFirstLastGroup() {
        var group = CreateGroup("fl_group");
        var grid = new Grid { Margin = new Thickness(8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddRow(grid, 0, "fl_first_year", CreateValueBox("fl_first_year"), "fl_first_nianhao", CreateWideValueBox("fl_first_nianhao"), "fl_first_notes", CreateMultilineValueBox("fl_first_notes"));
        AddRow(grid, 1, "fl_last_year", CreateValueBox("fl_last_year"), "fl_last_nianhao", CreateWideValueBox("fl_last_nianhao"), "fl_last_notes", CreateMultilineValueBox("fl_last_notes"));
        group.Content = grid;
        return group;
    }

    private GroupBox CreateIdentityGroup() {
        var group = CreateGroup("identity_group");
        group.Content = CreateSingleColumnGrid(new (string labelKey, FrameworkElement value)[] {
            ("choronym", CreateWideValueBox("choronym")),
            ("household", CreateWideValueBox("household")),
            ("ethnicity_tribe", CreateWideValueBox("ethnicity_tribe"))
        });
        return group;
    }

    private GroupBox CreateAuditGroup() {
        var group = CreateGroup("audit_group");
        var grid = new Grid { Margin = new Thickness(8) };
        for (var i = 0; i < 4; i++) {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i % 2 == 0 ? GridLength.Auto : new GridLength(i == 1 ? 160 : 180) });
        }
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddPair(grid, 0, 0, "created_by", CreateValueBox("created_by"));
        AddPair(grid, 0, 2, "created_date", CreateWideValueBox("created_date"));
        AddPair(grid, 1, 0, "modified_by", CreateValueBox("modified_by"));
        AddPair(grid, 1, 2, "modified_date", CreateWideValueBox("modified_date"));
        group.Content = grid;
        return group;
    }

    private FrameworkElement CreateLabeledGrid((string labelKey, FrameworkElement value)[] items, int valueColumns) {
        var grid = new Grid { Margin = new Thickness(8) };
        for (var i = 0; i < valueColumns; i++) {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == valueColumns - 1 ? new GridLength(1, GridUnitType.Star) : new GridLength(180) });
        }

        var rows = (int)Math.Ceiling(items.Length / (double)valueColumns);
        for (var row = 0; row < rows; row++) {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var index = 0; index < items.Length; index++) {
            var row = index / valueColumns;
            var pair = (index % valueColumns) * 2;
            AddPair(grid, row, pair, items[index].labelKey, items[index].value);
        }

        return grid;
    }

    private FrameworkElement CreateSingleColumnGrid((string labelKey, FrameworkElement value)[] items) {
        var grid = new Grid { Margin = new Thickness(8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < items.Length; i++) {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddPair(grid, i, 0, items[i].labelKey, items[i].value);
        }

        return grid;
    }

    private void AddRow(Grid grid, int row, string leftLabelKey, FrameworkElement leftValue, string midLabelKey, FrameworkElement midValue, string rightLabelKey, FrameworkElement rightValue) {
        AddPair(grid, row, 0, leftLabelKey, leftValue);
        AddPair(grid, row, 2, midLabelKey, midValue);
        AddPair(grid, row, 4, rightLabelKey, rightValue);
    }

    private void AddPair(Grid grid, int row, int column, string labelKey, FrameworkElement value) {
        var label = CreateLabel(labelKey);
        label.Margin = new Thickness(0, 0, 6, row == grid.RowDefinitions.Count - 1 ? 0 : 6);
        Grid.SetRow(label, row);
        Grid.SetColumn(label, column);
        grid.Children.Add(label);

        value.Margin = new Thickness(0, 0, column == grid.ColumnDefinitions.Count - 2 ? 0 : 12, row == grid.RowDefinitions.Count - 1 ? 0 : 6);
        Grid.SetRow(value, row);
        Grid.SetColumn(value, column + 1);
        grid.Children.Add(value);
    }

    private GroupBox CreateGroup(string key) {
        var group = new GroupBox { Margin = new Thickness(0, 0, 0, 10) };
        _basicGroups[key] = group;
        return group;
    }

    private TextBlock CreateLabel(string key) {
        var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _basicLabels[key] = label;
        return label;
    }

    private TextBox CreateValueBox(string key) {
        var box = new TextBox { IsReadOnly = true, MinWidth = 80 };
        _basicValues[key] = box;
        return box;
    }

    private TextBox CreateWideValueBox(string key) {
        var box = new TextBox { IsReadOnly = true, MinWidth = 160 };
        _basicValues[key] = box;
        return box;
    }

    private TextBox CreateMultilineValueBox(string key) {
        var box = new TextBox {
            IsReadOnly = true,
            MinWidth = 220,
            MinHeight = 44,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        _basicValues[key] = box;
        return box;
    }

    private CheckBox CreateReadOnlyCheckBox(string key) {
        var check = new CheckBox {
            IsEnabled = false,
            VerticalAlignment = VerticalAlignment.Center
        };
        _basicChecks[key] = check;
        return check;
    }

    private void ApplyBasicInfoLocalization() {
        SetGroupHeader("birth_group");
        SetGroupHeader("death_group");
        SetGroupHeader("fl_group");
        SetGroupHeader("identity_group");
        SetGroupHeader("audit_group");

        foreach (var pair in _basicLabels) {
            pair.Value.Text = BasicText(pair.Key);
        }

        if (_basicChecks.TryGetValue("birth_intercalary", out var birthIntercalary)) {
            birthIntercalary.Content = BasicText("birth_intercalary");
        }
        if (_basicChecks.TryGetValue("death_intercalary", out var deathIntercalary)) {
            deathIntercalary.Content = BasicText("death_intercalary");
        }
        if (_basicFieldsHeader is not null) {
            _basicFieldsHeader.Text = BasicText("other_fields_header");
        }
    }

    private void SetGroupHeader(string key) {
        if (_basicGroups.TryGetValue(key, out var group)) {
            group.Header = BasicText(key);
        }
    }

    private void PopulateBasicInfo(PersonDetail detail) {
        SetValue("birth_year", GetRawField(detail, "c_birthyear"));
        SetValue("birth_nianhao", GetDisplayOnlyField(detail, "c_by_nh_code"));
        SetValue("birth_nianhao_year", GetRawField(detail, "c_by_nh_year"));
        SetValue("birth_month", GetRawField(detail, "c_by_month"));
        SetCheck("birth_intercalary", GetBooleanField(detail, "c_by_intercalary"));
        SetValue("birth_day", GetRawField(detail, "c_by_day"));
        SetValue("birth_ganzhi", GetDisplayOnlyField(detail, "c_by_day_gz"));
        SetValue("birth_range", GetDisplayOnlyField(detail, "c_by_range"));

        SetValue("death_year", GetRawField(detail, "c_deathyear"));
        SetValue("death_nianhao", GetDisplayOnlyField(detail, "c_dy_nh_code"));
        SetValue("death_nianhao_year", GetRawField(detail, "c_dy_nh_year"));
        SetValue("death_month", GetRawField(detail, "c_dy_month"));
        SetCheck("death_intercalary", GetBooleanField(detail, "c_dy_intercalary"));
        SetValue("death_day", GetRawField(detail, "c_dy_day"));
        SetValue("death_ganzhi", GetDisplayOnlyField(detail, "c_dy_day_gz"));
        SetValue("death_range", GetDisplayOnlyField(detail, "c_dy_range"));
        SetValue("death_age", GetRawField(detail, "c_death_age"));
        SetValue("death_age_range", GetDisplayOnlyField(detail, "c_death_age_range"));

        SetValue("fl_first_year", GetRawField(detail, "c_fl_earliest_year"));
        SetValue("fl_first_nianhao", JoinDisplay(GetDisplayOnlyField(detail, "c_fl_ey_nh_code"), GetRawField(detail, "c_fl_ey_nh_year")));
        SetValue("fl_first_notes", GetRawField(detail, "c_fl_ey_notes"));
        SetValue("fl_last_year", GetRawField(detail, "c_fl_latest_year"));
        SetValue("fl_last_nianhao", JoinDisplay(GetDisplayOnlyField(detail, "c_fl_ly_nh_code"), GetRawField(detail, "c_fl_ly_nh_year")));
        SetValue("fl_last_notes", GetRawField(detail, "c_fl_ly_notes"));

        SetValue("choronym", GetDisplayOnlyField(detail, "c_choronym_code"));
        SetValue("household", GetDisplayOnlyField(detail, "c_household_status_code"));
        SetValue("ethnicity_tribe", JoinDisplay(GetDisplayOnlyField(detail, "c_ethnicity_code"), GetRawField(detail, "c_tribe")));

        SetValue("created_by", GetRawField(detail, "c_created_by"));
        SetValue("created_date", GetRawField(detail, "c_created_date"));
        SetValue("modified_by", GetRawField(detail, "c_modified_by"));
        SetValue("modified_date", GetRawField(detail, "c_modified_date"));

        _detailFields.Clear();
        foreach (var field in detail.Fields) {
            if (!StructuredBasicFieldNames.Contains(field.FieldName)) {
                _detailFields.Add(field);
            }
        }
    }

    private void ClearBasicInfo() {
        foreach (var box in _basicValues.Values) {
            box.Text = string.Empty;
        }
        foreach (var check in _basicChecks.Values) {
            check.IsChecked = false;
        }
        _detailFields.Clear();
    }

    private void SetValue(string key, string? value) {
        if (_basicValues.TryGetValue(key, out var box)) {
            box.Text = value ?? string.Empty;
        }
    }

    private void SetCheck(string key, bool isChecked) {
        if (_basicChecks.TryGetValue(key, out var check)) {
            check.IsChecked = isChecked;
        }
    }

    private static bool GetBooleanField(PersonDetail detail, string fieldName) {
        var value = GetRawField(detail, fieldName);
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRawField(PersonDetail detail, string fieldName) {
        return detail.Fields.FirstOrDefault(field => string.Equals(field.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }

    private static string GetDisplayOnlyField(PersonDetail detail, string fieldName) {
        var value = GetRawField(detail, fieldName);
        var separatorIndex = value.IndexOf("|", StringComparison.Ordinal);
        return separatorIndex >= 0 ? value[(separatorIndex + 1)..].Trim() : value;
    }

    private string BasicText(string key) {
        return _localizationService.CurrentLanguage switch {
            UiLanguage.TraditionalChinese => key switch {
                "birth_group" => "\u751F",
                "birth_gregorian" => "\u516C\u5143\u5E74\u4EFD",
                "birth_nianhao" => "\u5E74\u865F",
                "birth_nianhao_year" => "\u5E74\u865F\u5E74",
                "birth_month" => "\u6708",
                "birth_intercalary" => "\u958F\u6708",
                "birth_day" => "\u65E5",
                "birth_ganzhi" => "\u5E72\u652F",
                "birth_range" => "\u6642\u9650",
                "death_group" => "\u5352",
                "death_gregorian" => "\u516C\u5143\u5E74\u4EFD",
                "death_nianhao" => "\u5E74\u865F",
                "death_nianhao_year" => "\u5E74\u865F\u5E74",
                "death_month" => "\u6708",
                "death_intercalary" => "\u958F\u6708",
                "death_day" => "\u65E5",
                "death_ganzhi" => "\u5E72\u652F",
                "death_range" => "\u6642\u9650",
                "death_age" => "\u4EAB\u5E74",
                "death_age_range" => "\u6642\u9650",
                "fl_group" => "\u5728\u4E16\u5E74\u4EFD",
                "fl_first_year" => "\u5728\u4E16\u59CB\u5E74",
                "fl_first_nianhao" => "\u5E74\u865F\u5E74",
                "fl_first_notes" => "\u6CE8\u91CB",
                "fl_last_year" => "\u5728\u4E16\u7D42\u5E74",
                "fl_last_nianhao" => "\u5E74\u865F\u5E74",
                "fl_last_notes" => "\u6CE8\u91CB",
                "identity_group" => "\u8EAB\u4EFD\u8207\u6B78\u5C6C",
                "choronym" => "\u90E1\u671B",
                "household" => "\u6236\u7C4D",
                "ethnicity_tribe" => "\u7A2E\u65CF\uFF0F\u90E8\u65CF",
                "audit_group" => "\u5EFA\u7ACB\u8207\u4FEE\u6539",
                "created_by" => "\u5EFA\u7ACB\u4EBA",
                "created_date" => "\u5EFA\u7ACB\u65E5\u671F",
                "modified_by" => "\u4FEE\u6539\u4EBA",
                "modified_date" => "\u4FEE\u6539\u65E5\u671F",
                "other_fields_header" => "\u5176\u4ED6 BIOG_MAIN \u6B04\u4F4D",
                _ => key
            },
            UiLanguage.SimplifiedChinese => key switch {
                "birth_group" => "\u751F",
                "birth_gregorian" => "\u516C\u5143\u5E74\u4EFD",
                "birth_nianhao" => "\u5E74\u53F7",
                "birth_nianhao_year" => "\u5E74\u53F7\u5E74",
                "birth_month" => "\u6708",
                "birth_intercalary" => "\u95F0\u6708",
                "birth_day" => "\u65E5",
                "birth_ganzhi" => "\u5E72\u652F",
                "birth_range" => "\u65F6\u9650",
                "death_group" => "\u5352",
                "death_gregorian" => "\u516C\u5143\u5E74\u4EFD",
                "death_nianhao" => "\u5E74\u53F7",
                "death_nianhao_year" => "\u5E74\u53F7\u5E74",
                "death_month" => "\u6708",
                "death_intercalary" => "\u95F0\u6708",
                "death_day" => "\u65E5",
                "death_ganzhi" => "\u5E72\u652F",
                "death_range" => "\u65F6\u9650",
                "death_age" => "\u4EAB\u5E74",
                "death_age_range" => "\u65F6\u9650",
                "fl_group" => "\u5728\u4E16\u5E74\u4EFD",
                "fl_first_year" => "\u5728\u4E16\u59CB\u5E74",
                "fl_first_nianhao" => "\u5E74\u53F7\u5E74",
                "fl_first_notes" => "\u6CE8\u91CA",
                "fl_last_year" => "\u5728\u4E16\u7EC8\u5E74",
                "fl_last_nianhao" => "\u5E74\u53F7\u5E74",
                "fl_last_notes" => "\u6CE8\u91CA",
                "identity_group" => "\u8EAB\u4EFD\u4E0E\u5F52\u5C5E",
                "choronym" => "\u90E1\u671B",
                "household" => "\u6237\u7C4D",
                "ethnicity_tribe" => "\u79CD\u65CF\uFF0F\u90E8\u65CF",
                "audit_group" => "\u521B\u5EFA\u4E0E\u4FEE\u6539",
                "created_by" => "\u521B\u5EFA\u4EBA",
                "created_date" => "\u521B\u5EFA\u65E5\u671F",
                "modified_by" => "\u4FEE\u6539\u4EBA",
                "modified_date" => "\u4FEE\u6539\u65E5\u671F",
                "other_fields_header" => "\u5176\u4ED6 BIOG_MAIN \u5B57\u6BB5",
                _ => key
            },
            _ => key switch {
                "birth_group" => "Birth",
                "birth_gregorian" => "Gregorian Year",
                "birth_nianhao" => "Reign Title",
                "birth_nianhao_year" => "Reign Year",
                "birth_month" => "Month",
                "birth_intercalary" => "Intercalary Month",
                "birth_day" => "Day",
                "birth_ganzhi" => "Sexagenary Day",
                "birth_range" => "Range",
                "death_group" => "Death",
                "death_gregorian" => "Gregorian Year",
                "death_nianhao" => "Reign Title",
                "death_nianhao_year" => "Reign Year",
                "death_month" => "Month",
                "death_intercalary" => "Intercalary Month",
                "death_day" => "Day",
                "death_ganzhi" => "Sexagenary Day",
                "death_range" => "Range",
                "death_age" => "Age at Death",
                "death_age_range" => "Age Range",
                "fl_group" => "Years Alive",
                "fl_first_year" => "Earliest Living Year",
                "fl_first_nianhao" => "Reign Year",
                "fl_first_notes" => "Notes",
                "fl_last_year" => "Latest Living Year",
                "fl_last_nianhao" => "Reign Year",
                "fl_last_notes" => "Notes",
                "identity_group" => "Identity and Origin",
                "choronym" => "Choronym",
                "household" => "Household Status",
                "ethnicity_tribe" => "Ethnicity / Tribe",
                "audit_group" => "Created and Modified",
                "created_by" => "Created By",
                "created_date" => "Created Date",
                "modified_by" => "Modified By",
                "modified_date" => "Modified Date",
                "other_fields_header" => "Other BIOG_MAIN Fields",
                _ => key
            }
        };
    }
}
