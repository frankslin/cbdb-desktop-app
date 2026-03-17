using System.Reflection;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class QueryPickerBehaviorTests {
    [AvaloniaFact]
    public void StatusQueryWindow_ShowsCategorySummary_WhenSelectionMatchesType() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var child = new StatusTypeNode("11", "Neo-Confucian", "理學", Array.Empty<StatusTypeNode>(), new[] { "A", "B" });
        var extra = new StatusTypeNode("12", "Military", "軍功", Array.Empty<StatusTypeNode>(), new[] { "C" });
        var root = new StatusTypeNode(StatusPickerData.RootCode, null, null, new[] { child, extra }, new[] { "A", "B", "C" });
        var pickerData = new StatusPickerData(
            root,
            new[] {
                new StatusCodeOption("A", "Scholar", "學者", 1),
                new StatusCodeOption("B", "Teacher", "老師", 1),
                new StatusCodeOption("C", "Soldier", "士兵", 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["A"] = "11",
                ["B"] = "11",
                ["C"] = "12"
            }
        );

        var window = new StatusQueryWindow(string.Empty, localization);
        try {
            SetPrivateField(window, "_statusPickerData", pickerData);
            SetPrivateField(window, "_statusOptions", pickerData.AllStatusCodes);
            SetPrivateField(window, "_selectedStatusCodes", new List<string> { "A", "B" });

            InvokePrivate(window, "UpdateSelectedStatusesText");

            var textBox = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedStatuses");
            Assert.Equal("理學 / Neo-Confucian", textBox.Text);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EntryQueryWindow_ShowsCategorySummary_WhenSelectionMatchesType() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var child = new EntryTypeNode("21", "Examinations", "科舉", Array.Empty<EntryTypeNode>(), new[] { "E1", "E2" });
        var extra = new EntryTypeNode("22", "Recommendation", "薦舉", Array.Empty<EntryTypeNode>(), new[] { "E3" });
        var root = new EntryTypeNode(EntryPickerData.RootCode, null, null, new[] { child, extra }, new[] { "E1", "E2", "E3" });
        var pickerData = new EntryPickerData(
            root,
            new[] {
                new EntryCodeOption("E1", "Metropolitan Exam", "省試", 1),
                new EntryCodeOption("E2", "Palace Exam", "殿試", 1),
                new EntryCodeOption("E3", "Sponsored Entry", "蔭補", 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["E1"] = "21",
                ["E2"] = "21",
                ["E3"] = "22"
            }
        );

        var window = new EntryQueryWindow(string.Empty, localization);
        try {
            SetPrivateField(window, "_entryPickerData", pickerData);
            SetPrivateField(window, "_entryOptions", pickerData.AllEntryCodes);
            SetPrivateField(window, "_selectedEntryCodes", new List<string> { "E1", "E2" });

            InvokePrivate(window, "UpdateSelectedEntriesText");

            var textBox = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSelectedEntries");
            Assert.Equal("科舉 / Examinations", textBox.Text);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EntryCodePickerWindow_SearchWithoutTypeMapping_FallsBackToRootAndKeepsMatchVisible() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var child = new EntryTypeNode("21", "Examinations", "科舉", Array.Empty<EntryTypeNode>(), new[] { "E1" });
        var root = new EntryTypeNode(EntryPickerData.RootCode, null, null, new[] { child }, new[] { "E1", "X1" });
        var pickerData = new EntryPickerData(
            root,
            new[] {
                new EntryCodeOption("E1", "Metropolitan Exam", "省試", 1),
                new EntryCodeOption("X1", "Entry Special", "特入", 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["E1"] = "21"
            }
        );

        var window = new EntryCodePickerWindow(localization, pickerData);
        try {
            var txtSearch = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSearch");
            txtSearch.Text = "Entry Special";

            InvokePrivate(window, "RunSearch", false);

            var txtCurrentType = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtCurrentType");
            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "EntryOptionHost");

            Assert.Equal(localization.Get("entry_query.category_root"), txtCurrentType.Text);
            Assert.Contains(host.Children, childControl =>
                childControl is Border border &&
                border.Child is Grid grid &&
                grid.Children.OfType<StackPanel>()
                    .SelectMany(panel => panel.Children.OfType<TextBlock>())
                    .Any(text => string.Equals(text.Text, "特入 / Entry Special (X1)", StringComparison.Ordinal)));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StatusCodePickerWindow_SearchWithoutTypeMapping_FallsBackToRootAndKeepsMatchVisible() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var child = new StatusTypeNode("11", "Neo-Confucian", "理學", Array.Empty<StatusTypeNode>(), new[] { "A1" });
        var root = new StatusTypeNode(StatusPickerData.RootCode, null, null, new[] { child }, new[] { "A1", "X1" });
        var pickerData = new StatusPickerData(
            root,
            new[] {
                new StatusCodeOption("A1", "Scholar", "學者", 1),
                new StatusCodeOption("X1", "Status Special", "特例", 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["A1"] = "11"
            }
        );

        var window = new StatusCodePickerWindow(localization, pickerData);
        try {
            var txtSearch = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSearch");
            txtSearch.Text = "Status Special";

            InvokePrivate(window, "RunSearch", false);

            var txtCurrentType = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtCurrentType");
            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "StatusOptionHost");

            Assert.Equal(localization.Get("status_query.category_root"), txtCurrentType.Text);
            Assert.Contains(host.Children, childControl =>
                childControl is Border border &&
                border.Child is Grid grid &&
                grid.Children.OfType<TextBlock>()
                    .Any(text => string.Equals(text.Text, "Status Special", StringComparison.Ordinal)));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OfficeCodePickerWindow_SearchWithoutTypeMapping_FallsBackToRootAndKeepsMatchVisible() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var child = new OfficeTypeNode("31", "Civil Office", "文職", Array.Empty<OfficeTypeNode>(), new[] { "O1" });
        var root = new OfficeTypeNode(OfficePickerData.RootCode, null, null, new[] { child }, new[] { "O1", "X1" });
        var pickerData = new OfficePickerData(
            root,
            new[] {
                new OfficeCodeOption("O1", "Prefect", "知州", "Song", "宋", 1),
                new OfficeCodeOption("X1", "Example Office", "示例官職", "Yuan", "元", 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["O1"] = "31"
            }
        );

        var window = new OfficeCodePickerWindow(localization, pickerData);
        try {
            var txtSearch = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "TxtSearch");
            txtSearch.Text = "Example Office";

            InvokePrivate(window, "RunSearch", false);

            var txtCurrentType = AvaloniaUiTestHelper.FindRequiredControl<TextBlock>(window, "TxtCurrentType");
            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "OfficeOptionHost");

            Assert.Equal(localization.Get("office_query.category_root"), txtCurrentType.Text);
            Assert.Contains(host.Children, childControl =>
                childControl is Border border &&
                border.Child is Grid grid &&
                grid.Children.OfType<StackPanel>()
                    .SelectMany(panel => panel.Children.OfType<TextBlock>())
                    .Any(text => string.Equals(text.Text, "示例官職 / Example Office", StringComparison.Ordinal)));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OfficeCodePickerWindow_RootView_LimitsVisibleOptionsButKeepsSelectedItems() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var options = Enumerable.Range(1, 260)
            .Select(index => new OfficeCodeOption(
                $"O{index}",
                $"Office {index}",
                $"官職{index}",
                null,
                null,
                index))
            .ToArray();

        var pickerData = new OfficePickerData(
            new OfficeTypeNode(OfficePickerData.RootCode, null, null, Array.Empty<OfficeTypeNode>(), options.Select(option => option.Code).ToArray()),
            options,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var window = new OfficeCodePickerWindow(localization, pickerData, new[] { "O250" });
        try {
            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "OfficeOptionHost");

            Assert.Equal(200, host.Children.Count);
            Assert.Contains(host.Children, childControl =>
                childControl is Border border &&
                border.Child is Grid grid &&
                grid.Children.OfType<StackPanel>()
                    .SelectMany(panel => panel.Children.OfType<TextBlock>())
                    .Any(text => string.Equals(text.Text, "官職250 / Office 250", StringComparison.Ordinal)));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OfficeCodePickerWindow_ShowsCodeAndDynastyInSecondaryDetailText() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var pickerData = new OfficePickerData(
            new OfficeTypeNode(OfficePickerData.RootCode, null, null, Array.Empty<OfficeTypeNode>(), new[] { "O1" }),
            new[] {
                new OfficeCodeOption("O1", "Prefect", "知州", "Song", "宋", 12)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var window = new OfficeCodePickerWindow(localization, pickerData);
        try {
            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "OfficeOptionHost");
            var row = Assert.IsType<Border>(Assert.Single(host.Children));
            var grid = Assert.IsType<Grid>(row.Child);
            var textPanel = Assert.Single(grid.Children.OfType<StackPanel>());
            var textBlocks = textPanel.Children.OfType<TextBlock>().ToArray();

            Assert.Equal("知州 / Prefect", textBlocks[0].Text);
            Assert.Equal("Office Code: O1    宋 / Song    Count: 12", textBlocks[1].Text);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OfficeCodePickerWindow_CollapsedTreeNode_UsesPlaceholderUntilExpanded() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var grandChild = new OfficeTypeNode("311", "Circuit Office", "路級官職", Array.Empty<OfficeTypeNode>(), new[] { "O2" });
        var child = new OfficeTypeNode("31", "Civil Office", "文職", new[] { grandChild }, new[] { "O1", "O2" });
        var root = new OfficeTypeNode(OfficePickerData.RootCode, null, null, new[] { child }, new[] { "O1", "O2" });
        var pickerData = new OfficePickerData(
            root,
            new[] {
                new OfficeCodeOption("O1", "Prefect", "知州", null, null, 1),
                new OfficeCodeOption("O2", "Circuit Intendant", "轉運使", null, null, 1)
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["O1"] = "31",
                ["O2"] = "311"
            }
        );

        var window = new OfficeCodePickerWindow(localization, pickerData);
        try {
            var childItem = InvokePrivate<TreeViewItem>(window, "BuildTreeItem", child);
            var collapsedItems = Assert.IsAssignableFrom<IEnumerable<object>>(childItem.ItemsSource);
            Assert.Single(collapsedItems);
            Assert.DoesNotContain(collapsedItems, item => item is TreeViewItem);

            InvokePrivate(window, "TreeItem_Expanded", childItem, new RoutedEventArgs());

            var expandedItems = Assert.IsAssignableFrom<IEnumerable<object>>(childItem.ItemsSource);
            Assert.Contains(expandedItems, item => item is TreeViewItem treeItem && treeItem.Tag is OfficeTypeNode node && node.Code == "311");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OfficeCodePickerWindow_CategoryView_LimitsVisibleOptionsButKeepsSelectedItems() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var officeCodes = Enumerable.Range(1, 360).Select(index => $"O{index}").ToArray();
        var child = new OfficeTypeNode("31", "Civil Office", "文職", Array.Empty<OfficeTypeNode>(), officeCodes);
        var root = new OfficeTypeNode(OfficePickerData.RootCode, null, null, new[] { child }, officeCodes);
        var options = officeCodes
            .Select((code, index) => new OfficeCodeOption(code, $"Office {index + 1}", $"官職{index + 1}", null, null, index + 1))
            .ToArray();

        var pickerData = new OfficePickerData(
            root,
            options,
            officeCodes.ToDictionary(code => code, _ => "31", StringComparer.OrdinalIgnoreCase));

        var window = new OfficeCodePickerWindow(localization, pickerData, new[] { "O350" });
        try {
            SetPrivateField(window, "_activeTypeNode", child);
            InvokePrivate(window, "RenderOptions");

            var host = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "OfficeOptionHost");

            Assert.Equal(300, host.Children.Count);
            Assert.Contains(host.Children, childControl =>
                childControl is Border border &&
                border.Child is Grid grid &&
                grid.Children.OfType<StackPanel>()
                    .SelectMany(panel => panel.Children.OfType<TextBlock>())
                    .Any(text => string.Equals(text.Text, "官職350 / Office 350", StringComparison.Ordinal)));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PlacePickerWindow_SingleSelectionUpdatesExistingRowState() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var options = new[] {
            new PlaceOption(1001, "Kaifeng", "開封", "Prefecture", 960, 1127, null, null, null, null, 114.3, 34.8),
            new PlaceOption(1002, "Hangzhou", "杭州", "Prefecture", 960, 1279, null, null, null, null, 120.1, 30.3)
        };

        var window = new PlacePickerWindow(localization, options);
        try {
            var visibleRows = GetPrivateField<ObservableCollection<PlacePickerWindow.PlaceOptionRow>>(window, "_visibleRows");
            var firstRow = visibleRows[0];

            InvokePrivate(window, "SetPlaceSelection", firstRow.AddressId, true);

            Assert.Same(firstRow, visibleRows[0]);
            Assert.True(firstRow.IsSelected);
            Assert.Equal(new[] { firstRow.AddressId }, window.SelectedPlaceIds);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PlacePickerWindow_RowWithCoordinatesBuildsGoogleMapsUrl() {
        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var options = new[] {
            new PlaceOption(1001, "Kaifeng", "開封", "Prefecture", 960, 1127, null, null, null, null, 114.3, 34.8)
        };

        var window = new PlacePickerWindow(localization, options);
        try {
            var visibleRows = GetPrivateField<ObservableCollection<PlacePickerWindow.PlaceOptionRow>>(window, "_visibleRows");
            var row = visibleRows[0];

            Assert.True(row.HasCoordinates);
            Assert.Equal("https://www.openstreetmap.org/?mlat=34.8&mlon=114.3#map=4/34.8/114.3", row.MapUrl);
        } finally {
            window.Close();
        }
    }

    private static void SetPrivateField(object target, string fieldName, object? value) {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName) {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field!.GetValue(target));
    }

    private static void InvokePrivate(object target, string methodName, params object?[] args) {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(target, args);
    }

    private static T InvokePrivate<T>(object target, string methodName, params object?[] args) {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<T>(method!.Invoke(target, args));
    }
}
