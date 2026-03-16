using System.Reflection;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
}
